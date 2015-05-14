﻿using System;
using DotNetNuke.Entities.Portals;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckSiteRegistration : IAuditCheck
    {
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckSiteRegistration");
            try
            {
                var portalController = new PortalController();
                result.Severity = SeverityEnum.Pass;
                foreach (PortalInfo portal in portalController.GetPortals())
                {
                    //check for public registration
                    if (portal.UserRegistration == 2)
                    {
                        result.Severity = SeverityEnum.Warning;
                        result.Notes.Add("Portal:" + portal.PortalName);
                    }
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