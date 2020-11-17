using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public static class CounterHelpers
    {
        public static IServiceCollection AddCounterManager(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
            var counterManager = new CounterManager();
            counterManager.AddCounter(Counters.BadPasswordsTries);
            counterManager.AddCounter(Counters.BadChallengesTries);
            var passwordTriesAlarmValue = configuration.GetValue<uint>("badPasswordTriesAlarm");
            var challengeTriesAlarmValue = configuration.GetValue<uint>("badChallengeTriesAlarm");
            counterManager.SetCounterAlarm(Counters.BadPasswordsTries, passwordTriesAlarmValue, PasswordTriesAlarm);
            counterManager.SetCounterAlarm(Counters.BadChallengesTries, challengeTriesAlarmValue, ChallengeTriesAlarm);
            services.AddSingleton<ICounterManager>(counterManager);
            return services;
        }

        public static void PasswordTriesAlarm(string key, uint value)
        {

        }

        public static void ChallengeTriesAlarm(string key, uint value)
        {

        }
    }

    public static class Counters
    {
        public static string BadPasswordsTries => "badPasswordsTries";
        public static string BadChallengesTries => "badChallengesTries";
    }
}
