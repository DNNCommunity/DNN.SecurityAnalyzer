using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Host;
using DotNetNuke.Security;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Services.Mail;

namespace DNN.Modules.SecurityAnalyzer.HttpModules
{
    public class FileWatcherModule : IHttpModule
    {
        private static bool _initialized;
        private static object _threadLocker = new object();
        private static readonly IList<string> _defaultRestrictExtensions = new List<string>() {".asp", ".asa", ".aspx", ".asax", ".ashx", ".php"}; 
        private const string ResourceFile = "~/DesktopModules/DNNCorp/SecurityAnalyzer/App_LocalResources/View.ascx.resx";

        public void Init(HttpApplication context)
        {
            if (!_initialized)
            {
                lock (_threadLocker)
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

        private void Initialize()
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
            fileWatcher.Changed += WatcherOnChanged;
            fileWatcher.Error += WatcherOnError;

            fileWatcher.EnableRaisingEvents = true;

            AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
            {
                fileWatcher.Dispose();
            };
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            CheckFile(e.FullPath);
        }

        private void WatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            CheckFile(e.FullPath);
        }

        private void WatcherOnCreated(object sender, FileSystemEventArgs e)
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

        private void CheckFile(string path)
        {
            try
            {
                var extension = Path.GetExtension(path)?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(extension) && IsRestrictExtension(extension))
                {
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            AddEventLog(path);
                            NotifyManager(path);
                        }
                        catch (Exception ex)
                        {
                            LogException(ex);
                        }
                        
                    });
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
            
        }

        private bool IsRestrictExtension(string extension)
        {
            return GetRestrictExtensions().Select(WildCardToRegular).Any(pattern => Regex.IsMatch(extension, pattern));
        }

        private IList<string> GetRestrictExtensions()
        {
            var settings = HostController.Instance.GetString("SA_RestrictExtensions", string.Empty);
            if (string.IsNullOrEmpty(settings))
            {
                return _defaultRestrictExtensions;
            }

            return settings.ToLowerInvariant()
                                .Split(',')
                                .Where(e => !string.IsNullOrEmpty(e))
                                .Select(e => e.Trim()).ToList();
        }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

        private void AddEventLog(string path)
        {
            var log = new LogInfo
            {
                LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString(),
            };
            log.AddProperty("Summary", "Dangerous File modification found in the website.");
            log.AddProperty("File Name", path);

            new LogController().AddLog(log);
        }

        private void NotifyManager(string path)
        {
            var subject = Localization.GetString("RestrictFileMail_Subject.Text", ResourceFile);
            var body = Localization.GetString("RestrictFileMail_Body.Text", ResourceFile).Replace("[Path]", path);

            Mail.SendEmail(Host.HostEmail, Host.HostEmail, subject, body);
        }
    }
}