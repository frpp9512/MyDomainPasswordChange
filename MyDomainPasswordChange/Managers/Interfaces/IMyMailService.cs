using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Interfaces
{
    public interface IMyMailService
    {
        Task SendMailAsync(MailRequest request);
    }
}
