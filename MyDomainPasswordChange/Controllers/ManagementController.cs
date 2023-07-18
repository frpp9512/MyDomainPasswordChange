using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Extensions;
using MyDomainPasswordChange.Filters;
using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Managers.Interfaces;
using MyDomainPasswordChange.Managers.Models;
using MyDomainPasswordChange.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Controllers;

[ServiceFilter(typeof(BlacklistFilter))]
[Authorize]
public class ManagementController : Controller
{
    private readonly IDomainPasswordManagement _passwordManagement;
    private readonly ILogger<ManagementController> _logger;
    private readonly IDependenciesGroupsManagement _groupsManagement;
    private readonly IPasswordHistoryManager _historyManager;
    private readonly IConfiguration _configuration;
    private readonly IMailNotificator _mailNotificator;
    private readonly IMapper _mapper;

    public ManagementController(IDomainPasswordManagement passwordManagement,
                                ILogger<ManagementController> logger,
                                IDependenciesGroupsManagement groupsManagement,
                                IPasswordHistoryManager historyManager,
                                IConfiguration configuration,
                                IMailNotificator mailNotificator,
                                IMapper mapper)
    {
        _passwordManagement = passwordManagement;
        _logger = logger;
        _groupsManagement = groupsManagement;
        _historyManager = historyManager;
        _configuration = configuration;
        _mailNotificator = mailNotificator;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAsync()
    {
        var accountName = User.FindFirst(ClaimTypes.NameIdentifier).Value;
        IEnumerable<DependencyDeclaration> groupsDeclarations;

        try
        {
            var userInfo = await _passwordManagement.GetUserInfo(accountName);
            groupsDeclarations = userInfo.Groups.Any(g => _groupsManagement.DefineIfGlobalDeclaration(g.AccountName))
                ? _groupsManagement.GetAllDependenciesDeclarations()
                : userInfo.Groups.Where(g => _groupsManagement.DefineIfDependencyDeclaration(g.AccountName))
                                                        .Select(g => _groupsManagement.GetDeclarationByName(g.AccountName));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            groupsDeclarations = new List<DependencyDeclaration>();
        }

        var viewModel = await GenerateUserManagementViewModelAsync(groupsDeclarations);

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> GetUsersByInternetAccess()
    {
        var ldapFullInternetGroup = await _passwordManagement.GetGroupInfoByNameAsync("navInternacional");
        var ldapRestInternetGroup = await _passwordManagement.GetGroupInfoByNameAsync("navInternacionalRest");
        var usersWithFullInternet = await _passwordManagement.GetActiveUsersInfoFromGroupAsync(ldapFullInternetGroup);
        var usersWithRestInternet = await _passwordManagement.GetActiveUsersInfoFromGroupAsync(ldapRestInternetGroup);
        return Ok(new
        {
            totalUsersWithInternet = usersWithRestInternet.Count + usersWithFullInternet.Count,
            users = new[] {
                new {
                    access = "FullInternet",
                    total = usersWithFullInternet.Count,
                    users = usersWithFullInternet
                },
                new
                {
                    access = "RestInternet",
                    total = usersWithRestInternet.Count,
                    users = usersWithRestInternet
                }
            }
        });
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
            groupVM.Users = MapUsersToViewModels(groupUsers);
            viewModel.Groups.Add(groupVM);
        }

        var ldapGroups = groupsDeclarations.Select(g => _passwordManagement.GetGroupInfoByNameAsync(g.GroupName));
        return viewModel;
    }

    private List<UserViewModel> MapUsersToViewModels(List<UserInfo> groupUsers) => groupUsers.Select(user =>
    {
        var vm = _mapper.Map<UserViewModel>(user);
        vm.InternetAccess = user.Groups switch
        {
            var groups when groups.Any(g => g.AccountName == Constants.FullInternetGroup) => InternetAccess.Full,
            var groups when groups.Any(g => g.AccountName == Constants.RestInternetGroup) => InternetAccess.Restricted,
            var groups when groups.Any(g => g.AccountName == Constants.NationalInternetGroup) => InternetAccess.National,
            _ => InternetAccess.None
        };
        return vm;
    }).ToList();

    [HttpGet]
    public async Task<IActionResult> ResetUserPasswordAsync(string accountName)
    {
        UserInfo user;
        try
        {
            user = await _passwordManagement.GetUserInfoAsync(accountName);
        }
        catch (UserNotFoundException)
        {
            TempData["UserUnknown"] = accountName;
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }

        if (User.IsInRole("GlobalAdmin") || user.Groups.Any(g => User.Claims.First(c => c.Type == "DependencyGroups").Value.Contains(g.AccountName)))
        {
            var viewModel = _mapper.Map<UserViewModel>(user);
            return View(viewModel);
        }

        TempData["UnauthorizedAction"] = true;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetUserPasswordAsync(UserViewModel viewModel)
    {
        if (_passwordManagement.UserExists(viewModel.AccountName))
        {
            try
            {
                _passwordManagement.ResetPassword(viewModel.AccountName, viewModel.Description);
                await _mailNotificator.SendManagementUserPasswordResetted(
                    _mapper.Map<UserInfo>(viewModel),
                    (User.Identity.Name, User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value));
                TempData["PasswordResetted"] = viewModel.DisplayName;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
        }
        else
        {
            TempData["UserUnknown"] = viewModel.AccountName;
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> SetUserPasswordAsync(string accountName)
    {
        UserInfo user;
        try
        {
            user = await _passwordManagement.GetUserInfoAsync(accountName);
        }
        catch (UserNotFoundException)
        {
            TempData["UserUnknown"] = accountName;
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }

        if (User.IsInRole("GlobalAdmin")
            || user.Groups.Any(g => User.Claims.First(c => c.Type == "DependencyGroups").Value
                                               .Contains(g.AccountName)))
        {
            var viewModel = _mapper.Map<SetUserPasswordViewModel>(user);
            return View(viewModel);
        }

        TempData["UnauthorizedAction"] = true;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserPasswordAsync(SetUserPasswordViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            if (!_passwordManagement.UserExists(viewModel.AccountName))
            {
                TempData["UserUnknown"] = viewModel.AccountName;
            }
            else
            {
                if (await _historyManager.CheckPasswordHistoryAsync(viewModel.AccountName, viewModel.Password, _configuration.GetValue<int>("PasswordHistoryCheck")))
                {
                    ModelState.AddModelError("PasswordHistory", "La nueva contraseña ya ha sido utilizada por el usuario anteriormente.");
                    var user = await _passwordManagement.GetUserInfoAsync(viewModel.AccountName);
                    viewModel = new SetUserPasswordViewModel
                    {
                        AccountName = user.AccountName,
                        DisplayName = user.DisplayName,
                        Description = user.Description,
                        Email = user.Email,
                        LastPasswordSet = user.LastPasswordSet
                    };
                    return View(viewModel);
                }
                else
                {
                    try
                    {
                        _passwordManagement.SetUserPassword(viewModel.AccountName, viewModel.Password);
                        await _historyManager.RegisterPasswordAsync(viewModel.AccountName, viewModel.Password);
                        await _mailNotificator.SendManagementUserPasswordResetted(
                            new UserInfo
                            {
                                AccountName = viewModel.AccountName,
                                Description = viewModel.Description,
                                DisplayName = viewModel.DisplayName,
                                Email = viewModel.Email,
                            },
                            (User.Identity.Name, User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value));
                        TempData["PasswordSetted"] = new string[] { viewModel.DisplayName };
                        await _mailNotificator.SendManagementUserPasswordSetted(
                            _mapper.Map<UserInfo>(viewModel),
                            (User.Identity.Name, User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value));
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = ex.Message;
                    }
                }
            }
        }

        return View(viewModel);
    }
}