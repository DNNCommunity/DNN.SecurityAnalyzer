using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Host;
using DotNetNuke.Services.Installer.Packages;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Services.Mail;

namespace DNN.Modules.SecurityAnalyzer.HttpModules
{
    public class FileWatcherModule : IHttpModule
    {
        private static bool _initialized;
        private static readonly object ThreadLocker = new object();

        private static DateTime _lastRead;
        private static IEnumerable<string> _settingsRestrictExtensions = new string[] { };

        internal static bool Initialized => _initialized;

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
            if (!_initialized)
            {
                lock (ThreadLocker)
                {
                    if (!_initialized)
                    {
                        Initialize();
                        _initialized = true;
                    }
                }
            }
        }

        public void Dispose()
        {
        }

        private static void Initialize()
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
            Trace.WriteLine("Watcher Activity: N/A. Error: " + ex?.Message ?? "N/A");
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
    }
}