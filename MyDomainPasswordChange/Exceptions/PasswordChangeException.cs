﻿using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class PasswordChangeException : Exception
    {
        public PasswordChangeException(string message)
            : base(message)
        {

        }
    }
}
