namespace MyDomainPasswordChange.Api.Models;

public record DefaultAccountConfiguration
{
    public required string DefaultMailBoxSize { get; set; }
    public required bool PasswordNeverExpiresStatus { get; set; }
    public required bool DefaultEnabledStatus { get; set; }
}
