using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordExpirationCheckService
{
    public class MailNotificator : IMailNotificator
    {
        private readonly IMyMailService _mailService;
        private readonly IConfiguration _configuration;
        private readonly MyDomainPasswordManagement _passwordManagement;

        private string AdminEmail => _configuration.GetValue<string>("adminEmail");
        private int PasswordExpirationDays => _configuration.GetValue<int>("passwordExpirationDays");

        public MailNotificator(IMyMailService mailService,
                               IConfiguration configuration,
                               MyDomainPasswordManagement passwordManagement)
        {
            _mailService = mailService;
            _configuration = configuration;
            _passwordManagement = passwordManagement;
        }

        public async Task SendExpirationNotificationAsync(UserInfo userInfo, DateTime expirationDate) => await _mailService.SendMailAsync(new MailRequest
        {
            Body = GetExpirationAlertMailTemplate(userInfo, expirationDate),
            MailTo = userInfo.Email,
            Subject = "Su contraseña expirará pronto - Cambio de contraseña",
            Important = true
        });

        private string GetExpirationAlertMailTemplate(UserInfo userInfo, DateTime expirationDate)
        {
            var templatePath = _configuration.GetValue<string>("emailTemplatePath");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{displayName}", userInfo.DisplayName);
            var dateTime = DateTime.Now;
            template = template.Replace("{expirationDays}", (expirationDate - dateTime).Days.ToString());
            template = template.Replace("{expirationDate}", expirationDate.ToShortDateString());
            template = template.Replace("{accountName}", userInfo.AccountName);
            return template;
        }

        public Task SendChangePasswordNotificationAsync(string accountName) => throw new NotImplementedException();
        public Task SendChangePasswordAlertAsync(string accountName) => throw new NotImplementedException();
        public Task SendChallengeAlertAsync() => throw new NotImplementedException();
        public Task SendBlacklistAlertAsync(string reason) => throw new NotImplementedException();

        public Task SendManagementLoginFailAlertAsync()
        {
            throw new NotImplementedException();
        }

        public Task SendManagementLogin(UserInfo userInfo)
        {
            throw new NotImplementedException();
        }

        public Task SendManagementUserPasswordResetted(UserInfo userInfo, (string name, string email) adminInfo)
        {
            throw new NotImplementedException();
        }

        public Task SendManagementUserPasswordSetted(UserInfo userInfo, (string name, string email) adminInfo)
        {
            throw new NotImplementedException();
        }
    }
}