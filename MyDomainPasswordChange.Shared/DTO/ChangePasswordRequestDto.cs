namespace MyDomainPasswordChange.Shared.DTO;

public record ChangePasswordRequestDto
{
    public required string AccountName { get; init; }
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}
