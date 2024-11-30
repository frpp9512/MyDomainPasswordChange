namespace MyDomainPasswordChange.Shared.DTO;
public record AccountAuthDto
{
    public required string AccountName { get; init; }
    public required string Password { get; init; }
}
