using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) 
            : base(message)
        {

        }
    }
}
