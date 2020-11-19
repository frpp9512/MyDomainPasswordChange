using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
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
}
