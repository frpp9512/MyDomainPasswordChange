using Microsoft.AspNetCore.Authentication;
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
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IDomainPasswordManagement _passwordManagement;
        private readonly IAlertCountingManagement _alertCountingManagement;

        public AuthController(ILogger<AuthController> logger, IDomainPasswordManagement passwordManagement, IAlertCountingManagement alertCountingManagement)
        {
            _logger = logger;
            _passwordManagement = passwordManagement;
            _alertCountingManagement = alertCountingManagement;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl ?? "/Management" });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (_passwordManagement.AuthenticateUser(viewModel.Username, viewModel.Password))
                {
                    var user = _passwordManagement.GetUserInfo(viewModel.Username);

                    if (user.Enabled && user.IsDomainAdmin)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.AccountName),
                            new Claim(ClaimTypes.Name, user.DisplayName),
                            new Claim(ClaimTypes.Email, user.Email)
                        };
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
            return View(viewModel);
        }

        public async Task<IActionResult> Logout()
        {
            if (User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync();
            }
            return Redirect("/");
        }
    }
}