using MailKit.Net.Smtp;
using MimeKit;
using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Managers
{
    /// <summary>
    /// Represents a email sender service.
    /// </summary>
    public class MyMailService : IMyMailService
    {
        private readonly IMailSettingsProvider _settings;

        /// <summary>
        /// Creates a new instance of the <see cref="MyMailService"/>.
        /// </summary>
        /// <param name="settings">The <see cref="IMailSettingsProvider"/> implementation to access the mail settings..</param>
        public MyMailService(IMailSettingsProvider settings)
        {
            _settings = settings;
        }

        public async Task SendMailAsync(MailRequest request)
        {
            var mailSettings = _settings.GetMailSettings();
            var email = new MimeMessage
            {
                Sender = MailboxAddress.Parse(mailSettings.MailAddress),
                Subject = request.Subject,
            };
            email.Sender.Name = "Servicio de cambio de contraseñas - INGECO";
            email.To.Add(MailboxAddress.Parse(request.MailTo));
            if (!string.IsNullOrEmpty(request.Cc))
            {
                email.Cc.Add(MailboxAddress.Parse(request.Cc));
            }
            var builder = new BodyBuilder
            {
                HtmlBody = request.Body
            };
            email.Body = builder.ToMessageBody();
            if (request.Important)
            {
                email.Importance = MessageImportance.High;
            }
            using var smtp = new SmtpClient
            {
                ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };
            smtp.Connect(mailSettings.Host, mailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(mailSettings.MailAddress, mailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }
    }
}
