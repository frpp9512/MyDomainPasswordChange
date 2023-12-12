using MyDomainPasswordChange.Managers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyDomainPasswordChange.Managers.Services;

public class CounterManager : ICounterManager
{
    private readonly List<Counter> _counters = [];

    public void AddCounter(string key, string description = "")
    {
        if (!_counters.Any(c => c.Key == key))
        {
            _counters.Add(new Counter { Key = key, Description = description });
            return;
        }

        var counter = _counters.FirstOrDefault(c => c.Key == key);
        counter.Description = description;
        counter.Reset();
    }

    public bool ExistCounter(string counterKey) => _counters.Any(c => c.Key == counterKey);

    public void Count(string counterKey)
    {
        if (_counters.Any(c => c.Key == counterKey))
        {
            var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
            counter.Count();
            return;
        }

        AddCounter(counterKey);
        Count(counterKey);
    }

    public uint GetCounterValue(string counterKey)
    {
        if (_counters.Any(c => c.Key == counterKey))
        {
            var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
            return counter.Value;
        }

        throw new KeyNotFoundException("El contador espeficado no existe.");
    }

    public void ResetAllCounters() => _counters.ForEach(c => c.Reset());

    public void ResetCounter(string counterKey)
    {
        if (_counters.Any(c => c.Key == counterKey))
        {
            var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
            counter.Reset();
        }

        throw new KeyNotFoundException("El contador espeficado no existe.");
    }

    public void SetCounterAlarm(string counterKey, uint alarmValue, Action<string, uint> alarmCallback)
    {
        if (!_counters.Any(c => c.Key == counterKey))
        {
            throw new KeyNotFoundException("El contador espeficado no existe.");
        }

        var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
        if (counter.Alarm is not null)
        {
            counter.Alarm.AlarmValue = alarmValue;
            return;
        }

        counter.Alarm = new CounterAlarm
        {
            AlarmValue = alarmValue,
            AlarmCallback = alarmCallback
        };
    }

    public bool IsCounterAlarming(string counterKey)
    {
        if (!_counters.Any(c => c.Key == counterKey))
        {
            throw new KeyNotFoundException("El contador espeficado no existe.");
        }

        var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
        return counter.Alarm is not null && counter.Alarming;
    }

    public void RemoveCounter(string counterKey)
    {
        if (!_counters.Any(c => c.Key == counterKey))
        {
            return;
        }

        var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
        _ = _counters.Remove(counter);
    }

    public void RemoveAllCounters() => _counters.Clear();

    public DateTime GetCounterLastCount(string counterKey)
    {
        if (!_counters.Any(c => c.Key == counterKey))
        {
            return DateTime.MinValue;
        }

        var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
        return counter.LastCount;
    }

    public bool HasCounted(string counterKey) => _counters.Any(c => c.HasCounted);

    public bool HasAlarm(string counterKey)
    {
        if (!_counters.Any(c => c.Key == counterKey))
        {
            throw new KeyNotFoundException("El contador espeficado no existe.");
        }

        var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
        return counter.HasAlarm;
    }
}

internal class Counter
{
    public string Key { get; set; }
    public uint Value { get; set; }
    public string Description { get; set; }
    public DateTime LastCount { get; set; } = DateTime.MinValue;
    public CounterAlarm Alarm { get; set; }
    public bool HasAlarm => Alarm is not null;
    public bool Alarming => Alarm is not null && Value >= Alarm.AlarmValue;
    public bool HasCounted => Value > 0;

    public void Count()
    {
        Value++;
        LastCount = DateTime.Now;
        if (Alarming)
        {
            Alarm.AlarmCallback(Key, Value);
        }
    }

    public void Reset()
    {
        Value = 0;
        LastCount = DateTime.MinValue;
    }
}

internal class CounterAlarm
{
    public uint AlarmValue { get; set; }
    public Action<string, uint> AlarmCallback { get; set; }
}
