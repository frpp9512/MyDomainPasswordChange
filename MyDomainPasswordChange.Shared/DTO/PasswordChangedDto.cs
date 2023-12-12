namespace MyDomainPasswordChange.Shared.DTO;

public record PasswordChangedDto(string AccountName, string DisplayName, string Description, string Email, int PasswordExpirationDays);
