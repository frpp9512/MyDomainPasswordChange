using AutoMapper;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Shared.DTO;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;

namespace MyDomainPasswordChange.Api.Endpoints;

public static class AccountEndpoints
{
    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        var accountGroup = app.MapGroup("/account")
            .WithDisplayName("Account management")
            .WithDescription("Endpoints for accounts management");

        _ = accountGroup.MapGet("{accountName}", GetAccountInfoAsync);
        _ = accountGroup.MapPut("", CreateAccountAsync);
        _ = accountGroup.MapDelete("{accountName}", DeleteAccountAsync);
        _ = accountGroup.MapGet("image/{accountName}", GetAccountImageAsync);
        _ = accountGroup.MapPost("auth", AuthAccount);

        var accountsGroup = app.MapGroup("/accounts");

        _ = accountsGroup.MapGet("", GetAccountsAsync);
        _ = accountsGroup.MapGet("{dependencyId}", GetAccountsForDependencyAsync);

        return app;
    }

    private static async Task<IResult> GetAccountInfoAsync(string accountName,
                                                           IDomainPasswordManagement passwordManagement,
                                                           IMapper mapper,
                                                           ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AccountInfo");
        logger.LogInformation("Requested info for account: {accountName}", accountName);
        try
        {
            var userInfo = await passwordManagement.GetUserInfoAsync(accountName);
            var dto = mapper.Map<AccountDto>(userInfo);
            return Results.Ok(dto);
        }
        catch (BadPasswordException ex)
        {
            logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "Password error",
                Detail = $"An error occurred when binding with default account.",
                Extensions = { { "ErrorCode", "ChangePasswordError" } }
            });
        }
        catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
        {
            logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "LDAP connection error",
                Detail = $"An error occurred when connecting to the LDAP server.",
                Extensions = { { "ErrorCode", "LDAPError" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError("General error. Error message: {errorMessage}", ex.Message);
        }

        return Results.Problem();
    }

    private static async Task<IResult> CreateAccountAsync(CreateAccountDto newAccount,
                                                          IDomainPasswordManagement passwordManagement,
                                                          IOptions<DependenciesConfiguration> depConfigOptions,
                                                          IOptions<DefaultAccountConfiguration> defaultAccountConfigOptions,
                                                          HttpContext context,
                                                          IMapper mapper,
                                                          ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(newAccount, nameof(newAccount));
        var logger = loggerFactory.CreateLogger("CreateAccount");
        var dependenciesConfig = depConfigOptions.Value;
        var defaultAccountConfig = defaultAccountConfigOptions.Value;
        try
        {
            if (passwordManagement.UserExists(newAccount.AccountName))
            {
                return Results.Conflict(new ErrorResponseDto(StatusCodes.Status409Conflict, "AccountExists", "", new() { { "AccountName", newAccount.AccountName } }));
            }

            var userInfo = mapper.Map<UserInfo>(newAccount);
            userInfo.MailboxCapacity = defaultAccountConfig.DefaultMailBoxSize;
            userInfo = userInfo with
            {
                MailboxCapacity = defaultAccountConfig.DefaultMailBoxSize,
                Enabled = defaultAccountConfig.DefaultEnabledStatus,
                PasswordNeverExpires = defaultAccountConfig.PasswordNeverExpiresStatus
            };

            if (!dependenciesConfig.ExistDependency(newAccount.DependencyId))
            {
                return Results.BadRequest(new ErrorResponseDto(
                    StatusCodes.Status400BadRequest,
                    "DependencyIncorrect",
                    "The dependency provided doesn't exists.",
                    new Dictionary<string, object>
                    {
                        { "DependencyId", newAccount.DependencyId },
                    }
                ));
            }

            var dependency = dependenciesConfig[newAccount.DependencyId];

            if (!dependency.ExistsArea(newAccount.AreaId))
            {
                return Results.BadRequest(new ErrorResponseDto(
                    StatusCodes.Status400BadRequest,
                    "AreaIncorrect",
                    "The area provided doesn't exists.",
                    new Dictionary<string, object>
                    {
                        { "AreaId", newAccount.AreaId }
                    }
                ));
            }

            var area = dependency[newAccount.AreaId];

            if (!newAccount.GroupsId.Contains(dependency.GroupName))
            {
                newAccount = newAccount with { GroupsId = [.. newAccount.GroupsId, dependency.GroupName] };
            }

            if (!newAccount.GroupsId.Contains(area.GroupName))
            {
                newAccount = newAccount with { GroupsId = [.. newAccount.GroupsId, area.GroupName] };
            }

            if (await passwordManagement.CreateNewUserAsync(userInfo, newAccount.Password, dependency.OU, area.OU, newAccount.GroupsId))
            {
                logger.LogInformation("Account {accountName} created successfully in dependency {dependencyId} and area {areaId}.", newAccount.AccountName, newAccount.DependencyId, newAccount.AreaId);
                var createdUserInfo = await passwordManagement.GetUserInfo(newAccount.AccountName);
                var dto = mapper.Map<AccountDto>(createdUserInfo);
                return Results.Created(new Uri($"{context.Request.Scheme}://{context.Request.Host}/account/{newAccount.AccountName}"), dto);
            }
        }
        catch (BadPasswordException ex)
        {
            logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
            return Results.BadRequest(new ErrorResponseDto(StatusCodes.Status500InternalServerError, "PasswordError", $"An error occurred when changing the password of the account: {newAccount.AccountName}", new() { { "accountName", newAccount.AccountName } }));
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
    }

    private static async Task<IResult> GetAccountImageAsync(string accountName,
                                                            ILoggerFactory loggerFactory,
                                                            IDomainPasswordManagement passwordManagement)
    {
        var logger = loggerFactory.CreateLogger("GetAccountImage");
        logger.LogInformation("Requested the image for account: {accountName}", accountName);
        try
        {
            var picture = await passwordManagement.GetUserImageBytesAsync(accountName);
            if (picture is null)
            {
                logger.LogWarning("The account {accountName} have not image.", accountName);
                return Results.NotFound(new ErrorResponseDto(StatusCodes.Status404NotFound, "AccountHaveNoImage", $"The account {accountName} have not image.", new() { { "accountName", accountName } }));
            }

            return Results.File(picture, "image/jpg", $"{accountName}_picture.jpg");
        }
        catch (UserNotFoundException ex)
        {
            logger.LogError("The account {accountName} was not found.", accountName);
            return Results.BadRequest(new ErrorResponseDto(StatusCodes.Status400BadRequest, "AccountNotFound", $"The account {accountName} was not found. Error message: {ex.Message}", new() { { "accountName", accountName } }));
        }
        catch (BadPasswordException ex)
        {
            logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "Password error",
                Detail = $"An error occurred when binding with default account.",
                Extensions = { { "ErrorCode", "ChangePasswordError" } }
            });
        }
        catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
        {
            logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "LDAP connection error",
                Detail = $"An error occurred when connecting to the LDAP server.",
                Extensions = { { "ErrorCode", "LDAPError" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError("General error. Error message: {errorMessage}", ex.Message);
        }

        return Results.Problem();
    }

    private static IResult DeleteAccountAsync(string accountName,
                                              IDomainPasswordManagement passwordManagement,
                                              ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountName, nameof(accountName));
        var logger = loggerFactory.CreateLogger("DeleteAccount");
        logger.LogInformation("Requested delete the account {accountName}", accountName);
        try
        {
            passwordManagement.DeleteAccount(accountName);
            logger.LogInformation("Account {accountName} deleted successfully.", accountName);
            return Results.Ok($"Account {accountName} deleted successfully.");
        }
        catch (UserNotFoundException ex)
        {
            logger.LogError("The account {accountName} was not found.", accountName);
            return Results.BadRequest(new ErrorResponseDto(StatusCodes.Status400BadRequest, "AccountNotFound", $"The account {accountName} was not found. Error message: {ex.Message}", new() { { "accountName", accountName } }));
        }
        catch (BadPasswordException ex)
        {
            logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
            return Results.BadRequest(new ErrorResponseDto(StatusCodes.Status500InternalServerError, "PasswordError", $"An error occurred when changing the password of the account: {accountName}", new() { { "accountName", accountName } }));
        }
        catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
        {
            logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "LDAP connection error",
                Detail = $"An error occurred when connecting to the LDAP server: {accountName}",
                Extensions = { { "ErrorCode", "LDAPError" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError("General error. Error message: {errorMessage}", ex.Message);
        }

        return Results.Problem();
    }

    private static IResult AuthAccount(AccountAuthDto accountAuthRequest,
                                       IDomainPasswordManagement passwordManagement,
                                       ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("AccountAuth");
        logger.LogInformation("Requested authentication for account {accountName}", accountAuthRequest.AccountName);
        try
        {
            if (!passwordManagement.AuthenticateUser(accountAuthRequest.AccountName, accountAuthRequest.Password))
            {
                logger.LogError("Error authenticating the account {accountName}", accountAuthRequest.AccountName);
                return Results.Unauthorized();
            }

            logger.LogInformation("Account {accountName} authentication success.", accountAuthRequest.AccountName);
            return Results.Ok($"Account {accountAuthRequest.AccountName} authentication success.");
        }
        catch (BadPasswordException ex)
        {
            logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "Password error",
                Detail = $"An error occurred when binding with default account.",
                Extensions = { { "ErrorCode", "ChangePasswordError" } }
            });
        }
        catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
        {
            logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "LDAP connection error",
                Detail = $"An error occurred when connecting to the LDAP server.",
                Extensions = { { "ErrorCode", "LDAPError" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError("General error. Error message: {errorMessage}", ex.Message);
        }

        return Results.Problem();
    }

    private static async Task<IResult> GetAccountsForDependencyAsync(string dependencyId,
                                                                     string? areaId,
                                                                     IDomainPasswordManagement passwordManagement,
                                                                     IOptions<DependenciesConfiguration> depConfigOptions,
                                                                     IMapper mapper,
                                                                     ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(dependencyId, nameof(dependencyId));
        var logger = loggerFactory.CreateLogger("GetAccountsForDependency");
        logger.LogInformation("Requested account list for dependency {dependency}", dependencyId);
        var dependenciesConfig = depConfigOptions.Value;
        try
        {
            if (!dependenciesConfig.ExistDependency(dependencyId))
            {
                return Results.BadRequest(new ErrorResponseDto(
                    StatusCodes.Status400BadRequest,
                    "DependencyIncorrect",
                    "The dependency provided doesn't exists.",
                    new Dictionary<string, object>
                    {
                        { "DependencyId", dependencyId },
                    }
                ));
            }

            var dependency = dependenciesConfig[dependencyId];
            var group = await passwordManagement.GetGroupInfoByNameAsync(dependencyId);
            var accounts = await passwordManagement.GetActiveUsersInfoFromGroupAsync(group);

            var dto = new AccountsListDto
            {
                GroupInfo = mapper.Map<GroupInfoDto>(group),
                Accounts = areaId is null
                    ? accounts.Select(mapper.Map<AccountDto>).ToList()
                    : accounts.Where(a => a.Groups.Any(g => g.AccountName == areaId)).Select(mapper.Map<AccountDto>).ToList()
            };

            return Results.Ok(dto);
        }
        catch (BadPasswordException ex)
        {
            logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
            return Results.BadRequest(new ErrorResponseDto(StatusCodes.Status500InternalServerError, "PasswordError", $"An error occurred when changing the password of the default account.", []));
        }
        catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
        {
            logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "LDAP connection error",
                Detail = $"An error occurred when connecting to the LDAP server.",
                Extensions = { { "ErrorCode", "LDAPError" } }
            });
        }
        catch (Exception ex)
        {
            logger.LogError("General error. Error message: {errorMessage}", ex.Message);
        }

        return Results.Problem();
    }

    private static async Task<IResult> GetAccountsAsync(bool? includeGlobal,
                                                        IDomainPasswordManagement passwordManagement,
                                                        IOptions<DependenciesConfiguration> depConfigOptions,
                                                        IMapper mapper,
                                                        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("GetAccounts");
        logger.LogInformation("Requested account list of accounts of all dependencies.");
        var dependenciesConfig = depConfigOptions.Value;
        try
        {
            List<GroupInfo> groups = [];
            foreach (var groupDefinition in dependenciesConfig.Definitions)
            {
                if (groupDefinition.Type == "global" && includeGlobal is null or false)
                {
                    continue;
                }

                var group = await passwordManagement.GetGroupInfoByNameAsync(groupDefinition.GroupName);
                groups.Add(group);
            }

            List<(GroupInfo depGroup, List<UserInfo> accounts)> groupAccounts = [];
            foreach (var depGroup in groups)
            {
                var accounts = await passwordManagement.GetActiveUsersInfoFromGroupAsync(depGroup);
                groupAccounts.Add((depGroup, accounts));
            }

            var dtos = groupAccounts.Select(
                ga => new AccountsListDto
                {
                    GroupInfo = mapper.Map<GroupInfoDto>(ga.depGroup),
                    Accounts = ga.accounts.Select(a => mapper.Map<AccountDto>(a)).ToList()
                }).ToList();

            return Results.Ok(dtos);
        }
        catch (BadPasswordException ex)
        {
            logger.LogError("Error on password. Error message: {errorMessage}", ex.Message);
            return Results.BadRequest(new ErrorResponseDto(StatusCodes.Status500InternalServerError, "PasswordError", $"An error occurred when changing the password of the default account.", []));
        }
        catch (Exception ex) when (ex is PrincipalServerDownException or LdapException or ActiveDirectoryServerDownException)
        {
            logger.LogError("Error connecting to the LDAP server. Error message: {errorMessage}", ex.Message);
            return Results.Problem(new()
            {
                Title = "LDAP connection error",
                Detail = $"An error occurred when connecting to the LDAP server.",
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
