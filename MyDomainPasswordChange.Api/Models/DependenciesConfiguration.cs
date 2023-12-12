namespace MyDomainPasswordChange.Api.Models;

public record DependenciesConfiguration
{
    public List<DependencyDefinition> Definitions { get; set; } = [];

    public DependencyDefinition this[string dependencyName] => Definitions.First(dep => dep.GroupName == dependencyName);
    public bool ExistDependency(string dependencyName) => Definitions.Any(dep => dep.GroupName == dependencyName);
    public bool ExistAreaInDependency(string dependencyName, string areaName) => Definitions.FirstOrDefault(dep => dep.GroupName == dependencyName)?.AreaDefinitions.Any(area => area.GroupName == areaName) is true;
}
