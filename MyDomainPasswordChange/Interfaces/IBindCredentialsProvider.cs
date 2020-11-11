using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public interface IBindCredentialsProvider
    {
        string GetLdapServer();
        string GetLdapSearchBase();
        string GetBindUsername();
        string GetBindPassword();
    }
}
