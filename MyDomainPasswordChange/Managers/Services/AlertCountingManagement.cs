using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;

namespace MyDomainPasswordChange
{
    public class AlertCountingManagement : IAlertCountingManagement
    {
        private readonly ICounterManager _counterManager;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IMailNotificator _notificator;
        private readonly IIpAddressBlacklist _blacklist;
        private readonly double _alarmRefresh = 5;

        public AlertCountingManagement(IHttpContextAccessor contextAccessor,
                                       IConfiguration configuration,
                                       IMailNotificator notificator,
                                       IIpAddressBlacklist blacklist)
        {
            _counterManager = new CounterManager();
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _notificator = notificator;
            _blacklist = blacklist;
        }

        public void CountChallengeFail()
        {
            CountForChallengeAlert();
            CountForChallengeOffense();
        }

        private void CountForChallengeAlert()
        {
            var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            var counterAlertKey = $"challenge-alert-{remoteIp}";
            if (_counterManager.ExistCounter(counterAlertKey))
            {
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
            else
            {
                _counterManager.AddCounter(counterAlertKey, "");
                CountChallengeFail();
            }
        }

        private void CountForChallengeOffense()
        {
            var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            var counterAlertKey = $"challenge-offense-{remoteIp}";
            if (_counterManager.ExistCounter(counterAlertKey))
            {
                if (!_counterManager.HasAlarm(counterAlertKey))
                {
                    _counterManager.SetCounterAlarm(counterAlertKey,
                                                    GetBadChallengeTriesOffense(),
                                                    async (key, tries) => { _blacklist.AddIpAddressToBlacklist(remoteIp, "challenge"); await _notificator.SendBlacklistAlertAsync("challenge"); });
                }
                if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
                {
                    _counterManager.ResetCounter(counterAlertKey);
                }
                _counterManager.Count(counterAlertKey);
            }
            else
            {
                _counterManager.AddCounter(counterAlertKey, "");
                CountChallengeFail();
            }
        }

        public void CountPasswordFail(string accountName)
        {
            CountForPasswordAlert(accountName);
            CountForPasswordOffense(accountName);
        }

        private void CountForPasswordAlert(string accountName)
        {
            var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            var counterAlertKey = $"password-alert-{accountName}-{remoteIp}";
            if (_counterManager.ExistCounter(counterAlertKey))
            {
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
            else
            {
                _counterManager.AddCounter(counterAlertKey, "");
                CountChallengeFail();
            }
        }

        private void CountForPasswordOffense(string accountName)
        {
            var remoteIp = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            var counterAlertKey = $"password-offense-{accountName}-{remoteIp}";
            if (_counterManager.ExistCounter(counterAlertKey))
            {
                if (!_counterManager.HasAlarm(counterAlertKey))
                {
                    _counterManager.SetCounterAlarm(counterAlertKey,
                                                    GetBadPasswordTriesOffense(),
                                                    async (key, tries) => { _blacklist.AddIpAddressToBlacklist(remoteIp, "password"); await _notificator.SendBlacklistAlertAsync("password"); });
                }
                if ((DateTime.Now - _counterManager.GetCounterLastCount(counterAlertKey)).TotalMinutes > _alarmRefresh)
                {
                    _counterManager.ResetCounter(counterAlertKey);
                }
                _counterManager.Count(counterAlertKey);
            }
            else
            {
                _counterManager.AddCounter(counterAlertKey, "");
                CountChallengeFail();
            }
        }

        private uint GetBadPasswordTriesAlarm() => _configuration.GetValue<uint>("badPasswordTriesAlarm");
                
        private uint GetBadPasswordTriesOffense() => _configuration.GetValue<uint>("badPasswordTriesOffense");
                
        private uint GetBadChallengeTriesAlarm() => _configuration.GetValue<uint>("badChallengeTriesAlarm");
                
        private uint GetBadChallengeTriesOffense() => _configuration.GetValue<uint>("badChallengeTriesOffense");
    }
}