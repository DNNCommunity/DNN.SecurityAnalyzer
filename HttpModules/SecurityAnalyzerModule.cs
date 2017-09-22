#region Copyright
// 
// DotNetNuke® - http://www.dnnsoftware.com
// Copyright (c) 2002-2017
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using DNN.Modules.SecurityAnalyzer.Components;
using DotNetNuke.Application;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Data;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Host;
using DotNetNuke.Security;
using DotNetNuke.Services.Installer.Packages;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Services.Mail;

namespace DNN.Modules.SecurityAnalyzer.HttpModules
{
    public class SecurityAnalyzerModule : IHttpModule
    {
        private static FileSystemWatcher _fileWatcher;
        private static readonly Version Dnn911Ver = new Version(9, 1, 1);
        private static readonly Version Dnn920Ver = new Version(9, 2, 0);
        private static DateTime _lastRead;
        private static Globals.UpgradeStatus _appStatus = Globals.UpgradeStatus.None;
        private static IEnumerable<string> _settingsRestrictExtensions = new string[] { };
        private static Queue<string> _filesQ;
        private static Timer _qTimer;

        // used to indicate already send first real-time email within last SlidingDelay period
        private static int _notificationSent;

        private const int CacheTimeOut = 5; //obtain the setting and do calculations once every 5 minutes at most, plus no need for locking
        private const int SlidingDelay = 30 * 1000; // milliseconds

        private static bool FileWatcherInitialized { get; set; }
        private const int FileWatcherUpgradeGracePeriod = 5; //Wait x minutes after major upgrade is done
        private static DateTime _fileWatcherRecheckTime = DateTime.MinValue;
        private static bool _siteReadyForFileWatch = false; //Is site past the upgrade cycle and ready for file watching

        //indicates whether the SA Http Module is present or not
        internal static bool SAHttpModuleExists { get; private set; }

        // Source: Configuring Blocked File Extensions
        // https://msdn.microsoft.com/en-us/library/cc767397.aspx
        private static readonly IEnumerable<string> DefaultRestrictExtensions =
            (
                ".ade,.adp,.app,.ashx,.asmx,.asp,.aspx,.bas,.bat,.chm,.class,.cmd,.com,.cpl,.crt,.dll,.exe," +
                ".fxp,.hlp,.hta,.ins,.isp,.jse,.lnk,.mda,.mdb,.mde,.mdt,.mdw,.mdz,.msc,.msi,.msp,.mst,.ops,.pcd,.php," +
                ".pif,.prf,.prg,.py,.reg,.scf,.scr,.sct,.shb,.shs,.url,.vb,.vbe,.vbs,.wsc,.wsf,.wsh"
            )
            .ToLowerInvariant()
            .Split(',')
            .Where(e => !string.IsNullOrEmpty(e))
            .Select(e => e.Trim())
            .ToList();

        private const string ResourceFile = "~/DesktopModules/DNNCorp/SecurityAnalyzer/App_LocalResources/View.ascx.resx";

        public void Init(HttpApplication context)
        {
            if(!SAHttpModuleExists)
                SAHttpModuleExists = true;

            var currentAppVersion = DotNetNukeContext.Current.Application.Version;
            if (currentAppVersion < Dnn920Ver)
            {
                InitializeCookieHandler(context);
            }

            new TelerikCompatibility().PatchIt();

            /*
            if (currentAppVersion < Dnn920Ver)
            {
                if (!FileWatcherInitialized)
                {
                    lock (typeof(SecurityAnalyzerModule))
                    {
                        if (!FileWatcherInitialized)
                        {
                             InitializeFileWatcher();
                            FileWatcherInitialized = true;
                        }
                    }
                }
            }
            */
        }

        public void Dispose()
        {
        }

        #region File Watcher Functions

        private static void InitializeFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher
            {
                Filter = "*.*",
                Path = Globals.ApplicationMapPath,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
            };

            _fileWatcher.Created += WatcherOnCreated;
            _fileWatcher.Renamed += WatcherOnRenamed;
            _fileWatcher.Error += WatcherOnError;

            _filesQ = new Queue<string>();
            _qTimer = new Timer(QTimerCallBack);

            AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
            {
                _fileWatcher.Dispose();
                QTimerCallBack(null);
            };

            _fileWatcher.EnableRaisingEvents = true;
        }

        private static void QTimerCallBack(object obj)
        {
            try
            {
                string[] items;
                lock (_filesQ)
                {
                    while (!_qTimer.Change(Timeout.Infinite, Timeout.Infinite)) { }
                    Interlocked.Exchange(ref _notificationSent, 0);
                    items = _filesQ.ToArray();
                    while (_filesQ.Count > 0) _filesQ.Dequeue();
                }

                if (items.Length > 0)
                    NotifyManager(items);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private static void WatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            CheckFile(e.FullPath);
        }

        private static void WatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            CheckFile(e.FullPath);
        }

