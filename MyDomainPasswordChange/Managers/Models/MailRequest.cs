﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class MailRequest
    {
        public string MailTo { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
