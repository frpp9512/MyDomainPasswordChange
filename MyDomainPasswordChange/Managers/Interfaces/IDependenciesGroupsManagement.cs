using MyDomainPasswordChange.Managers.Models;
using System.Collections.Generic;

namespace MyDomainPasswordChange.Managers.Interfaces;

public interface IDependenciesGroupsManagement
{
    bool DefineIfGlobalDeclaration(string groupName);
    bool DefineIfDependencyDeclaration(string groupName);
    bool ExistDelclarationWithName(string groupName);
    DependencyDeclaration GetDeclarationByName(string groupName);
    IEnumerable<DependencyDeclaration> GetAllDependenciesDeclarations();
}