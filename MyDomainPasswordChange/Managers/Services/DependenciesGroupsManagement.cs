using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class DependenciesGroupsManagement : IDependenciesGroupsManagement
    {
        private readonly IConfiguration _configuration;

        private List<DependencyDeclaration> _dependencyDeclarations;

        public DependenciesGroupsManagement(IConfiguration configuration)
        {
            _configuration = configuration;
            LoadDeclarations();
        }

        private void LoadDeclarations()
            => _dependencyDeclarations = _configuration.GetSection("DependenciesGroup").Get<List<DependencyDeclaration>>();

        public DependencyDeclaration GetDeclarationByName(string groupName)
            => _dependencyDeclarations?.Where(d => d.GroupName == groupName).FirstOrDefault();

        public bool ExistDelclarationWithName(string groupName)
            => _dependencyDeclarations?.Any(d => d.GroupName == groupName) == true;

        public bool DefineIfGlobalDeclaration(string groupName)
            => _dependencyDeclarations?.FirstOrDefault(d => d.GroupName == groupName)?.Type == "global";

        public bool DefineIfDependencyDeclaration(string groupName)
            => _dependencyDeclarations?.FirstOrDefault(d => d.GroupName == groupName)?.Type == "dependency";

        public IEnumerable<DependencyDeclaration> GetAllDependenciesDeclarations()
            => _dependencyDeclarations?.Where(d => DefineIfDependencyDeclaration(d.GroupName));
    }
}