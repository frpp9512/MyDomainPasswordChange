using System;

namespace MyDomainPasswordChange.Management.Excepetions;

/// <summary>
/// Represents an error with the password change process.
/// </summary>
public class PasswordChangeException : Exception
{
    public PasswordChangeException(string message)
        : base(message)
    {

    }

    public PasswordChangeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
