namespace MyDomainPasswordChange.Shared.Models;

/// <summary>
/// Represents the general info of a LDAP User object.
/// </summary>
public record Account
{
    /// <summary>
    /// The account name (sAMAccountName) of the user.
    /// </summary>
    public required string AccountName { get; set; }

    /// <summary>
    /// The display name of the user.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// The user first name and middle name if case.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// The user last names.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// The description of the user.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The user personal id.
    /// </summary>
    public required string PersonalId { get; set; }

    /// <summary>
    /// The user current job title.
    /// </summary>
    public required string JobTitle { get; set; }

    /// <summary>
    /// The name of the office where the user works.
    /// </summary>
    public required string Office { get; set; }

    /// <summary>
    /// The home address of the user.
    /// </summary>
    public required string Address { get; set; }

    /// <summary>
    /// The last time when the password was setted.
    /// </summary>
    public DateTime LastPasswordSet { get; set; }

    /// <summary>
    /// Defines if the user doesn't need to change the password periodically.
    /// </summary>
    public bool PasswordNeverExpires { get; set; }

    /// <summary>
    /// Defines the workstations that the user is allowed to login to.
    /// </summary>
    public List<string> AllowedWorkstations { get; set; } = ["NONE"];

    /// <summary>
    /// Defines the size of the user mail inbox.
    /// </summary>
    public required string MailboxCapacity { get; set; }

    /// <summary>
    /// <see langword="true"/> if the user is enabled in the Active Directory.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// <see langword="true"/> if the user belongs to the Domain Admin's group.
    /// </summary>
    public bool IsDomainAdmin => Groups.Any(g => g.AccountName == "Domain Admins");

    /// <summary>
    /// The set of Security Groups which the user belongs to.
    /// </summary>
    public List<Group> Groups { get; set; } = [];
}