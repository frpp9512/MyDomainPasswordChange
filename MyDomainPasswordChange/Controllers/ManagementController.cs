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
public class ManagementController(IDomainPasswordManagement passwordManagement,
                                  ILogger<ManagementController> logger,
                                  IDependenciesGroupsManagement groupsManagement,
                                  IPasswordHistoryManager historyManager,
                                  IConfiguration configuration,
                                  IMailNotifier mailNotificator,
                                  IMapper mapper) : Controller
{
    private readonly IDomainPasswordManagement _passwordManagement = passwordManagement;
    private readonly ILogger<ManagementController> _logger = logger;
    private readonly IDependenciesGroupsManagement _groupsManagement = groupsManagement;
    private readonly IPasswordHistoryManager _historyManager = historyManager;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMailNotifier _mailNotificator = mailNotificator;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<IActionResult> IndexAsync()
    {
        var accountName = User.FindFirst(ClaimTypes.NameIdentifier).Value;
        IEnumerable<DependencyDeclaration> groupsDeclarations;

        try
        {
            UserInfo userInfo = await _passwordManagement.GetUserInfo(accountName);
            groupsDeclarations = userInfo.Groups.Any(g => _groupsManagement.DefineIfGlobalDeclaration(g.AccountName))
                ? _groupsManagement.GetAllDependenciesDeclarations()
                : userInfo.Groups.Where(g => _groupsManagement.DefineIfDependencyDeclaration(g.AccountName))
                                                        .Select(g => _groupsManagement.GetDeclarationByName(g.AccountName));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            groupsDeclarations = [];
        }

        UsersManagementViewModel viewModel = await GenerateUserManagementViewModelAsync(groupsDeclarations);

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Roles = "GlobalAdmin")]
    public async Task<IActionResult> GetUsersByInternetAccess()
    {
        GroupInfo ldapFullInternetGroup = await _passwordManagement.GetGroupInfoByNameAsync("navInternacional");
        GroupInfo ldapRestInternetGroup = await _passwordManagement.GetGroupInfoByNameAsync("navInternacionalRest");
        List<UserInfo> usersWithFullInternet = await _passwordManagement.GetActiveUsersInfoFromGroupAsync(ldapFullInternetGroup);
        List<UserInfo> usersWithRestInternet = await _passwordManagement.GetActiveUsersInfoFromGroupAsync(ldapRestInternetGroup);
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
        UsersManagementViewModel viewModel = new();
        foreach (DependencyDeclaration groupDeclaration in groupsDeclarations)
        {
            GroupInfo ldapGroup = await _passwordManagement.GetGroupInfoByNameAsync(groupDeclaration.GroupName);
            DependencyGroupViewModel groupVM = new()
            {
                DisplayName = ldapGroup.DisplayName,
                Name = ldapGroup.AccountName,
                Description = ldapGroup.Description
            };
            List<UserInfo> groupUsers = await _passwordManagement.GetActiveUsersInfoFromGroupAsync(ldapGroup);
            groupVM.Users = MapUsersToViewModels(groupUsers);
            viewModel.Groups.Add(groupVM);
        }

        IEnumerable<Task<GroupInfo>> ldapGroups = groupsDeclarations.Select(g => _passwordManagement.GetGroupInfoByNameAsync(g.GroupName));
        return viewModel;
    }

    private List<UserViewModel> MapUsersToViewModels(List<UserInfo> groupUsers)
        => groupUsers.Select(user =>
        {
            UserViewModel vm = _mapper.Map<UserViewModel>(user);
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
            UserViewModel viewModel = _mapper.Map<UserViewModel>(user);
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
            SetUserPasswordViewModel viewModel = _mapper.Map<SetUserPasswordViewModel>(user);
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
                    UserInfo user = await _passwordManagement.GetUserInfoAsync(viewModel.AccountName);
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

    [HttpGet]
    public async Task<IActionResult> CreateAccountAsync()
    {
        try
        {
            CreateAccountViewModel model = await GenerateCreateAccountViewModel();

            return View(model);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return Problem();
        }
    }

    private async Task<CreateAccountViewModel> GenerateCreateAccountViewModel()
    {
        var accountName = User.FindFirst(ClaimTypes.NameIdentifier).Value;
        UserInfo userInfo = await _passwordManagement.GetUserInfo(accountName);
        IEnumerable<DependencyDeclaration> groupsDeclarations = userInfo.Groups.Any(g => _groupsManagement.DefineIfGlobalDeclaration(g.AccountName))
            ? _groupsManagement.GetAllDependenciesDeclarations()
            : userInfo.Groups.Where(g => _groupsManagement.DefineIfDependencyDeclaration(g.AccountName))
                                                    .Select(g => _groupsManagement.GetDeclarationByName(g.AccountName));

        var workstations = _passwordManagement.GetAllWorkstations();
        var model = new CreateAccountViewModel
        {
            AccountName = "",
            Address = "",
            AreaId = "",
            DependencyId = "",
            Description = "",
            Email = "",
            FirstName = "",
            JobTitle = "",
            LastName = "",
            Office = "",
            Password = "",
            PersonalId = "",
            Dependencies = groupsDeclarations.ToList(),
            AvailableWorkstations = [.. workstations],
        };
        return model;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccountAsync(CreateAccountModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await GenerateCreateAccountViewModel());
        }

        try
        {
            if (_passwordManagement.UserExists(model.AccountName))
            {
                TempData["AccountNameTaken"] = model.AccountName;
                return View(await GenerateCreateAccountViewModel());
            }

            var accountName = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            UserInfo userInfo = await _passwordManagement.GetUserInfo(accountName);
            List<DependencyDeclaration> groupsDeclarations =
                userInfo.Groups.Any(g => _groupsManagement.DefineIfGlobalDeclaration(g.AccountName))
                ? _groupsManagement.GetAllDependenciesDeclarations().ToList()
                : userInfo.Groups.Where(g => _groupsManagement.DefineIfDependencyDeclaration(g.AccountName))
                                 .Select(g => _groupsManagement.GetDeclarationByName(g.AccountName))
                                 .ToList();

            if (groupsDeclarations.FirstOrDefault(group => group.GroupName == model.DependencyId) is not DependencyDeclaration selectedDependency
                || selectedDependency.AreaDefinitions.FirstOrDefault(area => area.GroupName == model.AreaId) is not AreaDefinition selectedArea)
            {
                TempData["Error"] = "Debe de seleccionar una dependencia y un area válida para crear la cuenta.";
                return View(await GenerateCreateAccountViewModel());
            }

            UserInfo newUserInfo = new()
            {
                AccountName = model.AccountName,
                Address = model.Address,
                AllowedWorkstations = model.AllowedWorkstations,
                Description = model.Description,
                DisplayName = $"{model.FirstName} {model.LastName}",
                Email = $"{model.AccountName}@ingeco.cu",
                Enabled = true,
                FirstName = model.FirstName,
                JobTitle = model.JobTitle,
                LastName = model.LastName,
                Office = model.Office,
                MailboxCapacity = "150M",
                PersonalId = model.PersonalId
            };

            string[] groups =
            [
                selectedDependency.GroupName,
                selectedArea.GroupName,
                model.InternetAccess switch
                {
                    InternetAccess.National => "navNacional",
                    InternetAccess.Full => "navInternacional",
                    InternetAccess.Restricted => "navInternacionalRest",
                    _ => ""
                },
                model.CloudAccess ? "accesoNube" : "",
                model.FTPAccess ? "accesoFtp" : "",
                model.JabberAccess ? "accesoJabber" : "",
                model.MediaAccess ? "mediaUser" : ""
            ];

            await _passwordManagement.CreateNewUserAsync(
                newUserInfo,
                model.Password,
                selectedDependency.OU,
                selectedArea.OU,
                [..groups.Where(g => !string.IsNullOrEmpty(g))]);

            await _mailNotificator.SendManagementCreatedUser(
                            _mapper.Map<UserInfo>(newUserInfo),
                            selectedDependency.Description,
                            selectedArea.Description,
                            (User.Identity.Name, User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value));

            TempData["UserCreated"] = $"{model.DisplayName}";
            return RedirectToActionPermanent("Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(await GenerateCreateAccountViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> UserDetailsAsync(string accountName)
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
            UserViewModel viewModel = _mapper.Map<UserViewModel>(user);
            viewModel.InternetAccess = user.Groups switch
            {
                var groups when groups.Any(g => g.AccountName == Constants.FullInternetGroup) => InternetAccess.Full,
                var groups when groups.Any(g => g.AccountName == Constants.RestInternetGroup) => InternetAccess.Restricted,
                var groups when groups.Any(g => g.AccountName == Constants.NationalInternetGroup) => InternetAccess.National,
                _ => InternetAccess.None
            };
            return View(viewModel);
        }

        TempData["UnauthorizedAction"] = true;
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> DeleteAccountAsync(string accountName)
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
            UserViewModel viewModel = _mapper.Map<UserViewModel>(user);
            viewModel.InternetAccess = user.Groups switch
            {
                var groups when groups.Any(g => g.AccountName == Constants.FullInternetGroup) => InternetAccess.Full,
                var groups when groups.Any(g => g.AccountName == Constants.RestInternetGroup) => InternetAccess.Restricted,
                var groups when groups.Any(g => g.AccountName == Constants.NationalInternetGroup) => InternetAccess.National,
                _ => InternetAccess.None
            };

            return View(viewModel);
        }

        TempData["UnauthorizedAction"] = true;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccountAsync(DeleteAccountModel viewModel)
    {
        if (_passwordManagement.UserExists(viewModel.AccountName))
        {
            try
            {
                var user = await _passwordManagement.GetUserInfoAsync(viewModel.AccountName);
                _passwordManagement.DeleteAccount(viewModel.AccountName);
                await _mailNotificator.SendManagementAccountDeleted(
                    user,
                    (User.Identity.Name, User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value));
                TempData["AccountDeleted"] = user.DisplayName;
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
}
