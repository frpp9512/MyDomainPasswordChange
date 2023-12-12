namespace MyDomainPasswordChange.Shared.DTO;
public record CreateAccountDto
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
    /// Defines the workstations that the user is allowed to login to.
    /// </summary>
    public List<string> AllowedWorkstations { get; set; } = ["NONE"];

    /// <summary>
    /// The password that will be used for the user.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// The identifier of the dependency.
    /// </summary>
    public required string DependencyId { get; set; }

    /// <summary>
    /// The identifier of the dependency's area.
    /// </summary>
    public required string AreaId { get; set; }

    /// <summary>
    /// The groups where the user 
    /// </summary>
    public string[] GroupsId { get; set; } = [];
}
