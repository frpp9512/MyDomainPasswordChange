using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordExpirationCheckService;

public class Worker(ILogger<Worker> logger,
                    IConfiguration configuration,
                    MyDomainPasswordManagement passwordManagement,
                    IMailNotificator mailNotificator) : BackgroundService, IDisposable
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly MyDomainPasswordManagement _passwordManagement = passwordManagement;
    private readonly IMailNotificator _mailNotificator = mailNotificator;
    private Timer _timer;
    private bool _running;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting the worker service for check and notify users about the password expiration.");
        _timer = new Timer(CheckPasswordExpiration, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
        return base.StartAsync(cancellationToken);
    }

    private async void CheckPasswordExpiration(object state)
    {
        _logger.LogInformation("Timer check interval arrived.");
        if (_running)
        {
            return;
        }

        _logger.LogInformation("The password expiration service will check for the configured time.");
        string checkTime = _configuration.GetValue<string>("checkExpirationTime");
        if (string.IsNullOrEmpty(checkTime))
        {
            await StopAsync(CancellationToken.None);
            return;
        }

        DateTime checkTimeValue = DateTime.ParseExact(checkTime, "HH:mm", null);
        DateTime now = DateTime.Now;
        if (checkTimeValue.Hour != now.Hour || checkTimeValue.Minute != now.Minute)
        {
            return;
        }

        _logger.LogInformation("The configured time as arrived. Starting password expiration check!");
        double expirationDays = _configuration.GetValue<double>("passwordExpirationDays");
        double notificationThreshold = _configuration.GetValue<double>("expirationNotificationThreshold");
        _logger.LogInformation("Loading users information from LDAP server...");
        System.Collections.Generic.List<MyDomainPasswordChange.Management.Models.UserInfo> users = await _passwordManagement.GetAllActiveUsersInfo();
        _logger.LogInformation($"Loaded {users.Count} users information from LDAP server.");
        _running = true;
        foreach (MyDomainPasswordChange.Management.Models.UserInfo user in users)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                continue;
            }

            DateTime expirationDate = user.LastPasswordSet.AddDays(expirationDays);
            if (expirationDate > now && now.AddDays(notificationThreshold) >= expirationDate)
            {
                _logger.LogInformation($"The user {user.AccountName} has his password near to expiration. Sending notification.");
                await _mailNotificator.SendExpirationNotificationAsync(user, expirationDate);
            }
        }

        _running = false;
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
