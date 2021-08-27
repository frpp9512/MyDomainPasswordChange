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
            template = template.Replace("{blacklist_reason}", reason switch
            {
                "challenge" => "se intentó enviar el fomulario respondiendo mal del desafío en múltiples ocasiones",
                "password" => "falló en escribir la contraseña de usuario en múltiples ocasiones",
                "mgmt_login" => "falló en acceder a la administración en múltiples ocasiones",
                _ => ""
            });
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

        public async Task SendManagementLoginFailAlertAsync() => await _mailService.SendMailAsync(new MailRequest
        {
            Body = GetManagementLoginFailedAlertTemplate(),
            MailTo = AdminEmail,
            Subject = "Intento de acceso a la administración - Cambio de contraseña",
            Important = true
        });

        private string GetManagementLoginFailedAlertTemplate()
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_admin_login_failed_alert_template.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            return template;
        }

        public async Task SendManagementLogin(UserInfo userInfo)
        {
            await _mailService.SendMailAsync(new MailRequest
            {
                Body = GetManagementLoginTemplate(userInfo),
                MailTo = AdminEmail,
                Subject = "Acceso a la administración - Cambio de contraseña"
            });
        }

        private string GetManagementLoginTemplate(UserInfo userInfo)
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_admin_login_template.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{accountName}", $"{userInfo.DisplayName} ({userInfo.Email})");
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            return template;
        }

        public async Task SendManagementUserPasswordResetted(UserInfo userInfo, (string name, string email) adminInfo)
            => await _mailService.SendMailAsync(new MailRequest
            {
                Body = GetManagementUserPasswordResettedTemplate(userInfo, adminInfo),
                MailTo = AdminEmail,
                Subject = "Contraseña reseteada por administrador - Cambio de contraseña"
            });

        private string GetManagementUserPasswordResettedTemplate(UserInfo userInfo, (string name, string email) adminInfo)
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_admin_user_password_resetted.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{adminAccountName}", $"{adminInfo.name} ({adminInfo.email})");
            template = template.Replace("{userAccountName}", $"{userInfo.DisplayName} ({userInfo.Email})");
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            return template;
        }

        public async Task SendManagementUserPasswordSetted(UserInfo userInfo, (string name, string email) adminInfo)
            => await _mailService.SendMailAsync(new MailRequest
            {
                Body = GetManagementUserPasswordSettedTemplate(userInfo, adminInfo),
                MailTo = AdminEmail,
                Subject = "Contraseña establecida por administrador - Cambio de contraseña"
            });

        private string GetManagementUserPasswordSettedTemplate(UserInfo userInfo, (string name, string email) adminInfo)
        {
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, $"templates{Path.DirectorySeparatorChar}mail_admin_user_password_setted.html");
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{adminAccountName}", $"{adminInfo.name} ({adminInfo.email})");
            template = template.Replace("{userAccountName}", $"{userInfo.DisplayName} ({userInfo.Email})");
            template = template.Replace("{requestIp}", HttpContext.Connection.RemoteIpAddress.ToString());
            var dateTime = DateTime.Now;
            template = template.Replace("{time}", dateTime.ToShortTimeString());
            template = template.Replace("{date}", dateTime.ToShortDateString());
            return template;
        }
    }
}