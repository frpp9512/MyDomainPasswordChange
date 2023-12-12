using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Data.Contexts;
using MyDomainPasswordChange.Data.DataManagers;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Data.Sqlite;
using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Managers.Services;
using MyDomainPasswordChange.Shared.DTO;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Net.Mime;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IBindCredentialsProvider>(services => new BindCredentialsProvider(services.GetService<IConfiguration>()));
builder.Services.AddTransient<IDomainPasswordManagement, MyDomainPasswordManagement>();

builder.Services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
builder.Services.AddSingleton<IMyMailService, MyMailService>();
builder.Services.AddTransient<IMailNotificator, MailNotificator>();

var connectionString = builder.Configuration.GetConnectionString("Sqlite");
var dataContext = new SqliteDataContext(connectionString);
builder.Services.AddSingleton<DataContext>(dataContext);
builder.Services.AddScoped<IPasswordHistoryManager, PasswordHistoryManager>();

builder.Services.AddSingleton<IIpAddressBlacklist, IpAddressBlacklist>();
builder.Services.Configure<PasswordHistoryConfiguration>(builder.Configuration.GetSection("PasswordHistoryConfiguration"));
builder.Services.Configure<AdminInfoConfiguration>(builder.Configuration.GetSection("AdminInfoConfiguration"));

builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/hello", () => Results.Ok("Hello World!"));

var passwordGroup = app.MapGroup("/password")
    .WithDisplayName("Password management")
    .WithDescription("Endpoints for password managements.");

passwordGroup.MapPost("change", async (ChangePasswordRequestDto changePasswordRequest,
                                       IDomainPasswordManagement passwordManagement,
                                       IPasswordHistoryManager _historyManager,
                                       IOptions<PasswordHistoryConfiguration> passwordHistoryConfiguration,
                                       IMailNotificator mailNotificator,
                                       ILoggerFactory loggerFactory) =>
{
    ArgumentNullException.ThrowIfNull(nameof(changePasswordRequest));
    var logger = loggerFactory.CreateLogger("ChangePassword");
    try
    {
        if (!passwordManagement.AuthenticateUser(changePasswordRequest.AccountName, changePasswordRequest.CurrentPassword))
        {
            return Results.Unauthorized();
        }

        logger.LogInformation("Requested change password for account: {accountName}", changePasswordRequest.AccountName);
        if (await _historyManager.CheckPasswordHistoryAsync(changePasswordRequest.AccountName, changePasswordRequest.NewPassword, passwordHistoryConfiguration.Value.LastPasswordHistoryCheck))
        {
            logger.LogError("The password provided for account {accountName} is incorrect.", changePasswordRequest.AccountName);
            return Results.BadRequest();
        }

        passwordManagement.ChangeUserPassword(changePasswordRequest.AccountName, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword);
        logger.LogInformation("Password changed successfully for account {accountName}.", changePasswordRequest.AccountName);

        await _historyManager.RegisterPasswordAsync(changePasswordRequest.AccountName, changePasswordRequest.NewPassword);
        logger.LogInformation("New user password registered in history successfully for account {accountName}.", changePasswordRequest.AccountName);

        var userInfo = await passwordManagement.GetUserInfo(changePasswordRequest.AccountName);
        logger.LogInformation("Retrieving user info for account {accountName}.", changePasswordRequest.AccountName);

        logger.LogInformation("Sending change password notification mail for {accountName}.", changePasswordRequest.AccountName);
        await mailNotificator.SendChangePasswordNotificationAsync(changePasswordRequest.AccountName);
        logger.LogInformation("Sent change password notification mail for {accountName}.", changePasswordRequest.AccountName);

        return Results.Ok(new PasswordChangedDto(
            changePasswordRequest.AccountName,
            userInfo.DisplayName,
            userInfo.Description,
            userInfo.Email,
            passwordHistoryConfiguration.Value.PasswordExpirationDays
        ));
    }
    catch (Exception ex) when (ex is PasswordChangeException or BadPasswordException)
    {
        logger.LogError("Error on password changing process. Error message: {errorMessage}", ex.Message);
        return Results.Problem(new()
        {
            Title = "Password changing error",
            Detail = $"An error occurred when changing the password of the account: {changePasswordRequest.AccountName}",
            Extensions = { { "ErrorCode", "ChangePasswordError" } }
        });
    }
    catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
    {
        logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
        return Results.Problem(new()
        {
            Title = "LDAP connection error",
            Detail = $"An error occurred when connecting to the LDAP server: {changePasswordRequest.AccountName}",
            Extensions = { { "ErrorCode", "LDAPError" } }
        });
    }
    catch (Exception ex)
    {
        logger.LogError("General error. Error message: {errorMessage}", ex.Message);
    }

    return Results.Problem();
})
    .WithName("Change password")
    .WithDisplayName("Change account password")
    .WithDescription("Changes the password of an account.");

