using System.Collections.Generic;
using DNN.Modules.SecurityAnalyzer.Components.Checks;

namespace DNN.Modules.SecurityAnalyzer.Components
{
    public class AuditChecks
    {
        private readonly IEnumerable<IAuditCheck> _auditChecks;

        public AuditChecks()
        {
            _auditChecks = new List<IAuditCheck>
            {
                new CheckDebug(),
                new CheckTracing(),
                new CheckViewstatemac(),
                new CheckBiography(),
                new CheckSiteRegistration(),
                new CheckRarelyUsedSuperuser(),
                new CheckSuperuserOldPassword(),
                new CheckUnexpectedExtensions(),
                new CheckDefaultPage(),
                new CheckModuleHeaderAndFooter(),
                new CheckPasswordFormat(),
                new CheckDiskAcccessPermissions(),
            }.AsReadOnly();
        }

        public List<CheckResult> DoChecks()
        {
            var results = new List<CheckResult>();
            foreach (var check in _auditChecks)
            {
                var result = check.Execute();
                results.Add(result);
            }
            return results;
        }
    }
}