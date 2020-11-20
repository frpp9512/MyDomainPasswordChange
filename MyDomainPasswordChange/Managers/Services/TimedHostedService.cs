using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<TimedHostedService> _logger;
        private readonly IConfiguration _configuration;
        private readonly MyDomainPasswordManagement _passwordManagement;
        private readonly IMailNotificator _mailNotificator;
        private Timer _timer;
        private bool _running;

        public TimedHostedService(ILogger<TimedHostedService> logger,
                                  IConfiguration configuration,
                                  MyDomainPasswordManagement passwordManagement,
                                  IMailNotificator mailNotificatior)
        {
            _logger = logger;
            _configuration = configuration;
            _passwordManagement = passwordManagement;
            _mailNotificator = mailNotificatior;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Started background timed service.");

            _timer = new Timer(CheckPasswordExpiration, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void CheckPasswordExpiration(object state)
        {
            if (!_running)
            {
                _logger.LogInformation("Starting to check the expiration date of users.");
                var checkTime = _configuration.GetValue<string>("checkExpirationTime");
                if (!string.IsNullOrEmpty(checkTime))
                {
                    var checkTimeValue = DateTime.ParseExact(checkTime, "HH:mm", null);
                    var now = DateTime.Now;
                    if (checkTimeValue.Hour == now.Hour && checkTimeValue.Minute == now.Minute)
                    {
                        var expirationDays = _configuration.GetValue<double>("passwordExpirationDays");
                        var notificationThreshold = _configuration.GetValue<double>("expirationNotificationThreshold");
                        var users = await _passwordManagement.GetAllActiveUsersInfo();
                        _running = true;
                        foreach (var user in users)
                        {
                            if (!string.IsNullOrEmpty(user.Email))
                            {
                                var expirationDate = user.LastPasswordSet.AddDays(expirationDays);
                                if (expirationDate > now && now.AddDays(notificationThreshold) >= expirationDate)
                                {
                                    await _mailNotificator.SendExpirationNotificationAsync(user, expirationDate);
                                }
                            }
                        }
                        _running = false;
                    }
                }
                else
                {
                    await StopAsync(CancellationToken.None);
                } 
            }
        }

        public void Dispose() => _timer?.Dispose();
    }
}
