namespace MyDomainPasswordChange.Shared.DTO;

public record SetPasswordRequestDto
{
    public required string AccountName { get; init; }
    public required string NewPassword { get; init; }
    public required bool CheckHistory { get; init; }
}
