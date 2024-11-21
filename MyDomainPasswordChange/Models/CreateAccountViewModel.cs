using MyDomainPasswordChange.Managers.Models;
using System.Collections.Generic;

namespace MyDomainPasswordChange.Models;
public record CreateAccountViewModel : CreateAccountModel
{
    public List<DependencyDeclaration> Dependencies { get; set; }
    public string[] AvailableWorkstations { get; set; }
}
