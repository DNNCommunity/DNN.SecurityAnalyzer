using System.Web;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckDebug : IAuditCheck
    {
        public string Id => "CheckDebug";

        public bool LazyLoad => false;

        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, Id)
            {
                Severity = HttpContext.Current.IsDebuggingEnabled
                    ? SeverityEnum.Warning
                    : SeverityEnum.Pass
            };
            return result;
        }
    }
}