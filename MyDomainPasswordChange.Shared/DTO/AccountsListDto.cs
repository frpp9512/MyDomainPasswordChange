namespace MyDomainPasswordChange.Shared.DTO;
public record AccountsListDto
{
    public required GroupInfoDto GroupInfo { get; set; }
    public List<AccountDto> Accounts { get; set; } = [];
}
