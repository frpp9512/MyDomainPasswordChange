using Microsoft.AspNetCore.Authentication;
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
    [ServiceFilter(typeof(BlacklistFilter))]
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IDomainPasswordManagement _passwordManagement;
        private readonly IDependenciesGroupsManagement _groupsManagement;
        private readonly IAlertCountingManagement _alertCountingManagement;
        private readonly IMailNotificator _notificator;

        public AuthController(ILogger<AuthController> logger,
                              IDomainPasswordManagement passwordManagement,
                              IDependenciesGroupsManagement groupsManagement,
                              IAlertCountingManagement alertCountingManagement,
                              IMailNotificator notificator)
        {
            _logger = logger;
            _passwordManagement = passwordManagement;
            _groupsManagement = groupsManagement;
            _alertCountingManagement = alertCountingManagement;
            _notificator = notificator;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/Management") 
            => User.Identity.IsAuthenticated 
                ? Redirect(returnUrl) 
                : View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (_passwordManagement.AuthenticateUser(viewModel.Username, viewModel.Password))
                {
                    var user = _passwordManagement.GetUserInfo(viewModel.Username);

                    await _notificator.SendManagementLogin(user);

                    if (user.Enabled && 
                        user.IsDomainAdmin && 
                        user.Groups.Any(g => 
                            _groupsManagement.DefineIfDependencyDeclaration(g.AccountName) ||
                            _groupsManagement.DefineIfGlobalDeclaration(g.AccountName)))
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.AccountName),
                            new Claim(ClaimTypes.Name, user.DisplayName),
                            new Claim(ClaimTypes.Email, user.Email),
                        };
                        if (user.Groups.Any(g => _groupsManagement.DefineIfGlobalDeclaration(g.AccountName)))
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "GlobalAdmin"));
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "DependencyAdmin"));
                        }
                        var dependencyGroups = string.Join(";", user.Groups.Where(g => _groupsManagement.ExistDelclarationWithName(g.AccountName))
                                                                  .Select(g => g.AccountName)
                                                                  .ToArray());
                        claims.Add(new Claim("DependencyGroups", dependencyGroups));
                        var identity = new ClaimsIdentity(claims, "CookieAuth");
                        var principal = new ClaimsPrincipal(identity);
                        await HttpContext.SignInAsync("CookieAuth",
                                                      principal,
                                                      new AuthenticationProperties { IsPersistent = viewModel.RememberMe });

                        return Redirect(viewModel.ReturnUrl);
                    }
                }
            }
            ModelState.AddModelError("BadLogin", "Error de autenticación.");
            await _alertCountingManagement.CountManagementAuthFailAsync();
            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> Logout(string returnUrl = "/")
        {
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync();
            }
            return Redirect(returnUrl);
        }
    }
}