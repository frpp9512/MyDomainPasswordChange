using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Managers.Interfaces;
using System;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Managers.Services;

public class AlertCountingManagement(IHttpContextAccessor contextAccessor,
                                     IConfiguration configuration,
                                     IMailNotificator notificator,
                                     IIpAddressBlacklist blacklist) : IAlertCountingManagement
{
    private readonly ICounterManager _counterManager = new CounterManager();
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMailNotificator _notificator = notificator;
    private readonly IIpAddressBlacklist _blacklist = blacklist;
    private readonly double _alarmRefresh = 5;

    public async Task CountChallengeFailAsync()
    {
        await CountForChallengeAlertAsync();
        await CountForChallengeOffenseAsync();
    }

    private async Task CountForChallengeAlertAsync()
    {
        var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        var counterAlertKey = $"challenge-alert-{remoteIp}";
        if (!_counterManager.ExistCounter(counterAlertKey))
        {
            _counterManager.AddCounter(counterAlertKey, "");
            await CountForChallengeAlertAsync();
            return;
        }

        if (!_counterManager.HasAlarm(counterAlertKey))
        {
            _counterManager.SetCounterAlarm(counterAlertKey,
                                            GetBadChallengeTriesAlarm(),
                                            async (key, tries) => await _notificator.SendChallengeAlertAsync());
        }
        if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
        {
            _counterManager.ResetCounter(counterAlertKey);
        }

        _counterManager.Count(counterAlertKey);
    }

    private async Task CountForChallengeOffenseAsync()
    {
        var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        var counterAlertKey = $"challenge-offense-{remoteIp}";
        if (!_counterManager.ExistCounter(counterAlertKey))
        {
            _counterManager.AddCounter(counterAlertKey, "");
            await CountChallengeFailAsync();
            return;
        }

        if (!_counterManager.HasAlarm(counterAlertKey))
        {
            _counterManager.SetCounterAlarm(counterAlertKey,
                                            GetBadChallengeTriesOffense(),
                                            async (key, tries) => { await _blacklist.AddIpAddressAsync(remoteIp, "challenge"); await _notificator.SendBlacklistAlertAsync("challenge"); });
        }

        if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
        {
            _counterManager.ResetCounter(counterAlertKey);
        }

        _counterManager.Count(counterAlertKey);
    }

    public async Task CountPasswordFailAsync(string accountName)
    {
        await CountForPasswordAlert(accountName);
        await CountForPasswordOffense(accountName);
    }

    private async Task CountForPasswordAlert(string accountName)
    {
        var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        var counterAlertKey = $"password-alert-{accountName}-{remoteIp}";
        if (!_counterManager.ExistCounter(counterAlertKey))
        {
            _counterManager.AddCounter(counterAlertKey, "");
            await CountForPasswordAlert(accountName);
            return;
        }

        if (!_counterManager.HasAlarm(counterAlertKey))
        {
            _counterManager.SetCounterAlarm(counterAlertKey,
                                            GetBadPasswordTriesAlarm(),
                                            async (key, tries) => await _notificator.SendChangePasswordAlertAsync(accountName));
        }

        if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
        {
            _counterManager.ResetCounter(counterAlertKey);
        }

        _counterManager.Count(counterAlertKey);
    }

    private async Task CountForPasswordOffense(string accountName)
    {
        var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        var counterAlertKey = $"password-offense-{accountName}-{remoteIp}";
        if (!_counterManager.ExistCounter(counterAlertKey))
        {
            _counterManager.AddCounter(counterAlertKey, "");
            await CountChallengeFailAsync();
            return;
        }

        if (!_counterManager.HasAlarm(counterAlertKey))
        {
            _counterManager.SetCounterAlarm(counterAlertKey,
                                            GetBadPasswordTriesOffense(),
                                            async (key, tries) =>
                                            {
                                                await _blacklist.AddIpAddressAsync(remoteIp, "password");
                                                await _notificator.SendBlacklistAlertAsync("password");
                                            });
        }

        if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
        {
            _counterManager.ResetCounter(counterAlertKey);
        }

        _counterManager.Count(counterAlertKey);
    }

    private uint GetBadPasswordTriesAlarm() => _configuration.GetValue<uint>("badPasswordTriesAlarm");

    private uint GetBadPasswordTriesOffense() => _configuration.GetValue<uint>("badPasswordTriesOffense");

    private uint GetBadChallengeTriesAlarm() => _configuration.GetValue<uint>("badChallengeTriesAlarm");

    private uint GetBadChallengeTriesOffense() => _configuration.GetValue<uint>("badChallengeTriesOffense");

    public async Task CountManagementAuthFailAsync()
    {
        await CountForLoginFailAlertAsync();
        await CountForLoginOffenseAsync();
    }

    private async Task CountForLoginFailAlertAsync()
    {
        var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        var counterAlertKey = $"auth-failed-{remoteIp}";
        if (!_counterManager.ExistCounter(counterAlertKey))
        {
            _counterManager.AddCounter(counterAlertKey, "");
            await CountForLoginFailAlertAsync();
            return;
        }

        if (!_counterManager.HasAlarm(counterAlertKey))
        {
            _counterManager.SetCounterAlarm(counterAlertKey, 3, (key, tries) => _notificator.SendManagementLoginFailAlertAsync());
        }

        if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
        {
            _counterManager.ResetCounter(counterAlertKey);
        }

        _counterManager.Count(counterAlertKey);
    }

    private async Task CountForLoginOffenseAsync()
    {
        var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        var counterAlertKey = $"auth-offense-{remoteIp}";
        if (!_counterManager.ExistCounter(counterAlertKey))
        {
            _counterManager.AddCounter(counterAlertKey, "");
            await CountForLoginOffenseAsync();
            return;
        }

        if (!_counterManager.HasAlarm(counterAlertKey))
        {
            _counterManager.SetCounterAlarm(counterAlertKey, 5, async (key, tries) =>
            {
                await _blacklist.AddIpAddressAsync(remoteIp, "password");
                await _notificator.SendBlacklistAlertAsync("mgmt_login");
            });
        }

        if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
        {
            _counterManager.ResetCounter(counterAlertKey);
        }

        _counterManager.Count(counterAlertKey);
    }
}