﻿using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
    public interface IMailNotificator
    {
        Task SendChangePasswordNotificationAsync(string accountName);
        Task SendChangePasswordAlertAsync(string accountName);
        Task SendChallengeAlertAsync();
        Task SendBlacklistAlertAsync(string reason);
        Task SendExpirationNotificationAsync(UserInfo userInfo, DateTime expirationDate);
    }
}
