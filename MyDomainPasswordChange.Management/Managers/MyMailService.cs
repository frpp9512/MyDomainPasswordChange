﻿using MailKit.Net.Smtp;
using MimeKit;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management.Managers;

/// <summary>
/// Represents a email sender service.
/// </summary>
public class MyMailService : IMyMailService
{
    private readonly IMailSettingsProvider _settingsProvider;

    private readonly MailSettings _settings;
    private readonly Queue<MailRequest> _mailQueue;
    private readonly Timer _timer;
    private bool _sendingMails = false;
    private DateTime _lastEmailSended = DateTime.MinValue;

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
            var now = DateTime.Now;
            if ((now - _lastEmailSended).TotalSeconds > settings.MailIntervalInSeconds * 1.30)
            {
                _sendingMails = true;
                var mailSended = 0;
                while (_mailQueue.Any() && mailSended < settings.MaxMailPerInterval)
                {
                    try
                    {
                        await SendMail(_mailQueue.Dequeue());
                        mailSended++;
                    }
                    catch (SmtpCommandException ex)
                    {
                        Console.WriteLine($"Exception founded sending email: {ex.Message}");
                        return;
                    }
                }

                _lastEmailSended = DateTime.Now;
                _sendingMails = false;
            }
        }
    }

    /// <summary>
    /// Send the specified <see cref="MailRequest"/>.
    /// </summary>
    /// <param name="request">The sending email data.</param>
    /// <returns></returns>
    private async Task SendMail(MailRequest request)
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
        _ = await smtp.SendAsync(email);
        smtp.Disconnect(true);
    }

    public Task SendMailAsync(MailRequest request)
    {
        _mailQueue.Enqueue(request);
        return Task.CompletedTask;
    }
}
