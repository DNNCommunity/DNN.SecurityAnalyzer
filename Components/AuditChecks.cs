using System;
using System.Collections.Generic;
using DNN.Modules.SecurityAnalyzer.Components.Checks;
using DotNetNuke.Common;

namespace DNN.Modules.SecurityAnalyzer.Components
{
    public class AuditChecks
    {
        private readonly IEnumerable<IAuditCheck> _auditChecks;

        public AuditChecks()
        {
            var checks = new List<IAuditCheck>
            {
                new CheckDebug(),
                new CheckTracing(),
                new CheckBiography(),
                new CheckSiteRegistration(),
                new CheckRarelyUsedSuperuser(),
                new CheckSuperuserOldPassword(),
                new CheckUnexpectedExtensions(),
                new CheckDefaultPage(),
                new CheckModuleHeaderAndFooter(),
                new CheckPasswordFormat(),
                new CheckDiskAcccessPermissions(),
                new CheckSqlRisk(),
                new CheckAllowableFileExtensions(),
                new CheckTelerikVulnerability()
            };

            if (Globals.NETFrameworkVersion <= new Version(4, 5, 1))
            {
                checks.Insert(2, new CheckViewstatemac());
            }

            _auditChecks= checks.AsReadOnly();
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