using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class MailNotificator : IMailNotificator
    {
        private readonly IMyMailService _mailService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDomainPasswordManagement _passwordManagement;

        private string AdminEmail => _configuration.GetValue<string>("adminEmail");
        private int PasswordExpirationDays => _configuration.GetValue<int>("passwordExpirationDays");
        private HttpContext HttpContext => _httpContextAccessor.HttpContext;

        public MailNotificator(IMyMailService mailService,
                               IConfiguration configuration,
                               IWebHostEnvironment webHostEnvironment,
                               IHttpContextAccessor httpContextAccessor,
                               IDomainPasswordManagement passwordManagement)
        {
            _mailService = mailService;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _passwordManagement = passwordManagement;
        }

        public async Task SendChangePasswordAlertAsync(string accountName)
        {
            var user = _passwordManagement.GetUserInfo(accountName);
            await _mailService.SendMailAsync(new MailRequest
            {
                Body = GetAlertMailTemplate(user.DisplayName),
                MailTo = user.Email ?? AdminEmail,
                Cc = !String.IsNullOrEmpty(user.Email) ? AdminEmail : "",
                Subject = "Alerta de contraseña - Cambio de contraseña",
                Important = true
            });
        }

        public async Task SendChangePasswordNotificationAsync(string accountName)
        {
            var user = _passwordManagement.GetUserInfo(accountName);
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _mailService.SendMailAsync(new MailRequest
                {
                    Body = GetNotificationMailTemplate(user.DisplayName, PasswordExpirationDays),
                    MailTo = user.Email,
                    Subject = "Notificación - Cambio de contraseña"
                });
            }
        }

        public async Task SendChallengeAlertAsync() => await _mailService.SendMailAsync(new MailRequest
        {
            Body = GetChallengeAlertMailTemplate(),
            MailTo = AdminEmail,
            Subject = "Alerta del desafío - Cambio de contraseña",
            Important = true
        });

        public async Task SendBlacklistAlertAsync(string reason) => await _mailService.SendMailAsync(new MailRequest
        {
            Body = GetIpBlacklistMailTemplate(reason),
            MailTo = AdminEmail,
            Subject = "Alerta de lista negra - Cambio de contraseña",
            Important = true
        });

        private string GetNotificationMailTemplate(string accountName, int expirationDays)
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_notification_template.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{accountName}", accountName);
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            template = template.Replace("{expirationDays}", expirationDays.ToString());
            var expirationDate = dateTime.AddDays(expirationDays);
            template = template.Replace("{expirationDate}", expirationDate.ToShortDateString());
            return template;
        }

        private string GetAlertMailTemplate(string accountName)
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_alert_template.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{accountName}", accountName);
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            return template;
        }

        private string GetChallengeAlertMailTemplate()
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_challenge_alert_template.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            return template;
        }

        private string GetIpBlacklistMailTemplate(string reason)
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_ip_blacklisted_template.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            switch (reason)
            {
                case "challenge":
                    template = template.Replace("{blacklist_reason}", "se intentó enviar el fomulario respondiendo mal del desafío en múltiples ocasiones");
                    break;
                case "password":
                    template = template.Replace("{blacklist_reason}", "falló en escribir la contraseña de usuario en múltiples ocasiones");
                    break;
                default:
                    break;
            }
            return template;
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
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_expiration_notification_template.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{displayName}", userInfo.DisplayName);
            var dateTime = DateTime.Now;
            template = template.Replace("{expirationDays}", (expirationDate - dateTime).Days.ToString());
            template = template.Replace("{expirationDate}", expirationDate.ToShortDateString());
            template = template.Replace("{accountName}", userInfo.AccountName);
            return template;
        }
    }
}
