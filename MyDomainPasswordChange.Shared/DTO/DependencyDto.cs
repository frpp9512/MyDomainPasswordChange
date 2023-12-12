namespace MyDomainPasswordChange.Shared.DTO;
public record DependencyDto
{
    public required string GroupName { get; init; }
    public required List<AreaDto> Areas { get; init; } = [];
}
