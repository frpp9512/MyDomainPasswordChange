namespace MyDomainPasswordChange.Shared.DTO;
public record CreateAccountDto : AccountDto
{
    public required string Password { get; set; }
    public required string DependencyId { get; set; }
    public required string AreaId { get; set; }
    public string[] GroupsId { get; set; } = [];
}
