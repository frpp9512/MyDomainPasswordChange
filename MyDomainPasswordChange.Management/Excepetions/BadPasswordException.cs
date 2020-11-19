using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
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
}
