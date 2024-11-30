namespace MyDomainPasswordChange.Api.Models;

public record AdminInfoConfiguration
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string AccountName { get; set; }
    public required string Password { get; set; }
}
