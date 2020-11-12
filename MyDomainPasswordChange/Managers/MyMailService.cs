using MailKit.Net.Smtp;
using MimeKit;
using MyDomainPasswordChange.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Managers
{
    public class MyMailService : IMyMailService
    {
        private readonly IMailSettingsProvider _settings;

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
            var builder = new BodyBuilder
            {
                HtmlBody = request.Body
            };
            email.Body = builder.ToMessageBody();
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
