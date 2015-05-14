using System;
using System.Collections.Generic;
using DotNetNuke.Services.Localization;

namespace DNN.Modules.SecurityAnalyzer.Components
{
    public class CheckResult
    {
        public CheckResult(SeverityEnum severity, string checkname)
        {
            Severity = severity;
            CheckName = checkname;
            Notes = new List<string>();
        }

        public SeverityEnum Severity { get; set; }
        public string CheckName { get; set; }

        public string Reason
        {
            get
            {
                var test = CheckName + "Reason.Text";
                return Localization.GetString(test, LocalResourceFile);
            }
        }

        public string FailureText
        {
            get { return Localization.GetString(CheckName + "Failure", LocalResourceFile); }
        }

        public string SuccessText
        {
            get { return Localization.GetString(CheckName + "Success", LocalResourceFile); }
        }

        public string CheckNameText
        {
            get { return  Localization.GetString(CheckName + "Name", LocalResourceFile); }
        }

        public IList<String> Notes { get; set; }

        private string LocalResourceFile
        {
            get { return "~/DesktopModules/SecurityAnalyzer/App_LocalResources/view.ascx"; }
        }
    }
}