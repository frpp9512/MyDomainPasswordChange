namespace MyDomainPasswordChange.Models;

public record AreaDefinition
{
    public required string GroupName { get; set; }
    public string Description { get; set; }
    public required string OU { get; set; }
}
