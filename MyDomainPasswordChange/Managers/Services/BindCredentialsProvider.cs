using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management.Interfaces;

namespace MyDomainPasswordChange.Managers.Services;

public class BindCredentialsProvider : IBindCredentialsProvider
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _searchBase;
    private readonly string _ldapServer;

    public BindCredentialsProvider(IConfiguration configuration)
    {
        _username = configuration["LdapBindUsername"];
        _password = configuration["LdapBindPassword"];
        _searchBase = configuration["LdapSearchBase"];
        _ldapServer = configuration["LdapServer"];
    }

    public string GetBindPassword() => _password;
    public string GetBindUsername() => _username;
    public string GetLdapSearchBase() => _searchBase;
    public string GetLdapServer() => _ldapServer;
}