        private static void WatcherOnError(object sender, ErrorEventArgs e)
        {
            LogException(e.GetException());
        }

        private static void LogException(Exception ex)
        {
            Trace.TraceError("Watcher Activity Error: " + ex?.Message);
        }

        private static void CheckFile(string path)
        {
            try
            {
                if (IsRestrictdExtension(path) && ReadyToFileWatch())
                {
                    if (_appStatus != Globals.UpgradeStatus.Install && _appStatus != Globals.UpgradeStatus.Upgrade)
                    {
                        Globals.UpgradeStatus appStatus;

                        try
                        {
                            appStatus = Globals.Status;
                        }
                        catch (NullReferenceException)
                        {
                            appStatus = Globals.UpgradeStatus.None;
                        }

                        // make status sticky; once set to install/upgrade, it stays so until finishing & appl restarts
                        if (appStatus == Globals.UpgradeStatus.Install || appStatus == Globals.UpgradeStatus.Upgrade)
                        {
                            _appStatus = appStatus;
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem(_ => AddEventLog(path));
                            var val = Interlocked.Increment(ref _notificationSent);
                            if (val <= 1)
                            {
                                // first notification goes immediately
                                ThreadPool.QueueUserWorkItem(_ => NotifyManager(new[] { path }));
                            }
                            else
                            {
                                lock (_filesQ)
                                {
                                    while (!_qTimer.Change(val >= 100 ? 1 : SlidingDelay, Timeout.Infinite)) { }
                                    _filesQ.Enqueue(path);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }


        //Ensure few minutes have elapsed since last upgrade, or else it will send lots of notifications
        private static bool ReadyToFileWatch()
        {
            if (_siteReadyForFileWatch) return true;

            //Don't bother making SQL calls as we know that time has not come to recheck
            if (DateTime.Now < _fileWatcherRecheckTime) return false;

            var elapsedMinutes = GetElapsedMinutesSinceUpgrade();

            if (elapsedMinutes >= FileWatcherUpgradeGracePeriod)
            {
                _siteReadyForFileWatch = true;
                return true;
            }
            _fileWatcherRecheckTime = DateTime.Now.AddMinutes(FileWatcherUpgradeGracePeriod - elapsedMinutes);
            return false;
        }


        private static int GetElapsedMinutesSinceUpgrade()
        {
            var elapsedMinutes = 0;
            try
            {
                using (var reader = DataProvider.Instance().ExecuteSQL("SELECT DATEDIFF(MI,MAX([CreatedDate]), GETDATE()) AS ElapsedMinutes FROM {databaseOwner}[{objectQualifier}Version]"))
                {
                    if (reader.Read())
                    {
                        elapsedMinutes = Convert.ToInt32(reader["ElapsedMinutes"]);
                    }
                    reader.Close();
                }
            }
            catch (Exception)
            {
                //do nothing
            }

            return elapsedMinutes;
        }

        private static bool IsRestrictdExtension(string path)
        {
            var extension = Path.GetExtension(path)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) &&
                GetRestrictExtensions().Contains(extension);
        }

        private static IEnumerable<string> GetRestrictExtensions()
        {
            // obtain the setting and do calculations once every 5 minutes at most, plus no need for locking
            if ((DateTime.Now - _lastRead).TotalMinutes > CacheTimeOut)
            {
                _lastRead = DateTime.Now;
                var settings = HostController.Instance.GetString("SA_RestrictExtensions", string.Empty);
                _settingsRestrictExtensions = string.IsNullOrEmpty(settings)
                    ? DefaultRestrictExtensions
                    : settings.ToLowerInvariant()
                        .Split(',')
                        .Where(e => !string.IsNullOrEmpty(e))
                        .Select(e => e.Trim())
                        .Concat(DefaultRestrictExtensions)
                        .ToList();
            }

            return _settingsRestrictExtensions ?? DefaultRestrictExtensions;
        }

        private static void AddEventLog(string path)
        {
            try
            {
                var log = new LogInfo
                {
                    LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString(),
                };
                log.AddProperty("Summary", Localization.GetString("PotentialDangerousFile.Text", ResourceFile));
                log.AddProperty("File Name", path);

                new LogController().AddLog(log);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private static void NotifyManager(string[] paths)
        {
            try
            {                
                var package = PackageController.GetPackages(Null.NullInteger)
                    .FirstOrDefault(
                        p =>
                            p.Name.Equals("SecurityAnalyzer", StringComparison.InvariantCultureIgnoreCase) &&
                            p.PackageType.Equals("Module", StringComparison.InvariantCultureIgnoreCase));
                var pathNames = string.Join("<br/>", paths);
                var subject = Localization.GetString("RestrictFileMail_Subject.Text", ResourceFile);
                var body = Localization.GetString("RestrictFileMail_Body.Text", ResourceFile)
                    .Replace("[Path]", pathNames)
                    .Replace("[ModuleName]", package?.FriendlyName)
                    .Replace("[ModuleVersion]", package?.Version.ToString());

                Mail.SendEmail(Host.HostEmail, Host.HostEmail, subject, body);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        #endregion

        #region Personalization Cookie Functions

        private static void InitializeCookieHandler(HttpApplication application)
        {
            application.BeginRequest += CookieHandler_OnBeginRequest;
            application.EndRequest += CookieHandler_OnEndRequest;
        }

        private static void CookieHandler_OnBeginRequest(object sender, EventArgs e)
        {
            try
            {
                var httpApplication = sender as HttpApplication;
                if (httpApplication == null) return;

                var request = httpApplication.Request;
                if (request.Cookies["DNNPersonalization"] != null)
                {
                    var cookiesValue = request.Cookies["DNNPersonalization"].Value;
                    string decryptValue;
                    if (NeedDecrypt(cookiesValue, out decryptValue))
                    {
                        var cookie = request.Headers["Cookie"]
                            .Replace("DNNPersonalization=" + cookiesValue, "DNNPersonalization=" + decryptValue);
                        request.Headers["Cookie"] = cookie;

                        var workRequestField = request.GetType().GetField("_wr", BindingFlags.Instance | BindingFlags.NonPublic);
                        var workRequest = workRequestField?.GetValue(request);

                        var headerField = workRequest?.GetType().GetField("_knownRequestHeaders", BindingFlags.Instance | BindingFlags.NonPublic);
                        var headers = headerField?.GetValue(workRequest) as string[];

                        for (var i = 0; i < headers?.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(headers[i]) && headers[i].Contains("DNNPersonalization="))
                            {
                                headers[i] = cookie;
                                break;
                            }
                        }

                        headerField?.SetValue(workRequest, headers);

                        if (string.IsNullOrEmpty(decryptValue))
                        {
                            var personalizationCookie = new HttpCookie("DNNPersonalization", string.Empty)
                            {
                                Expires = DateTime.Now.AddDays(-1),
                                Path = (!string.IsNullOrEmpty(Globals.ApplicationPath) ? Globals.ApplicationPath : "/")
                            };

                            httpApplication.Response.SetCookie(personalizationCookie);
                        }

                        httpApplication.Response.SetCookie(new HttpCookie("RegenerateCookies", "true"));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        private static void CookieHandler_OnEndRequest(object sender, EventArgs e)
        {
            try
            {
                var httpApplication = sender as HttpApplication;
                if (httpApplication == null) return;

                var response = httpApplication.Response;
                if (response.Cookies.AllKeys.Contains("DNNPersonalization"))
                {
                    var cookiesValue = response.Cookies["DNNPersonalization"]?.Value;
                    if (!string.IsNullOrEmpty(cookiesValue) && IsPlainText(cookiesValue))
                    {
                        var encryptValue = new PortalSecurity().Encrypt(GetDecryptionkey(), cookiesValue);
                        response.Cookies["DNNPersonalization"].Value = encryptValue
                            .Replace("/", "_")
                            .Replace("+", "-")
                            .Replace("=", "%3d");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        private static string GetDecryptionkey()
        {
            var key = System.Configuration.ConfigurationManager.GetSection("system.web/machineKey") as MachineKeySection;
            return key?.DecryptionKey ?? string.Empty;
        }

        private static bool NeedDecrypt(string cookiesValue, out string decryptValue)
        {
            decryptValue = string.Empty;

            if (string.IsNullOrEmpty(cookiesValue))
            {
                return false;
            }

            if (IsPlainText(cookiesValue))
            {
                return true;
            }

            cookiesValue = cookiesValue
                .Replace("_", "/")
                .Replace("-", "+")
                .Replace("%3d", "=");
            decryptValue = new PortalSecurity().Decrypt(GetDecryptionkey(), cookiesValue);

            if (!IsPlainText(decryptValue))
            {
                decryptValue = string.Empty;
                return false;
            }
            return true;
        }

        private static bool IsPlainText(string cookiesValue)
        {
            // we need this to be very fast and not throw any exception
            return cookiesValue.IndexOf("<profile", StringComparison.InvariantCultureIgnoreCase) > Null.NullInteger
                    || cookiesValue.IndexOf("item", StringComparison.InvariantCultureIgnoreCase) > Null.NullInteger
                    || cookiesValue.IndexOf("System.", StringComparison.InvariantCultureIgnoreCase) > Null.NullInteger
                    || cookiesValue.IndexOf("MethodName", StringComparison.InvariantCultureIgnoreCase) > Null.NullInteger
                    || cookiesValue.IndexOf("MethodParameters", StringComparison.InvariantCultureIgnoreCase) > Null.NullInteger;
        }

        #endregion
    }
}