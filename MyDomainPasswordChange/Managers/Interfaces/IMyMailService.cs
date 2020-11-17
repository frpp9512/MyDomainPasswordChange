using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public interface IMyMailService
    {
        Task SendMailAsync(MailRequest request);
    }
}
