using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management.Interfaces;

namespace MyDomainPasswordChange.Managers.Services;

public class BindCredentialsProvider(IConfiguration configuration) : IBindCredentialsProvider
{
    private readonly string _username = configuration["LdapBindUsername"];
    private readonly string _password = configuration["LdapBindPassword"];
    private readonly string _searchBase = configuration["LdapSearchBase"];
    private readonly string _ldapServer = configuration["LdapServer"];

    public string GetBindPassword() => _password;
    public string GetBindUsername() => _username;
    public string GetLdapSearchBase() => _searchBase;
    public string GetLdapServer() => _ldapServer;
}
