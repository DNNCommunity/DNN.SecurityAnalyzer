using System;
using System.Web;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Host;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckAllowableFileExtensions : IAuditCheck
    {
        public string Id => "CheckAllowableFileExtensions";

        public bool LazyLoad => false;

        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, Id);
            var allowedExtensions = new FileExtensionWhitelist(HostController.Instance.GetString("FileExtensions"));
            try
            {
                if (allowedExtensions.IsAllowedExtension("asp")
                        || allowedExtensions.IsAllowedExtension("aspx")
                        || allowedExtensions.IsAllowedExtension("php"))
                {
                    result.Severity = SeverityEnum.Failure;
                    result.Notes.Add("Extensions: " + allowedExtensions.ToDisplayString());
                }
                else
                {
                    result.Severity = SeverityEnum.Pass;
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