passwordGroup.MapGet("reset", async (ResetPasswordRequestDto resetPasswordRequest,
                                     IDomainPasswordManagement passwordManagement,
                                     IPasswordHistoryManager _historyManager,
                                     IOptions<PasswordHistoryConfiguration> passwordHistoryConfiguration,
                                     IOptions<AdminInfoConfiguration> AdminInfoConfiguration,
                                     IMailNotificator mailNotificator,
                                     IMapper mapper,
                                     ILoggerFactory loggerFactory) =>
{
    ArgumentNullException.ThrowIfNull(nameof(resetPasswordRequest));
    var logger = loggerFactory.CreateLogger("ResetPassword");
    try
    {
        if (!passwordManagement.AuthenticateUser(resetPasswordRequest.AccountName, resetPasswordRequest.CurrentPassword))
        {
            return Results.Unauthorized();
        }

        logger.LogInformation("Requested reset password for account: {accountName}", resetPasswordRequest.AccountName);
        passwordManagement.ResetPassword(resetPasswordRequest.AccountName, resetPasswordRequest.TempPassword);
        var userInfo = await passwordManagement.GetUserInfo(resetPasswordRequest.AccountName);
        await mailNotificator.SendManagementUserPasswordResetted(
                    userInfo,
                    (AdminInfoConfiguration.Value.Name, AdminInfoConfiguration.Value.Email));
    }
    catch (Exception ex) when (ex is PasswordChangeException or BadPasswordException)
    {
        logger.LogError("Error on password changing process. Error message: {errorMessage}", ex.Message);
        return Results.Problem(new()
        {
            Title = "Password changing error",
            Detail = $"An error occurred when changing the password of the account: {resetPasswordRequest.AccountName}",
            Extensions = { { "ErrorCode", "ChangePasswordError" } }
        });
    }
    catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
    {
        logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
        return Results.Problem(new()
        {
            Title = "LDAP connection error",
            Detail = $"An error occurred when connecting to the LDAP server: {resetPasswordRequest.AccountName}",
            Extensions = { { "ErrorCode", "LDAPError" } }
        });
    }
    catch (Exception ex)
    {
        logger.LogError("General error. Error message: {errorMessage}", ex.Message);
    }

    return Results.Problem();
});

var accountGroup = app.MapGroup("/account")
    .WithDisplayName("Account management")
    .WithDescription("Endpoints for accounts management");

accountGroup.MapPut("", async (CreateAccountDto newAccount, IDomainPasswordManagement passwordManagement, IMapper mapper, ILoggerFactory loggerFactory) =>
{
    ArgumentNullException.ThrowIfNull(newAccount, nameof(newAccount));
    var logger = loggerFactory.CreateLogger("CreateAccount");
    try
    {
        if (passwordManagement.UserExists(newAccount.AccountName))
        {
            return Results.Conflict(new { ErrorCode = "AccountExists", newAccount.AccountName });
        }

        var userInfo = mapper.Map<UserInfo>(newAccount);

        if (await passwordManagement.CreateNewUserAsync(userInfo, newAccount.Password, newAccount.DependencyId, newAccount.AreaId, newAccount.GroupsId))
        {
            return Results.Created();
        }
    }
    catch (BadPasswordException ex)
    {
        logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
        return Results.Problem(new()
        {
            Title = "Password error",
            Detail = $"An error occurred when changing the password of the account: {newAccount.AccountName}",
            Extensions = { { "ErrorCode", "ChangePasswordError" } }
        });
    }
    catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
    {
        logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
        return Results.Problem(new()
        {
            Title = "LDAP connection error",
            Detail = $"An error occurred when connecting to the LDAP server: {newAccount.AccountName}",
            Extensions = { { "ErrorCode", "LDAPError" } }
        });
    }
    catch (Exception ex)
    {
        logger.LogError("General error. Error message: {errorMessage}", ex.Message);
    }

    return Results.Problem();

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
});

accountGroup.MapGet("image/{accountName}", async (string accountName,
                                            IDomainPasswordManagement passwordManagement) =>
{
    var picture = await passwordManagement.GetUserImageBytesAsync(accountName);
    return Results.File(picture, "image/jpg", $"{accountName}_picture.jpg");
});

app.Run();
