namespace MyDomainPasswordChange.Api.Models;

public record DependencyDefinition
{
    public required string Type { get; init; }
    public required string GroupName { get; init; }
    public required string OU { get; init; }
    public List<AreaDefinition> AreaDefinitions { get; init; } = [];

    public AreaDefinition this[string areaName] => AreaDefinitions.First(area => area.GroupName == areaName);

    public bool ExistsArea(string areaName) => AreaDefinitions.Any(area => area.GroupName == areaName);
}
