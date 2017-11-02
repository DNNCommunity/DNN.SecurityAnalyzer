﻿using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using DotNetNuke.Common;
using DotNetNuke.Data;
using DotNetNuke.Services.Localization;
using Assembly = System.Reflection.Assembly;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckSqlRisk : IAuditCheck
    {
        public string Id => "CheckSqlRisk";

        public bool LazyLoad => false;

        private string LocalResourceFile
        {
            get { return "~/DesktopModules/DNNCorp/SecurityAnalyzer/App_LocalResources/view.ascx"; }
        }

        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, Id);
            IList<string> checkList = new List<string>()
            {
                "SysAdmin",
                "ExecuteCommand",
                "GetFolderTree",
                "CheckFileExists",
                "RegRead"
            };

            result.Severity = SeverityEnum.Pass;
            foreach (var name in checkList)
            {
                if (!VerifyScript(name))
                {
                    result.Severity = SeverityEnum.Warning;
                    result.Notes.Add(Localization.GetString(name + ".Error", LocalResourceFile));
                }
            }
            return result;
        }

        private static bool VerifyScript(string name)
        {
            try
            {
                var script = LoadScript(name);
                if (!string.IsNullOrEmpty(script))
                {
                    using (var reader = DataProvider.Instance().ExecuteSQL(script))
                    {
                        if (reader != null && reader.Read())
                        {
                            int affectCount;
                            int.TryParse(reader[0].ToString(), out affectCount);
                            return affectCount == 0;
                        }
                    }
                }
            }
            catch (SqlException)
            {
                //ignore; return no failure
            }
            return true;
        }

        public static string LoadScript(string name)
        {
            var resourceName = string.Format("DNN.Modules.SecurityAnalyzer.Resources.{0}.resources", name);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    var script = new StreamReader(stream).ReadToEnd();
                    return script.Replace("%SiteRoot%", Globals.ApplicationMapPath);
                }

                return null;
            }
        }
    }
}