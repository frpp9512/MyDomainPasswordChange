namespace MyDomainPasswordChange.Models;

public record GroupModel
{
    public string AccountName { get; set; }
    public string DistinguishedName { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
}
