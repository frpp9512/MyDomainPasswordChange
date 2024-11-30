namespace MyDomainPasswordChange.Management.Models;

public record GroupInfo
{
    /// <summary>
    /// The account name (sAMAccountName) of the user.
    /// </summary>
    public string AccountName { get; set; }

    /// <summary>
    /// The Distinguished Name (DN) of the group.
    /// </summary>
    public string DistinguishedName { get; set; }

    /// <summary>
    /// The display name of the user.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// The description of the group.
    /// </summary>
    public string Description { get; set; }
}