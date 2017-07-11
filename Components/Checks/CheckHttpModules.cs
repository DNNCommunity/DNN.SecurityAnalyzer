using System.Web;
using DNN.Modules.SecurityAnalyzer.HttpModules;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckHttpModules : IAuditCheck
    {
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckHttpModules")
            {
                Severity = !SecurityAnalyzerModule.Initialized
                    ? SeverityEnum.Failure
                    : SeverityEnum.Pass
            };
            return result;
        }
    }
}