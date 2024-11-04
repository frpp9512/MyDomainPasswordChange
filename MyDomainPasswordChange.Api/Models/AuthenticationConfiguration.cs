namespace MyDomainPasswordChange.Api.Models;

public record AuthenticationConfiguration
{
    public required string ValidIssuer { get; set; }
    public required string ValidAudience { get; set; }
    public required string Secret { get; set; }
    public string GlobalAdministratorsGroup { get; set; } = "netAdmins";
}
