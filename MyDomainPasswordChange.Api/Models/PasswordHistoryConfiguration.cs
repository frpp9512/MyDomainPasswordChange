namespace MyDomainPasswordChange.Api.Models;

public record PasswordHistoryConfiguration
{
    public int LastPasswordHistoryCheck { get; set; }
    public int PasswordExpirationDays { get; set; }
}
