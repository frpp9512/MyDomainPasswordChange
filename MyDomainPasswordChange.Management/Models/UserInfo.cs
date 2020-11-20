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
        /// The user's job title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The specified user's company.
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// The specified user's deparment.
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// The user's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The last time when the password was setted.
        /// </summary>
        public DateTime LastPasswordSet { get; set; }
    }
}
