using System;

namespace YpSecurity
{
    public class LogonEventArgs : EventArgs
    {
        public LogonEventArgs(int logonTimes, DateTime logonAuthDateTime)
        {
            LogonTimes = logonTimes;
            AuthDateTime = logonAuthDateTime;
        }

        public int LogonTimes { get; }

        public DateTime AuthDateTime { get; }
    }
}
