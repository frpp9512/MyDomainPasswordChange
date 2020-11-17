using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public interface IMailNotificator
    {
        Task SendChangePasswordNotificationAsync(string accountName);
        Task SendChangePasswordAlertAsync(string accountName);
        Task SendChallengeAlertAsync();
    }
}
