using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
    /// <summary>
    /// Represents the necessary settings for email sending.
    /// </summary>
    public class MailSettings
    {
        /// <summary>
        /// The sender email address.
        /// </summary>
        public string MailAddress { get; set; }

        /// <summary>
        /// The Display name used for the sender email address.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The password of the email account.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The email server that hosts the email account.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The server communication port for sending emails.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Defines the interval to refresh the email queue.
        /// </summary>
        public int RefreshQueueInterval { get; set; }

        /// <summary>
        /// Defines that the configuration should be updated every X queue refresh updates.
        /// </summary>
        public int RefreshConfigurationEvery { get; set; }

        /// <summary>
        /// The maximun sending email amount per time interval.
        /// </summary>
        public int MaxMailPerInterval { get; set; }

        /// <summary>
        /// The time interval for the maximun amount of 
        /// </summary>
        public double MailIntervalInSeconds { get; set; }
    }
}
