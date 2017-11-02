using DotNetNuke.Entities.Controllers;

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
            return result;
        }
    }
}