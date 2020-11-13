using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public interface ICounterManager
    {
        void AddCounter(string key);
        void Count(string counterKey);
        uint GetCounterValue(string counterKey);
        void ResetCounter(string counterKey);
        void ResetAllCounters();
        void SetCounterAlarm(string counterKey, uint alarmValue, Action<string, uint> alarmCallback);
    }
}
