namespace MyDomainPasswordChange.Authentication.Configuration;

public record LdapAuthenticationOptions
{
    public required string LdapServer { get; set; }
    public required int LdapPort { get; set; }
    public required string SearchBase { get; set; }
    public string? AccountPrefix { get; set; }
    public string? AccountSuffix { get; set; }

    public string GetFullAccountName(string accountName) => string.Concat(AccountPrefix, accountName, AccountSuffix);
}
