using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class DependencyDeclaration
    {
        /// <summary>
        /// Defines the type of the group, "global" for general access, "dependency" for specific.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Defines the name of the LDAP group.
        /// </summary>
        public string GroupName { get; set; }
    }
}
