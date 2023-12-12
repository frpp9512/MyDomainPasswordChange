using AutoMapper;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Shared.DTO;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;

namespace MyDomainPasswordChange.Api.Endpoints;

public static class PasswordEndpoints
{
    public static WebApplication MapPasswordEndpoints(this WebApplication app)
    {
        var passwordGroup = app.MapGroup("/password")
            .WithDisplayName("Password management")
            .WithDescription("Endpoints for password managements.");

        passwordGroup.MapPost("change", ChangeAccountPasswordAsync)
            .WithName("Change password")
            .WithDisplayName("Change account password")
            .WithDescription("Changes the password of an account.");

        passwordGroup.MapPost("set", SetAccountPasswordAsync)
            .WithName("Set password")
            .WithDisplayName("Set the account password")
            .WithDescription("Set the password of an account.");

        passwordGroup.MapPost("reset", ResetAccountPasswordAsync)
            .WithName("Reset password")
            .WithDisplayName("Reset account password")
            .WithDescription("Reset the password of an account for a temporary default one.");

        return app;
    }

    public static async Task<IResult> ChangeAccountPasswordAsync(ChangePasswordRequestDto changePasswordRequest,
                                                           IDomainPasswordManagement passwordManagement,
                                                           IPasswordHistoryManager _historyManager,
                                                           IOptions<PasswordHistoryConfiguration> passwordHistoryConfiguration,
                                                           IMailNotificator mailNotificator,
                                                           ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(nameof(changePasswordRequest));
        var logger = loggerFactory.CreateLogger("ChangePassword");
        logger.LogInformation("Requested change password for account: {accountName}", changePasswordRequest.AccountName);
        try
        {
            if (!passwordManagement.AuthenticateUser(changePasswordRequest.AccountName, changePasswordRequest.CurrentPassword))
            {
                logger.LogError("Error authenticating the account {accountName}", changePasswordRequest.AccountName);
                return Results.Unauthorized();
            }

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
    }

    public static async Task<IResult> SetAccountPasswordAsync(SetPasswordRequestDto setPasswordRequest,
                                                              IDomainPasswordManagement passwordManagement,
                                                              IPasswordHistoryManager _historyManager,
                                                              IOptions<PasswordHistoryConfiguration> passwordHistoryConfiguration,
                                                              IMailNotificator mailNotificator,
                                                              ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(nameof(setPasswordRequest));
        var logger = loggerFactory.CreateLogger("ChangePassword");
        logger.LogInformation("Requested set password for account: {accountName}", setPasswordRequest.AccountName);
        try
        {
            if (setPasswordRequest.CheckHistory && await _historyManager.CheckPasswordHistoryAsync(setPasswordRequest.AccountName, setPasswordRequest.NewPassword, passwordHistoryConfiguration.Value.LastPasswordHistoryCheck))
            {
                logger.LogError("The password provided for account {accountName} has been used.", setPasswordRequest.AccountName);
                return Results.BadRequest(new ErrorResponseDto(StatusCodes.Status409Conflict, "PasswordInHistory", $"The new password has already been used by the account {setPasswordRequest.AccountName}", new() { { "accountName", setPasswordRequest.AccountName } }));
            }

            passwordManagement.SetUserPassword(setPasswordRequest.AccountName, setPasswordRequest.NewPassword);
            logger.LogInformation("Password established successfully for account {accountName}.", setPasswordRequest.AccountName);

            await _historyManager.RegisterPasswordAsync(setPasswordRequest.AccountName, setPasswordRequest.NewPassword);
            logger.LogInformation("New user password registered in history successfully for account {accountName}.", setPasswordRequest.AccountName);

            var userInfo = await passwordManagement.GetUserInfo(setPasswordRequest.AccountName);
            logger.LogInformation("Retrieving user info for account {accountName}.", setPasswordRequest.AccountName);

            //logger.LogInformation("Sending change password notification mail for {accountName}.", setPasswordRequest.AccountName);
            //await mailNotificator.SendChangePasswordNotificationAsync(setPasswordRequest.AccountName);
            //logger.LogInformation("Sent change password notification mail for {accountName}.", setPasswordRequest.AccountName);

            return Results.Ok(new PasswordChangedDto(
                setPasswordRequest.AccountName,
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
                Detail = $"An error occurred when changing the password of the account: {setPasswordRequest.AccountName}",
                Extensions = { { "ErrorCode", "ChangePasswordError" } }
            });
        }
        catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
        {
            logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "LDAP connection error",
                Detail = $"An error occurred when connecting to the LDAP server: {setPasswordRequest.AccountName}",
                Extensions = { { "ErrorCode", "LDAPError" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError("General error. Error message: {errorMessage}", ex.Message);
        }

        return Results.Problem();
    }

    public static async Task<IResult> ResetAccountPasswordAsync(ResetPasswordRequestDto resetPasswordRequest,
                                                                IDomainPasswordManagement passwordManagement,
                                                                IPasswordHistoryManager _historyManager,
                                                                IOptions<PasswordHistoryConfiguration> passwordHistoryConfiguration,
                                                                IOptions<AdminInfoConfiguration> AdminInfoConfiguration,
                                                                IMailNotificator mailNotificator,
                                                                IMapper mapper,
                                                                ILoggerFactory loggerFactory)
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
    }
}
