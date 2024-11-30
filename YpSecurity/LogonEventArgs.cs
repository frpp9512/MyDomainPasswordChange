using System;

namespace YpSecurity;

public class LogonEventArgs(int logonTimes, DateTime logonAuthDateTime) : EventArgs
{
    public int LogonTimes { get; } = logonTimes;
    public DateTime AuthDateTime { get; } = logonAuthDateTime;
}
