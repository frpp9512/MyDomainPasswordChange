using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDomainPasswordChange.Management;
using MyDomainPasswordChange.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Controllers
{
    [Authorize]
    public class ManagementController : Controller
    {
        private readonly IDomainPasswordManagement _passwordManagement;
        private readonly ILogger<ManagementController> _logger;
        private readonly IDependenciesGroupsManagement _groupsManagement;

        public ManagementController(IDomainPasswordManagement passwordManagement,
                                    ILogger<ManagementController> logger,
                                    IDependenciesGroupsManagement groupsManagement)
        {
            _passwordManagement = passwordManagement;
            _logger = logger;
            _groupsManagement = groupsManagement;
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync()
        {
            var accountName = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userInfo = _passwordManagement.GetUserInfo(accountName);
            IEnumerable<DependencyDeclaration> groupsDeclarations = userInfo.Groups.Any(g => _groupsManagement.DefineIfGlobalDeclaration(g.AccountName))
                ? _groupsManagement.GetAllDependenciesDeclarations()
                : userInfo.Groups.Where(g => _groupsManagement.DefineIfDependencyDeclaration(g.AccountName))
                                                        .Select(g => _groupsManagement.GetDeclarationByName(g.AccountName));
            
            var viewModel = await GenerateUserManagementViewModelAsync(groupsDeclarations);
            return View(viewModel);
        }

        private async Task<UsersManagementViewModel> GenerateUserManagementViewModelAsync(IEnumerable<DependencyDeclaration> groupsDeclarations)
        {
            var viewModel = new UsersManagementViewModel();
            foreach (var groupDeclaration in groupsDeclarations)
            {
                var ldapGroup = await _passwordManagement.GetGroupInfoByNameAsync(groupDeclaration.GroupName);
                var groupVM = new DependencyGroupViewModel
                {
                    DisplayName = ldapGroup.DisplayName,
                    Name = ldapGroup.AccountName,
                    Description = ldapGroup.Description
                };
                var groupUsers = await _passwordManagement.GetActiveUsersInfoFromGroupAsync(ldapGroup);
                groupVM.Users = groupUsers.Select(u => new UserViewModel
                {
                    AccountName = u.AccountName,
                    DisplayName = u.DisplayName,
                    Description = u.Description,
                    Email = u.Email,
                    LastPasswordSet = u.LastPasswordSet
                }).ToList();

                viewModel.Groups.Add(groupVM);
            }
            var ldapGroups = groupsDeclarations.Select(g => _passwordManagement.GetGroupInfoByNameAsync(g.GroupName));
            return viewModel;
        }

        [HttpGet]
        public async Task<IActionResult> ResetUserPassword(string accountName)
        {
            var user = await _passwordManagement.GetUserInfoAsync(accountName);
            var viewModel = new UserViewModel 
            {
                AccountName = accountName,
                DisplayName = user.DisplayName,
                Description = user.Description,
                Email = user.Email,
                LastPasswordSet = user.LastPasswordSet
            };
            return View(viewModel);
        }
    }
}