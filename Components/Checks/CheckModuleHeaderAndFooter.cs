using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using DotNetNuke.Application;
using DotNetNuke.Common;
using DotNetNuke.Data;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckModuleHeaderAndFooter : IAuditCheck
    {
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckModuleHeaderAndFooter");
            try
            {
                var dr = DataProvider.Instance().ExecuteReader("SecurityAnalyzer_GetModulesHasHeaderFooter");
                result.Severity = SeverityEnum.Pass;
                while (dr.Read())
                {
                    result.Severity = SeverityEnum.Warning;
                    var note = string.Format("TabId: {0}, Module Id: {1}", dr["TabId"], dr["ModuleId"]);
                    var headerValue = dr["Header"].ToString();
                    var footerValue = dr["Footer"].ToString();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        note += string.Format("<br />Header: {0}", HttpUtility.HtmlEncode(headerValue));
                    }
                    if (!string.IsNullOrEmpty(footerValue))
                    {
                        note += string.Format("<br />Footer: {0}", HttpUtility.HtmlEncode(footerValue));
                    }

                    result.Notes.Add(note);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }
    }
}