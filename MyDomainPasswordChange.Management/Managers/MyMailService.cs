using MailKit.Net.Smtp;
using MimeKit;
using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Managers
{
    /// <summary>
    /// Represents a email sender service.
    /// </summary>
    public class MyMailService : IMyMailService
    {
        private readonly IMailSettingsProvider _settingsProvider;
        
        private MailSettings _settings;
        private Queue<MailRequest> _mailQueue;
        private Timer _timer;
        private bool _sendingMails = false;
        private DateTime _lastEmailSended = DateTime.MinValue;
        private int _refreshConfigCounter = 0;

        /// <summary>
        /// Creates a new instance of the <see cref="MyMailService"/>.
        /// </summary>
        /// <param name="settingsProvider">The <see cref="IMailSettingsProvider"/> implementation to access the mail settings..</param>
        public MyMailService(IMailSettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
            _settings = _settingsProvider.GetMailSettings();
            _mailQueue = new Queue<MailRequest>();
            
            _timer = new Timer(SendMails, null, TimeSpan.Zero, TimeSpan.FromSeconds(_settings.RefreshQueueInterval));
        }

        /// <summary>
        /// [Callback method for the timer]
        /// Sends the mail in the queue.
        /// </summary>
        /// <param name="state"></param>
        private async void SendMails(object state)
        {
            if (!_sendingMails && _mailQueue.Any())
            {
                var settings = _settingsProvider.GetMailSettings();
                if ((DateTime.Now - _lastEmailSended).TotalSeconds > settings.MailIntervalInSeconds)
                {
                    _sendingMails = true;
                    var mailSended = 0;
                    while (_mailQueue.Any() && mailSended < settings.MaxMailPerInterval)
                    {
                        await SendQueuedMail(_mailQueue.Dequeue());
                        mailSended++;
                    }
                    _lastEmailSended = DateTime.Now;
                    _sendingMails = false; 
                }
            }

            // Update the counter for refresh the configuration
            _refreshConfigCounter++;
            // Check if the conter arrives to the setting and if it does, refresh the configuration
            if (_refreshConfigCounter >= _settings.RefreshConfigurationEvery)
            {
                _settings = _settingsProvider.GetMailSettings();
            }
        }

        /// <summary>
        /// Send the specified <see cref="MailRequest"/>.
        /// </summary>
        /// <param name="request">The sending email data.</param>
        /// <returns></returns>
        private async Task SendQueuedMail(MailRequest request)
        {
            var email = new MimeMessage
            {
                Sender = MailboxAddress.Parse(_settings.MailAddress),
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
            smtp.Connect(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.MailAddress, _settings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        public Task SendMailAsync(MailRequest request)
        {
            _mailQueue.Enqueue(request);
            return Task.CompletedTask;
        }
    }
}
