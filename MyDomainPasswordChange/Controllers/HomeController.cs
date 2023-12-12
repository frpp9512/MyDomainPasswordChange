using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Filters;
using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Managers.Interfaces;
using MyDomainPasswordChange.Models;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Controllers;

[ServiceFilter(typeof(BlacklistFilter), IsReusable = true)]
public class HomeController(ILogger<HomeController> logger,
                            IDomainPasswordManagement passwordManagement,
                            IPasswordHistoryManager historyManager,
                            IMailNotificator mailNotificator,
                            IWebHostEnvironment webHostEnvironment,
                            IConfiguration configuration,
                            IChallenger challenger,
                            IAlertCountingManagement countingManagement) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly IDomainPasswordManagement _passwordManagement = passwordManagement;
    private readonly IPasswordHistoryManager _historyManager = historyManager;
    private readonly IMailNotificator _mailNotificator = mailNotificator;
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
    private readonly IConfiguration _configuration = configuration;
    private readonly IChallenger _challenger = challenger;
    private readonly IAlertCountingManagement _countingManagement = countingManagement;

    [HttpGet]
    public IActionResult Index(string accountName = "") =>
        //var userInfo = new UserInfo
        //{
        //    AccountName = "testaccount1",
        //    FirstName = "Test",
        //    LastName = "Account",
        //    DisplayName = "Test Account Porpuse 1",
        //    Email = "testaccount1@ingeco.cu",
        //    Description = "Description of the test account",
        //    Enabled = true,
        //    MailboxCapacity = "100M",
        //    PersonalId = "95120844341",
        //    JobTitle = "Nada con importancia",
        //    Office = "La oficina del terror",
        //    Address = "En algún lugar de la mancha",
        //    AllowedWorkstations = { "Abcd", "Defg", "Hijk" }
        //};
        //await _passwordManagement.CreateNewUserAsync(userInfo, "test.123*-", "Empresa", "Dirección", "accesoJabber", "navInternacionalRest", "mediaUser", "accesoNube", "accesoFtp", "depEmpresa");
        View(model: new ChangePasswordViewModel { Username = accountName });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePasswordAsync(ChangePasswordViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", viewModel);
        }

        if (!_challenger.EvaluateChallengeAnswer(viewModel.ChallengeId, viewModel.ChallengeAnswer))
        {
            ModelState.AddModelError("Challenge failed", "El debe responder correctamente el desafío.");
            await _countingManagement.CountChallengeFailAsync();
            viewModel.ChallengeAnswer = "";
            return View("Index", viewModel);
        }

        try
        {
            if (!_passwordManagement.AuthenticateUser(viewModel.Username, viewModel.Password))
            {
                throw new BadPasswordException($"La contraseña escrita no es correcta.");
            }

            if (await _historyManager.AccountHasEntries(viewModel.Username))
            {
                if (await _historyManager.CheckPasswordHistoryAsync(viewModel.Username, viewModel.NewPassword, _configuration.GetValue<int>("PasswordHistoryCheck")))
                {
                    ModelState.AddModelError("PasswordUsed", "La nueva contraseña ya sido utilizada, debe de definir una contraseña nueva.");
                    return View("Index", viewModel);
                }
            }
            else
            {
                await _historyManager.RegisterPasswordAsync(viewModel.Username, viewModel.Password);
            }

            _passwordManagement.ChangeUserPassword(viewModel.Username, viewModel.Password, viewModel.NewPassword);
            await _historyManager.RegisterPasswordAsync(viewModel.Username, viewModel.NewPassword);
            var userInfo = await _passwordManagement.GetUserInfo(viewModel.Username);
            await _mailNotificator.SendChangePasswordNotificationAsync(viewModel.Username);
            TempData["PasswordChanged"] = true;
            return RedirectToAction("ChangePasswordSuccess", new UserViewModel
            {
                AccountName = userInfo.AccountName,
                DisplayName = userInfo.DisplayName,
                Description = userInfo.Description,
                Email = userInfo.Email,
                PasswordExpirationDays = _configuration.GetValue<int>("passwordExpirationDays")
            });
        }
        catch (BadPasswordException bpex)
        {
            ModelState.AddModelError("BadPassword", bpex.Message);
            await _countingManagement.CountPasswordFailAsync(viewModel.Username);
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult ChangePasswordSuccess(UserViewModel viewModel)
        => TempData["PasswordChanged"] != null && (bool)TempData["PasswordChanged"] ? View(viewModel) : RedirectToAction("Index");

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
    public FileContentResult ChallengePicture(int challengeId)
    {
        try
        {
            _logger.LogInformation($"Requesting challenge image Id: {challengeId}.");
            var challengeImage = _challenger.GetChallengeImage(challengeId);
            _logger.LogInformation($"Obtained challenge image with: {challengeImage?.Width} width.");
            var stream = new MemoryStream();
            challengeImage.Save(stream, ImageFormat.Jpeg);
            return new FileContentResult(stream.ToArray(), new MediaTypeHeaderValue("image/jpg"));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
