namespace MyDomainPasswordChange.Shared.DTO;
public record ResetPasswordRequestDto
{
    public required string AccountName { get; init; }
    public required string CurrentPassword { get; init; }
    public required string TempPassword { get; init; }
}
