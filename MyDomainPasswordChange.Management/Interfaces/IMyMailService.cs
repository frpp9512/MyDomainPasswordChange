using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    /// <summary>
    /// Defines a class with the capability of send emails.
    /// </summary>
    public interface IMyMailService
    {
        /// <summary>
        /// Send the email defined by the specified <see cref="MailRequest"/> data.
        /// </summary>
        /// <param name="request">The data of the mail.</param>
        /// <returns>The async task of the email sending process.</returns>
        Task SendMailAsync(MailRequest request);
    }
}
