using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyDomainPasswordChange.Models;

namespace MyDomainPasswordChange.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyDomainPasswordManagement _passwordManagement;

        public HomeController(ILogger<HomeController> logger, MyDomainPasswordManagement passwordManagement)
        {
            _logger = logger;
            _passwordManagement = passwordManagement;
        }

        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _passwordManagement.ChangeUserPassword(viewModel.Username, viewModel.Password, viewModel.NewPassword);
                }
                catch (UserNotFoundException unfex)
                {
                    ModelState.AddModelError("UserNotFound", unfex.Message);
                    return View("Index");
                }
                catch (PasswordChangeException pcex)
                {
                    ModelState.AddModelError("PasswordChangeError", pcex.Message);
                    return View("Index");
                }
            }
            return View(viewModel);
        }

        public IActionResult ChangePasswordSuccess(UserViewModel viewModel)
        {
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
