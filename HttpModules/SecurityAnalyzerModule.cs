using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web;
using DotNetNuke.Application;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
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
        private static readonly object ThreadLocker = new object();

        private static readonly Version DnnCutoffVer = new Version(9,1,1);
        private static DateTime _lastRead;
        private static IEnumerable<string> _settingsRestrictExtensions = new string[] { };

        internal static bool Initialized { get; private set; }

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
            if (DnnCutoffVer < DotNetNukeContext.Current.Application.Version)
            {
                InitializeCookieHandler(context);
            }

            if (!Initialized)
            {
                lock (ThreadLocker)
                {
                    if (!Initialized)
                    {
                        InitializeFileWatcher();
                        Initialized = true;
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        #region File Watcher Functions

        private static void InitializeFileWatcher()
        {
            var fileWatcher = new FileSystemWatcher
            {
                Filter = "*.*",
                Path = Globals.ApplicationMapPath,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
            };

            fileWatcher.Created += WatcherOnCreated;
            fileWatcher.Renamed += WatcherOnRenamed;
            fileWatcher.Error += WatcherOnError;

            fileWatcher.EnableRaisingEvents = true;

            AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
            {
                fileWatcher.Dispose();
            };
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
            Trace.WriteLine("Watcher Activity: N/A. Error: " + ex?.Message);
        }

        private static void CheckFile(string path)
        {
            try
            {
                if (IsRestrictdExtension(path))
                {
                    ThreadPool.QueueUserWorkItem(_ => AddEventLog(path));
                    ThreadPool.QueueUserWorkItem(_ => NotifyManager(path));
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
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
            if ((DateTime.Now - _lastRead).TotalMinutes > 5)
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
                log.AddProperty("Summary", "A dangerous file has been added to your website");
                log.AddProperty("File Name", path);

                new LogController().AddLog(log);
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private static void NotifyManager(string path)
        {
            try
            {
                var package = PackageController.GetPackages(Null.NullInteger)
                    .FirstOrDefault(
                        p =>
                            p.Name.Equals("SecurityAnalyzer", StringComparison.InvariantCultureIgnoreCase) &&
                            p.PackageType.Equals("Module", StringComparison.InvariantCultureIgnoreCase));
                var subject = Localization.GetString("RestrictFileMail_Subject.Text", ResourceFile);
                var body = Localization.GetString("RestrictFileMail_Body.Text", ResourceFile)
                    .Replace("[Path]", path)
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
            application.PreRequestHandlerExecute += CookieHandler_OnBeginRequest;
            application.EndRequest += CookieHandler_OnEndRequest;
        }

        private static void CookieHandler_OnBeginRequest(object sender, EventArgs e)
        {
            try
            {
                var httpApplication = sender as HttpApplication;
                var request = httpApplication?.Request;
                if (request?.Cookies["DNNPersonalization"] != null)
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
                        httpApplication?.Response.SetCookie(new HttpCookie("RegenerateCookies", "true"));
                    }
                    else
                    {
                        //TODO: remove plain text DNNPersonalization cookie
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private static void CookieHandler_OnEndRequest(object sender, EventArgs e)
        {
            try
            {
                var httpApplication = sender as HttpApplication;
                if (httpApplication != null)
                {
                    var response = httpApplication.Response;
                    if (response.Cookies.AllKeys.Contains("DNNPersonalization"))
                    {
                        var cookiesValue = response.Cookies["DNNPersonalization"]?.Value;
                        if (!string.IsNullOrEmpty(cookiesValue) && IsPlainText(cookiesValue))
                        {
                            var encryptValue = new PortalSecurity().Encrypt(Config.GetDecryptionkey(), cookiesValue);
                            response.Cookies["DNNPersonalization"].Value = encryptValue
                                .Replace("/", "_")
                                .Replace("+", "-")
                                .Replace("=", "%3d");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private static bool NeedDecrypt(string cookiesValue, out string decryptValue)
        {
            decryptValue = string.Empty;

            if (string.IsNullOrEmpty(cookiesValue) || IsPlainText(cookiesValue))
            {
                return false;
            }

            cookiesValue = cookiesValue
                .Replace("_", "/")
                .Replace("-", "+")
                .Replace("%3d", "=");
            decryptValue = new PortalSecurity().Decrypt(Config.GetDecryptionkey(), cookiesValue);

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
            return cookiesValue.IndexOf("<profile", StringComparison.InvariantCultureIgnoreCase) > 0;
        }

        #endregion
    }
}