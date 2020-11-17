using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        private readonly MyDomainPasswordManagement _passwordManagement;

        private string AdminEmail => _configuration.GetValue<string>("adminEmail");
        private int PasswordExpirationDays => _configuration.GetValue<int>("passwordExpirationDays");
        private HttpContext HttpContext => _httpContextAccessor.HttpContext;

        public MailNotificator(IMyMailService mailService,
                               IConfiguration configuration,
                               IWebHostEnvironment webHostEnvironment,
                               IHttpContextAccessor httpContextAccessor,
                               MyDomainPasswordManagement passwordManagement)
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
                Subject = "Alerta de seguridad - Cambio de contraseña",
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

        public async Task SendChallengeAlertAsync()
        {
            await _mailService.SendMailAsync(new MailRequest
            {
                Body = GetChallengeAlertMailTemplate(),
                MailTo = AdminEmail,
                Subject = "Alerta de seguridad - Cambio de contraseña"
            });
        }

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
    }
}
