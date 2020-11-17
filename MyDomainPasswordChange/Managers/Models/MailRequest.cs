using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class MailRequest
    {
        public string MailTo { get; set; }
        public string Cc { get; set; }
        public string Subject { get; set; }
        public bool Important { get; set; }
        public string Body { get; set; }
    }
}
