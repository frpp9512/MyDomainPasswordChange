using System;

namespace MyDomainPasswordChange.Management.Excepetions;

/// <summary>
/// Represents an error in user search process.
/// </summary>
public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message)
        : base(message)
    {

    }
}
