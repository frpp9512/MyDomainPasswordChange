using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MyDomainPasswordChange.Interfaces;
using MyDomainPasswordChange.Models;

namespace MyDomainPasswordChange.Controllers
{
    [ServiceFilter(typeof(BlacklistFilter))]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyDomainPasswordManagement _passwordManagement;
        private readonly IMailNotificator _mailNotificator;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IChallenger _challenger;
        private readonly IAlertCountingManagement _countingManagement;

        public HomeController(ILogger<HomeController> logger,
                              MyDomainPasswordManagement passwordManagement,
                              IMailNotificator mailNotificator,
                              IWebHostEnvironment webHostEnvironment,
                              IConfiguration configuration,
                              IChallenger challenger,
                              IAlertCountingManagement countingManagement)
        {
            _logger = logger;
            _passwordManagement = passwordManagement;
            _mailNotificator = mailNotificator;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _challenger = challenger;
            _countingManagement = countingManagement;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                if (!_challenger.EvaluateChallengeAnswer(viewModel.ChallengeId, viewModel.ChallengeAnswer))
                {
                    ModelState.AddModelError("Challenge failed", "El debe responder correctamente el desafío.");
                    _countingManagement.CountChallengeFail();
                    viewModel.ChallengeAnswer = "";
                    return View("Index", viewModel);
                }
                try
                {
                    _passwordManagement.ChangeUserPassword(viewModel.Username, viewModel.Password, viewModel.NewPassword);
                    var userInfo = _passwordManagement.GetUserInfo(viewModel.Username);
                    await _mailNotificator.SendChangePasswordNotificationAsync(viewModel.Username);
                    return RedirectToAction("ChangePasswordSuccess", new UserViewModel 
                    {
                        AccountName = userInfo.AccountName,
                        Company = userInfo.Company,
                        Department = userInfo.Department,
                        DisplayName = userInfo.DisplayName,
                        Email = userInfo.Email,
                        Title = userInfo.Title,
                        PasswordExpirationDays = _configuration.GetValue<int>("passwordExpirationDays")
                    });
                }
                catch (BadPasswordException bpex)
                {
                    ModelState.AddModelError("BadPassword", bpex.Message);
                    _countingManagement.CountPasswordFail(viewModel.Username);
                    return View("Index");
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
            return View("Index", viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ChangePasswordSuccess(UserViewModel viewModel) => View(viewModel);

        [HttpGet]
        public async Task<FileStreamResult> UserPicture(string accountName)
        {
            var image = await _passwordManagement.GetUserImageBytesAsync(accountName);
            if (image == null)
            {
                var defaultPicture = Path.Combine(_webHostEnvironment.WebRootPath, $"img{Path.DirectorySeparatorChar}default_user.jpg");
                image = await System.IO.File.ReadAllBytesAsync(defaultPicture);
            }
            var stream = new MemoryStream(image);
            return new FileStreamResult(stream, new MediaTypeHeaderValue("image/jpg"))
            {
                FileDownloadName = $"{accountName}.jpeg"
            };
        }

        [HttpGet]
        public IActionResult ChallengePicture(int challengeId)
        {
            try
            {
                var challengeImage = _challenger.GetChallengeImage(challengeId);
                var stream = new MemoryStream();
                challengeImage.Save(stream, ImageFormat.Jpeg);
                return new FileContentResult(stream.ToArray(), new MediaTypeHeaderValue("image/jpg"));
            }
            catch (Exception)
            {
                return null;
            }
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
