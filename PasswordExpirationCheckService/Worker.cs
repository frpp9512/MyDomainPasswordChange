using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyDomainPasswordChange.Management;
using MyDomainPasswordChange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordExpirationCheckService
{
    public class Worker : BackgroundService, IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly MyDomainPasswordManagement _passwordManagement;
        private readonly IMailNotificator _mailNotificator;
        private Timer _timer;
        private bool _running;

        public Worker(ILogger<Worker> logger,
                      IConfiguration configuration,
                      MyDomainPasswordManagement passwordManagement,
                      IMailNotificator mailNotificator)
        {
            _logger = logger;
            _configuration = configuration;
            _passwordManagement = passwordManagement;
            _mailNotificator = mailNotificator;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting the worker service for check and notify users about the password expiration.");

            _timer = new Timer(CheckPasswordExpiration, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));

            return base.StartAsync(cancellationToken);
        }

        private async void CheckPasswordExpiration(object state)
        {
            _logger.LogInformation("Timer check interval arrived.");
            if (!_running)
            {
                _logger.LogInformation("The password expiration service will check for the configured time.");
                var checkTime = _configuration.GetValue<string>("checkExpirationTime");
                if (!string.IsNullOrEmpty(checkTime))
                {
                    var checkTimeValue = DateTime.ParseExact(checkTime, "HH:mm", null);
                    var now = DateTime.Now;
                    if (checkTimeValue.Hour == now.Hour && checkTimeValue.Minute == now.Minute)
                    {
                        _logger.LogInformation("The configured time as arrived. Starting password expiration check!");
                        var expirationDays = _configuration.GetValue<double>("passwordExpirationDays");
                        var notificationThreshold = _configuration.GetValue<double>("expirationNotificationThreshold");
                        _logger.LogInformation("Loading users information from LDAP server...");
                        var users = await _passwordManagement.GetAllActiveUsersInfo();
                        _logger.LogInformation($"Loaded {users.Count} users information from LDAP server.");
                        _running = true;
                        foreach (var user in users)
                        {
                            if (!string.IsNullOrEmpty(user.Email))
                            {
                                var expirationDate = user.LastPasswordSet.AddDays(expirationDays);
                                if (expirationDate > now && now.AddDays(notificationThreshold) >= expirationDate)
                                {
                                    _logger.LogInformation($"The user {user.AccountName} has his password near to expiration. Sending notification.");
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

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Dispose();
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
