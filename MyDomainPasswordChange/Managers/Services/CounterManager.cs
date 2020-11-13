using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class CounterManager : ICounterManager
    {
        private List<Counter> _counters = new List<Counter>();

        public void AddCounter(string key) => _counters.Add(new Counter { Key = key });

        public void Count(string counterKey)
        {
            if (_counters.Any(c => c.Key == counterKey))
            {
                var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
                counter.Count();
            }
            else
            {
                throw new KeyNotFoundException("El contador espeficado no existe.");
            }
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
            else
            {
                throw new KeyNotFoundException("El contador espeficado no existe.");
            }
        }

        public void SetCounterAlarm(string counterKey, uint alarmValue, Action<string, uint> alarmCallback)
        {
            if (_counters.Any(c => c.Key == counterKey))
            {
                var counter = _counters.FirstOrDefault(c => c.Key == counterKey);
                if (counter.Alarm is not null)
                {
                    counter.Alarm.AlarmValue = alarmValue;
                }
                else
                {
                    counter.Alarm = new CounterAlarm
                    {
                        AlarmValue = alarmValue,
                        AlarmCallback = alarmCallback
                    };
                }
            }
            else
            {
                throw new KeyNotFoundException("El contador espeficado no existe.");
            }
        }
    }

    class Counter
    {
        public string Key { get; set; }
        public uint Value { get; set; }
        public CounterAlarm Alarm { get; set; }

        public void Count()
        {
            Value++;
            if (Alarm is not null && Value >= Alarm.AlarmValue)
            {
                Alarm.AlarmCallback(Key, Value);
            }
        }

        public void Reset() => Value = 0;
    }

    class CounterAlarm
    {
        public uint AlarmValue { get; set; }
        public Action<string, uint> AlarmCallback { get; set; }
    }
}
