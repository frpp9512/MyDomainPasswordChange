namespace MyDomainPasswordChange.Api.Models;

public record AreaDefinition
{
    public required string GroupName { get; set; }
    public required string OU { get; set; }
}
