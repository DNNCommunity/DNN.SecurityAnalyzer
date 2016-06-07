using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Data;
using DotNetNuke.Services.Localization;
using Microsoft.SqlServer.Server;
using Assembly = System.Reflection.Assembly;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckSqlRisk : IAuditCheck
    {
        private string LocalResourceFile
        {
            get { return "~/DesktopModules/DNNCorp/SecurityAnalyzer/App_LocalResources/view.ascx"; }
        }

        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckSqlRisk");
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
                    result.Severity = SeverityEnum.Failure;
                    result.Notes.Add(Localization.GetString(name + ".Error", LocalResourceFile));
                }
            }
            return result;
        }

        private bool VerifyScript(string name)
        {
            try
            {
                var script = LoadScript(name);
                var reader = DataProvider.Instance().ExecuteSQL(script);
                if (reader.Read())
                {
                    var affectCount = Convert.ToInt32(reader[0]);
                    return affectCount == 0;
                }

                if (!reader.IsClosed)
                {
                    reader.Close();
                }
            }
            catch (SqlException ex)
            {
                return true;
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
                else
                {
                    return null;
                }

            }
        }
    }
}