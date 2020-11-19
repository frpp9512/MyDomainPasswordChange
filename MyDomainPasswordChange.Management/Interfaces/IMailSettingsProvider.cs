using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
    /// <summary>
    /// Defines a class that provides the email general settigns.
    /// </summary>
    public interface IMailSettingsProvider
    {
        /// <summary>
        /// Provides the general settings for email sending.
        /// </summary>
        /// <returns>The settings needed for email sending.</returns>
        MailSettings GetMailSettings();
    }
}
