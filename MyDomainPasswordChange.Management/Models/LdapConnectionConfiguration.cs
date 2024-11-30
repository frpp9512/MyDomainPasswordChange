namespace MyDomainPasswordChange.Management.Models;
public record LdapConnectionConfiguration
{
    public string LdapServer { get; init; }
    public string LdapSearchBase { get; init; }
    public string LdapBindUsername { get; init; }
    public string LdapBindPassword { get; init; }
}
