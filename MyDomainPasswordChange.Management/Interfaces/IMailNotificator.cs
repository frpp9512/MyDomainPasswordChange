using MyDomainPasswordChange.Management.Models;
using System;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management.Interfaces;

public interface IMailNotificator
{
    Task SendChangePasswordNotificationAsync(string accountName);
    Task SendChangePasswordAlertAsync(string accountName);
    Task SendChallengeAlertAsync();
    Task SendBlacklistAlertAsync(string reason);
    Task SendExpirationNotificationAsync(UserInfo userInfo, DateTime expirationDate);
    Task SendManagementLoginFailAlertAsync();
    Task SendManagementLogin(UserInfo userInfo);
    Task SendManagementUserPasswordResetted(UserInfo userInfo, (string name, string email) adminInfo);
    Task SendManagementUserPasswordSetted(UserInfo userInfo, (string name, string email) adminInfo);
}