using System;

namespace MyDomainPasswordChange.Management.Excepetions;

/// <summary>
/// Represents an error with the password authentication.
/// </summary>
public class BadPasswordException : Exception
{
    public BadPasswordException(string message)
        : base(message)
    {

    }
}
