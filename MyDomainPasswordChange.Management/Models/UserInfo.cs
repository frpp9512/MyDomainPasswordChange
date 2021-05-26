using MyDomainPasswordChange.Management.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
    /// <summary>
    /// Represents the general info of a LDAP User object.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// The account name (sAMAccountName) of the user.
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// The display name of the user.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The description of the user.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The user's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The last time when the password was setted.
        /// </summary>
        public DateTime LastPasswordSet { get; set; }

        /// <summary>
        /// Defines if the user doesn't need to change the password periodically.
        /// </summary>
        public bool PasswordNeverExpires { get; set; }

        /// <summary>
        /// <see langword="true"/> if the user is enabled in the Active Directory.
        /// </summary>
        public bool Enabled { get; internal set; }

        /// <summary>
        /// <see langword="true"/> if the user belongs to the Domain Admin's group.
        /// </summary>
        public bool IsDomainAdmin => Groups.Any(g => g.AccountName == "Domain Admins");

        /// <summary>
        /// The set of Security Groups which the user belongs to.
        /// </summary>
        public List<GroupInfo> Groups { get; set; } = new List<GroupInfo>();
    }
}
