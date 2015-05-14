using System;
using System.Web;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckDebug : IAuditCheck
    {
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckDebug");
            try
            {
                if (HttpContext.Current.IsDebuggingEnabled)
                {
                    result.Severity = SeverityEnum.Warning;
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