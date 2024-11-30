using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Authorization.Helpers;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Shared.DTO;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.Protocols;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MyDomainPasswordChange.Api.Endpoints;

public static class AccountEndpoints
{
    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        RouteGroupBuilder accountGroup = app.MapGroup("/account")
            .WithDisplayName("Account management")
            .WithDescription("Endpoints for accounts management");

        _ = accountGroup.MapGet("{accountName}", GetAccountInfoAsync)
                        .RequireAuthorization(AuthorizationConstants.Policies.DOMAIN_ADMINS_POLICY);

        _ = accountGroup.MapPut("", CreateAccountAsync)
                        .RequireAuthorization(AuthorizationConstants.Policies.DOMAIN_ADMINS_POLICY);

        _ = accountGroup.MapDelete("{accountName}", DeleteAccountAsync)
                        .RequireAuthorization(AuthorizationConstants.Policies.DOMAIN_ADMINS_POLICY);

        _ = accountGroup.MapGet("image/{accountName}", GetAccountImageAsync)
                        .WithName("GetAccountImage")
                        .AllowAnonymous();

        _ = accountGroup.MapPut("image/{accountName}", SetAccountImageAsync)
                        .WithName("SetAccountImage")
                        .RequireAuthorization(AuthorizationConstants.Policies.DOMAIN_ADMINS_POLICY)
                        .Produces(403)
                        .Produces(200, contentType: "application/json")
                        .DisableAntiforgery();

        _ = accountGroup.MapPost("auth", AuthAccount)
                        .AllowAnonymous();

        RouteGroupBuilder accountsGroup = app.MapGroup("/accounts")
                               .RequireAuthorization(AuthorizationConstants.Policies.LOCALIZED_DOMAIN_ADMINS_POLICY);

        _ = accountsGroup.MapGet("", GetAccountsAsync);
        _ = accountsGroup.MapGet("{dependencyId}", GetAccountsForDependencyAsync);

        return app;
    }

    private static async Task<IResult> GetAccountInfoAsync(string accountName,
                                                           IDomainPasswordManagement passwordManagement,
                                                           IMapper mapper,
                                                           ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("AccountInfo");
        logger.LogInformation("Requested info for account: {accountName}", accountName);
        try
        {
            UserInfo userInfo = await passwordManagement.GetUserInfoAsync(accountName);
            AccountDto dto = mapper.Map<AccountDto>(userInfo);
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
        ILogger logger = loggerFactory.CreateLogger("CreateAccount");
        DependenciesConfiguration dependenciesConfig = depConfigOptions.Value;
        DefaultAccountConfiguration defaultAccountConfig = defaultAccountConfigOptions.Value;
        try
        {
            if (passwordManagement.UserExists(newAccount.AccountName))
            {
                return Results.Conflict(new ErrorResponseDto(StatusCodes.Status409Conflict, "AccountExists", "", new() { { "AccountName", newAccount.AccountName } }));
            }

            UserInfo userInfo = mapper.Map<UserInfo>(newAccount);
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

            DependencyDefinition dependency = dependenciesConfig[newAccount.DependencyId];

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

            AreaDefinition area = dependency[newAccount.AreaId];

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
                UserInfo createdUserInfo = await passwordManagement.GetUserInfo(newAccount.AccountName);
                AccountDto dto = mapper.Map<AccountDto>(createdUserInfo);
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
        ILogger logger = loggerFactory.CreateLogger("GetAccountImage");
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

    private static async Task<IResult> SetAccountImageAsync([FromRoute] string accountName,
                                                            IFormFile imageFile,
                                                            ILoggerFactory loggerFactory,
                                                            IDomainPasswordManagement passwordManagement)
    {
        ILogger logger = loggerFactory.CreateLogger("SetAccountImage");
        logger.LogInformation("Requested set the image for account: {accountName}", accountName);
        try
        {
            byte[] fileBytes;
            using (MemoryStream memoryStream = new())
            {
                await imageFile.CopyToAsync(memoryStream);
                try
                {
                    var image = Image.FromStream(memoryStream);
                    fileBytes = memoryStream.ToArray();
                    if (fileBytes.Length > (1024 * 100))
                    {
                        logger.LogInformation("The image was too big it will be resized.");
                        image = ResizeImage(image, 90, 90);
                    }

                    using MemoryStream resizedImageMemoryStream = new();
                    image.Save(resizedImageMemoryStream, ImageFormat.Jpeg);
                    fileBytes = resizedImageMemoryStream.ToArray();
                }
                catch (Exception ex)
                {
                    logger.LogError("Error processing the image. Error message: {errorMessage}", ex.Message);
                    return Results.Problem(new()
                    {
                        Title = "Invalid image",
                        Detail = $"The provided account image is invalid.",
                        Extensions = { { "ErrorCode", "InvalidAccountImage" } }
                    });
                }
            }

            await passwordManagement.SetUserImageAsync(accountName, fileBytes);
            return Results.CreatedAtRoute($"GetAccountImage");
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

    private static Bitmap ResizeImage(Image image, int width, int height)
    {
        Rectangle destRect = new(0, 0, width, height);
        Bitmap destImage = new(width, height);
        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using ImageAttributes wrapMode = new();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        }

        return destImage;
    }

    private static IResult DeleteAccountAsync(string accountName,
                                              IDomainPasswordManagement passwordManagement,
                                              ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountName, nameof(accountName));
        ILogger logger = loggerFactory.CreateLogger("DeleteAccount");
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
        ILogger logger = loggerFactory.CreateLogger("AccountAuth");
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
        ILogger logger = loggerFactory.CreateLogger("GetAccountsForDependency");
        logger.LogInformation("Requested account list for dependency {dependency}", dependencyId);
        DependenciesConfiguration dependenciesConfig = depConfigOptions.Value;
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

            DependencyDefinition dependency = dependenciesConfig[dependencyId];
            GroupInfo group = await passwordManagement.GetGroupInfoByNameAsync(dependencyId);
            List<UserInfo> accounts = await passwordManagement.GetActiveUsersInfoFromGroupAsync(group);

            AccountsListDto dto = new()
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
        ILogger logger = loggerFactory.CreateLogger("GetAccounts");
        logger.LogInformation("Requested account list of accounts of all dependencies.");
        DependenciesConfiguration dependenciesConfig = depConfigOptions.Value;
        try
        {
            List<GroupInfo> groups = [];
            foreach (DependencyDefinition groupDefinition in dependenciesConfig.Definitions)
            {
                if (groupDefinition.Type == "global" && includeGlobal is null or false)
                {
                    continue;
                }

                GroupInfo group = await passwordManagement.GetGroupInfoByNameAsync(groupDefinition.GroupName);
                groups.Add(group);
            }

            List<(GroupInfo depGroup, List<UserInfo> accounts)> groupAccounts = [];
            foreach (GroupInfo depGroup in groups)
            {
                List<UserInfo> accounts = await passwordManagement.GetActiveUsersInfoFromGroupAsync(depGroup);
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
