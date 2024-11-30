using MyDomainPasswordChange.Models;
using System.Collections.Generic;
using System.Linq;

namespace MyDomainPasswordChange.Managers.Models;

public record DependencyDeclaration
{
    public required string Type { get; init; }
    public required string GroupName { get; init; }
    public required string OU { get; init; }
    public string Description { get; set; }
    public List<AreaDefinition> AreaDefinitions { get; init; } = [];

    public AreaDefinition this[string areaName] => AreaDefinitions.First(area => area.GroupName == areaName);

    public bool ExistsArea(string areaName) => AreaDefinitions.Any(area => area.GroupName == areaName);
}
