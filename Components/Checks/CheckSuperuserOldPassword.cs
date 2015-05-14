﻿using System;
using DotNetNuke.Entities.Users;

namespace DNN.Modules.SecurityAnalyzer.Components.Checks
{
    public class CheckSuperuserOldPassword : IAuditCheck
    {
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, "CheckSuperuserOldPassword");
            try
            {
                var totalRecords = 0;

                var superUsers = UserController.GetUsers(-1, 1, int.MaxValue, ref totalRecords, true, true);
                result.Severity = SeverityEnum.Pass;
                foreach (UserInfo user  in superUsers)
                {
                    if (DateTime.Now.AddMonths(-6) > user.Membership.LastPasswordChangeDate)
                    {
                        result.Severity = SeverityEnum.Warning;
                        result.Notes.Add("Superuser:" + user.Username);
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