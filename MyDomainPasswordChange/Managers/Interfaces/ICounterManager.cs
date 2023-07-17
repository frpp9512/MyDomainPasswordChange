using System;

namespace MyDomainPasswordChange.Managers.Interfaces;

public interface ICounterManager
{
    void AddCounter(string key, string description);
    bool ExistCounter(string counterKey);
    void Count(string counterKey);
    uint GetCounterValue(string counterKey);
    DateTime GetCounterLastCount(string counterKey);
    bool HasCounted(string counterKey);
    void ResetCounter(string counterKey);
    void ResetAllCounters();
    void RemoveCounter(string counterKey);
    void RemoveAllCounters();
    void SetCounterAlarm(string counterKey, uint alarmValue, Action<string, uint> alarmCallback);
    bool IsCounterAlarming(string counterKey);
    bool HasAlarm(string counterKey);
}
