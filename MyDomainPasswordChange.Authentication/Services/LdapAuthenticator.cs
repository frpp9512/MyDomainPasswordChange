using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Authentication.Configuration;
using MyDomainPasswordChange.Authentication.Contracts;
using Novell.Directory.Ldap;

namespace MyDomainPasswordChange.Authentication.Services;

public class LdapAuthenticator(IOptions<LdapAuthenticationOptions> options) : ILdapAuthenticator
{
    private readonly LdapAuthenticationOptions _options = options.Value;

    public bool Authenticate(string accountName, string password)
    {
        try
        {
            using LdapConnection ldapConnection = new() { SecureSocketLayer = false };
            ldapConnection.Connect(_options.LdapServer, _options.LdapPort);
            ldapConnection.Bind(_options.GetFullAccountName(accountName), password);
            return ldapConnection.Bound;
        }
        catch (LdapException ldapException) when (ldapException.ResultCode == 49)
        {
            return false;
        }
    }

    public Task<bool> AuthenticateAsync(string accountName, string password) => Task.Run(() => Authenticate(accountName, password));
}
