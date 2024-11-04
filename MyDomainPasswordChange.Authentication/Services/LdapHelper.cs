using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Authentication.Configuration;
using Novell.Directory.Ldap;
using System.Security.Principal;

namespace MyDomainPasswordChange.Authentication.Services;

public class LdapHelper(IOptions<LdapAuthenticationOptions> options)
{
    private readonly LdapAuthenticationOptions _options = options.Value;

    public string? GetPrimaryGroup(string accountName, string password)
    {
        using LdapConnection ldapConnection = new() { SecureSocketLayer = false };
        ldapConnection.Connect(_options.LdapServer, _options.LdapPort);
        ldapConnection.Bind(_options.GetFullAccountName(accountName), password);

        var searchFilter = $"(sAMAccountName={accountName})";
        LdapSearchConstraints searchConstraints = new();
        ILdapSearchResults searchResults = ldapConnection.Search(
            _options.SearchBase,
            LdapConnection.ScopeSub,
            searchFilter,
            null,
            false,
            searchConstraints);

        string? primaryGroupID = null;
        if (searchResults.HasMore())
        {
            LdapEntry entry = searchResults.Next();
            primaryGroupID = entry.GetAttribute("primaryGroupID").StringValue;
        }

        if (primaryGroupID == null)
        {
            return null;
        }

        var domainSid = GetDomainSid(accountName, password);
        var groupSid = $"{domainSid}-{primaryGroupID}";
        var groupSearchFilter = $"(objectSid={groupSid})";
        ILdapSearchResults groupSearchResults = ldapConnection.Search(
            _options.SearchBase,
            LdapConnection.ScopeSub,
            groupSearchFilter,
            null,
            false);

        string? primaryGroupCN = null;
        if (groupSearchResults.HasMore())
        {
            LdapEntry groupEntry = groupSearchResults.Next();
            primaryGroupCN = groupEntry.GetAttribute("cn").StringValue;
        }

        return primaryGroupCN;
    }

    public string? GetDomainSid(string username, string password)
    {
        using LdapConnection ldapConnection = new() { SecureSocketLayer = false };
        ldapConnection.Connect(_options.LdapServer, _options.LdapPort);
        ldapConnection.Bind(_options.GetFullAccountName(username), password);
        var searchFilter = "(objectClass=domain)";
        ILdapSearchResults searchResults = ldapConnection.Search(
            _options.SearchBase,
            LdapConnection.ScopeBase,
            searchFilter,
            null,
            false);

        byte[]? domainSid = null;
        if (searchResults.HasMore())
        {
            LdapEntry entry = searchResults.Next();
            domainSid = entry.GetAttribute("objectSid").ByteValue;
        }

        return domainSid is not null ? new SecurityIdentifier(domainSid, 0).ToString() : null;
    }

    public List<string> GetUserGroups(string username, string password)
    {
        LdapConnection ldapConnection = new() { SecureSocketLayer = false };
        ldapConnection.Connect(_options.LdapServer, _options.LdapPort);
        ldapConnection.Bind(_options.GetFullAccountName(username), password);

        var searchFilter = $"(sAMAccountName={username})";
        ILdapSearchResults searchResults = ldapConnection.Search(
            _options.SearchBase,
            LdapConnection.ScopeSub,
            searchFilter,
            ["memberOf"],
            false);

        List<string> groups = [];
        if (!searchResults.HasMore())
        {
            return groups;
        }

        LdapEntry entry = searchResults.Next();
        LdapAttribute? attribute = entry.GetAttribute("memberOf");
        if (attribute is null)
        {
            return groups;
        }

        var groupDns = attribute.StringValueArray;
        return [.. groupDns.Select(GetCommonNameFromDn)];
    }

    private static string? GetCommonNameFromDn(string dn)
    {
        var parts = dn.Split(',');
        foreach (var part in parts)
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && kv[0].Trim().Equals("CN", StringComparison.OrdinalIgnoreCase))
            {
                return kv[1].Trim();
            }
        }

        return null;
    }
}
