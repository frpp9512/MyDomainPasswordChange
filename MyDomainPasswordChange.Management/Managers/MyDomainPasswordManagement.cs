using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Helpers;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management.Managers;

/// <summary>
/// Manages the LDAP user accounts for getting information and changing passwords.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="MyDomainPasswordManagement"/>.
/// </remarks>
/// <param name="credentialsProvider">The implementation of <see cref="IBindCredentialsProvider"/> to access the bind credential info.</param>
public class MyDomainPasswordManagement(IOptions<LdapConnectionConfiguration> connectionOptions, IOptions<AuthConfiguration> authConfig) : IDomainPasswordManagement
{
    private readonly LdapConnectionConfiguration _connectionOptions = connectionOptions.Value;
    private readonly AuthConfiguration _authConfig = authConfig.Value;

    public async Task<bool> CreateNewUserAsync(UserInfo userInfo, string password, string dependencyOU, string areaOU, params string[] groups)
    {
        PrincipalContext context = GenerateContext();

        // Create the user.
        UserPrincipal userPrincipal = new(context, userInfo.AccountName, password, userInfo.Enabled)
        {
            Name = userInfo.DisplayName,
            DisplayName = userInfo.DisplayName,
            EmailAddress = userInfo.Email,
            Description = userInfo.Description,
            PasswordNeverExpires = userInfo.PasswordNeverExpires
        };
        foreach (var workstation in userInfo.AllowedWorkstations)
        {
            userPrincipal.PermittedWorkstations.Add(workstation);
        }

        userPrincipal.Save();

        // Assign it to the groups.
        foreach (var group in groups)
        {
            var groupPrincipal = GroupPrincipal.FindByIdentity(context, group);
            if (groupPrincipal is null)
            {
                continue;
            }

            groupPrincipal.Members.Add(userPrincipal);
            groupPrincipal.Save();
        }

        // Move it to the organizational unit.
        using DirectoryEntry directoryEntry = GetDirectoryEntry();
        DirectorySearcher userSearcher = new(directoryEntry)
        {
            Filter = $"{LdapAttributesConstants.ACCOUNT_NAME}={userPrincipal.SamAccountName}"
        };

        SearchResult userResult = await Task.Run(userSearcher.FindOne);
        DirectoryEntry userEntry = userResult.GetDirectoryEntry();

        // Setting the other values.
        userEntry.Properties[LdapAttributesConstants.GIVEN_NAME].Value = userInfo.FirstName;
        userEntry.Properties[LdapAttributesConstants.SURNAME].Value = userInfo.LastName;
        userEntry.Properties[LdapAttributesConstants.PO_BOX].Value = userInfo.MailboxCapacity;
        userEntry.Properties[LdapAttributesConstants.GID_NUMBER].Value = userInfo.PersonalId;
        userEntry.Properties[LdapAttributesConstants.ADDRESS].Value = userInfo.Address;
        userEntry.Properties[LdapAttributesConstants.TITLE].Value = userInfo.JobTitle;
        userEntry.Properties[LdapAttributesConstants.OFFICE].Value = userInfo.Office;
        userEntry.CommitChanges();

        // Finding Dependency OU
        DirectorySearcher dependencyOUSearcher = new(directoryEntry)
        {
            Filter = $"(&(objectClass=organizationalUnit)(name={dependencyOU}))",
            SearchScope = SearchScope.Subtree,
        };
        SearchResult dependencyOUResult = await Task.Run(dependencyOUSearcher.FindOne);
        DirectoryEntry dependencyOUEntry = dependencyOUResult.GetDirectoryEntry();

        // Finding Area OU
        DirectorySearcher areaOUSearcher = new(directoryEntry)
        {
            Filter = $"(&(objectClass=organizationalUnit)(name={areaOU}))",
            SearchScope = SearchScope.Subtree,
            SearchRoot = dependencyOUEntry
        };
        SearchResult areaOUResult = await Task.Run(areaOUSearcher.FindOne);
        DirectoryEntry areaOUEntry = areaOUResult.GetDirectoryEntry();

        userEntry.MoveTo(areaOUEntry);
        userEntry.CommitChanges();

        return true;
    }

    public void DeleteAccount(string accountName)
    {
        using PrincipalContext context = GetPrincipalContext();
        UserPrincipal userPrincipal = GetUserPrincipal(context, accountName) ?? throw new UserNotFoundException($"The user {accountName} is not registered in the domain.");
        userPrincipal.Delete();
    }

    /// <summary>
    /// Authenticates the specified user credentials.
    /// </summary>
    /// <param name="accountName">The account name of the user.</param>
    /// <param name="password">The password of the user account.</param>
    /// <returns><see langword="true"/> if the authentication succeeded.</returns>
    public bool AuthenticateUser(string accountName, string password)
    {
        return AuthenticateUserAsync(accountName, password).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Authenticates the specified user credentials.
    /// </summary>
    /// <param name="accountName">The account name of the user.</param>
    /// <param name="password">The password of the user account.</param>
    /// <returns><see langword="true"/> if the authentication succeeded.</returns>
    public async Task<bool> AuthenticateUserAsync(string accountName, string password)
    {
        var apiBaseUri = new Uri(_authConfig.AuthApiBaseUrl);
        var client = new HttpClient
        {
            BaseAddress = apiBaseUri
        };

        var authRequest = new
        {
            accountName,
            password
        };

        var response = await client.PostAsJsonAsync("/auth", authRequest);

        return response.IsSuccessStatusCode;
    }

    private PrincipalContext GenerateContext() => new(ContextType.Domain,
                                               _connectionOptions.LdapServer,
                                               _connectionOptions.LdapSearchBase,
                                               _connectionOptions.LdapBindUsername,
                                               _connectionOptions.LdapBindPassword);

    /// <summary>
    /// Determines if exists an user account with the provided name.
    /// </summary>
    /// <param name="accountName">The account name to determine if exists an user account with.</param>
    /// <returns><see langword="true"/> if exists an user account with the provided name.</returns>
    public bool UserExists(string accountName)
    {
        using PrincipalContext context = GetPrincipalContext();
        return GetUserPrincipal(context, accountName) is not null;
    }

    /// <summary>
    /// Changes the password of the specified user.
    /// It checks if the current passwords match and then sets the new one.
    /// </summary>
    /// <exception cref="UserNotFoundException"></exception>
    /// <exception cref="PasswordChangeException"></exception>
    /// <param name="accountName"></param>
    /// <param name="password"></param>
    /// <param name="newPassword"></param>
    public void ChangeUserPassword(string accountName, string password, string newPassword) => SetPassword(accountName, password, newPassword);

    public void SetUserPassword(string accountName, string newPassword) => SetPassword(accountName, "", newPassword, false);

    public void ResetPassword(string accountName, string tempPassword) => SetPassword(accountName, "", tempPassword, false, true);

    private void SetPassword(string accountName, string password, string newPassword, bool authenticate = true, bool setAsTempPassword = false)
    {
        using PrincipalContext context = GetPrincipalContext();
        UserPrincipal user = GetUserPrincipal(context, accountName) ?? throw new UserNotFoundException($"El usuario {accountName} no existe en el dominio.");

        if (authenticate && !AuthenticateUser(accountName, password))
        {
            throw new BadPasswordException($"La contraseña escrita no es correcta.");
        }

        try
        {
            user.SetPassword(newPassword);
            if (setAsTempPassword)
            {
                user.ExpirePasswordNow();
            }
        }
        catch (PasswordException ex)
        {
            if (ex.Message.Contains("0x800708C5"))
            {
                throw new PasswordChangeException($"La nueva contraseña no cumple con los requisitos de seguridad requeridas. Siempre incluya mayúsculas, números y símbolos para hacer su contraseña más segura.");
            }

            throw new PasswordChangeException($"Ocurrió un problema a la hora de cambiar la contraseña. El mensaje de error fue: {ex.Message}.");
        }
    }

    /// <summary>
    /// Gets the LDAP user with the provided account name.
    /// </summary>
    /// <param name="accountName">The account name of the user to search for.</param>
    /// <returns>An instance of <see cref="UserPrincipal"/> that represents the LDAP user founded, otherwise <see langword="null"/>.</returns>
    private static UserPrincipal GetUserPrincipal(PrincipalContext context, string accountName)
    {
        var searchUser = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, accountName);
        return searchUser;
    }

    /// <summary>
    /// Gets the <see cref="PrincipalContext"/> with the configured settings.
    /// </summary>
    /// <returns></returns>
    private PrincipalContext GetPrincipalContext() => new(ContextType.Domain,
                                                            _connectionOptions.LdapServer,
                                                            _connectionOptions.LdapSearchBase,
                                                            _connectionOptions.LdapBindUsername,
                                                            _connectionOptions.LdapBindPassword);

    /// <summary>
    /// Get the directory entry with the configured credentials.
    /// </summary>
    /// <returns></returns>
    private DirectoryEntry GetDirectoryEntry() => new($"LDAP://{_connectionOptions.LdapServer}",
                                                        _connectionOptions.LdapBindUsername,
                                                        _connectionOptions.LdapBindPassword);

    /// <summary>
    /// Gets the LDAP user info with the specified account name.
    /// </summary>
    /// <param name="accountName">The account name of the user to search for.</param>
    /// <returns>An instance of <see cref="UserInfo"/> with the info the LDAP user founded.</returns>
    public async Task<UserInfo> GetUserInfo(string accountName)
    {
        using PrincipalContext context = GetPrincipalContext();
        UserPrincipal userPrincipal = GetUserPrincipal(context, accountName) ?? throw new UserNotFoundException($"The user {accountName} is not registered in the domain.");

        UserInfo info = await GetUserInfoFromPrincipal(userPrincipal, true);
        return info;
    }

    /// <summary>
    /// Gets the info of the group with the specified name.
    /// </summary>
    /// <param name="groupName">The name of the group to obtain the <see cref="GroupInfo"/> instance from.</param>
    /// <returns></returns>
    public async Task<GroupInfo> GetGroupInfoByNameAsync(string groupName)
    {
        using PrincipalContext context = GetPrincipalContext();
        GroupPrincipal principal = await Task.Run(() => GroupPrincipal.FindByIdentity(context, groupName));
        return GetGroupInfoFromPrincipal(principal);
    }

    /// <summary>
    /// Gets a <see cref="GroupInfo"/> instance from <see cref="GroupPrincipal"/>.
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    private static GroupInfo GetGroupInfoFromPrincipal(GroupPrincipal principal) => new()
    {
        AccountName = principal.SamAccountName,
        DistinguishedName = principal.DistinguishedName,
        DisplayName = principal.DisplayName,
        Description = principal.Description
    };

    /// <summary>
    /// Load the users that belongs to the specified group.
    /// </summary>
    /// <param name="group">The group which the users belongs to.</param>
    /// <returns></returns>
    public async Task<List<UserInfo>> GetActiveUsersInfoFromGroupAsync(GroupInfo group)
    {
        using PrincipalContext context = GetPrincipalContext();
        GroupPrincipal principal = await Task.Run(() => GroupPrincipal.FindByIdentity(context, group.AccountName));
        List<UserInfo> users = [];
        foreach (UserPrincipal member in principal.GetMembers().Where(member => member is UserPrincipal).Cast<UserPrincipal>())
        {
            if (member.Enabled.GetValueOrDefault())
            {
                UserInfo user = await GetUserInfoFromPrincipal(member, true);
                users.Add(user);
                user.Groups.Add(group);
            }
        }

        return users;
    }

    /// <summary>
    /// Gets all LDAP users info.
    /// </summary>
    /// <returns>The list.</returns>
    public async Task<List<UserInfo>> GetAllActiveUsersInfo()
    {
        using PrincipalContext context = GetPrincipalContext();
        PrincipalSearcher searcher = new(new UserPrincipal(context) { Enabled = true });
        List<UserInfo> users = [];
        var principals = await Task.Run(searcher.FindAll);
        foreach (UserPrincipal result in principals.Where(principal => principal is UserPrincipal).Cast<UserPrincipal>())
        {
            users.Add(await GetUserInfoFromPrincipal(result));
        }

        return users;
    }

    /// <summary>
    /// Gets a instance of <see cref="UserInfo"/> from <see cref="UserPrincipal"/>.
    /// </summary>
    /// <param name="principal"></param>
    /// <param name="loadGroups"></param>
    /// <returns></returns>
    private async Task<UserInfo> GetUserInfoFromPrincipal(UserPrincipal principal, bool loadGroups = false)
    {
        UserInfo userInfo = new()
        {
            AccountName = principal.SamAccountName,
            DisplayName = principal.DisplayName,
            Email = principal.EmailAddress,
            Description = principal.Description,
            LastPasswordSet = principal.LastPasswordSet.GetValueOrDefault(),
            Enabled = principal.Enabled.GetValueOrDefault(),
            PasswordNeverExpires = principal.PasswordNeverExpires,
            AllowedWorkstations = [.. principal.PermittedWorkstations]
        };

        // Move it to the organizational unit.
        using DirectoryEntry directoryEntry = GetDirectoryEntry();
        DirectorySearcher userSearcher = new(directoryEntry)
        {
            Filter = $"{LdapAttributesConstants.ACCOUNT_NAME}={userInfo.AccountName}"
        };

        SearchResult userResult = await Task.Run(userSearcher.FindOne);
        DirectoryEntry userEntry = userResult.GetDirectoryEntry();

        // Setting the other values.
        userInfo.FirstName = userEntry.Properties[LdapAttributesConstants.GIVEN_NAME].Value?.ToString() ?? "";
        userInfo.LastName = userEntry.Properties[LdapAttributesConstants.SURNAME].Value?.ToString() ?? "";
        userInfo.MailboxCapacity = userEntry.Properties[LdapAttributesConstants.PO_BOX].Value?.ToString() ?? "";
        userInfo.PersonalId = userEntry.Properties[LdapAttributesConstants.GID_NUMBER].Value?.ToString() ?? "";
        userInfo.Address = userEntry.Properties[LdapAttributesConstants.ADDRESS].Value?.ToString() ?? "";
        userInfo.JobTitle = userEntry.Properties[LdapAttributesConstants.TITLE].Value?.ToString() ?? "";
        userInfo.Office = userEntry.Properties[LdapAttributesConstants.OFFICE].Value?.ToString() ?? "";

        if (!loadGroups)
        {
            return userInfo;
        }

        foreach (Principal group in principal.GetAuthorizationGroups())
        {
            GroupInfo gi = new()
            {
                AccountName = group.SamAccountName,
                DistinguishedName = group.DistinguishedName,
                DisplayName = group.DisplayName,
                Description = group.Description
            };
            userInfo.Groups.Add(gi);
        }

        return userInfo;
    }

    public List<string> GetAllWorkstations()
    {
        List<string> workstations = [];

        try
        {
            using var entry = GetDirectoryEntry();
            using DirectorySearcher searcher = new DirectorySearcher(entry);
            searcher.Filter = "(&(objectClass=computer)(objectCategory=computer))";
            searcher.PropertiesToLoad.Add("name");

            foreach (SearchResult result in searcher.FindAll())
            {
                if (result.Properties["name"].Count > 0)
                {
                    workstations.Add(result.Properties["name"][0].ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }

        return workstations;
    }

    /// <summary>
    /// Gets the user image stored in the LDAP (in attribute "jpegPhoto").
    /// </summary>
    /// <param name="accountName">The account name of the user to search for the image.</param>
    /// <returns>A instance of <see cref="Image"/> with the founded user image, otherwise <see langword="null"/>.</returns>
    public async Task<Image> GetUserImage(string accountName)
    {
        using DirectoryEntry entry = GetDirectoryEntry();
        DirectorySearcher searcher = new(entry)
        {
            Filter = $"{LdapAttributesConstants.ACCOUNT_NAME}={accountName}"
        };

        SearchResult results = await Task.Run(searcher.FindOne);
        DirectoryEntry userEntry = results.GetDirectoryEntry();

        if (userEntry.Properties[LdapAttributesConstants.JPEG_PHOTO].Value == null)
        {
            return null;
        }

        var photo = userEntry.Properties[LdapAttributesConstants.JPEG_PHOTO].Value as byte[];
        MemoryStream ms = new(photo);
        var image = Image.FromStream(ms);

        return image;
    }

    /// <summary>
    /// Gets the user image stored in the LDAP (in attribute "jpegPhoto").
    /// </summary>
    /// <param name="accountName">The account name of the user to search for the image.</param>
    /// <returns>The image <see cref="byte"/> array of the founded user image, otherwise <see langword="null"/>.</returns>
    public async Task<byte[]> GetUserImageBytesAsync(string accountName)
    {
        using DirectoryEntry entry = GetDirectoryEntry();
        DirectorySearcher searcher = new(entry)
        {
            Filter = $"{LdapAttributesConstants.ACCOUNT_NAME}={accountName}"
        };

        SearchResult results = await Task.Run(searcher.FindOne) ?? throw new UserNotFoundException("Entry not found for {accountName}.");
        DirectoryEntry userEntry = results.GetDirectoryEntry();

        return userEntry.Properties[LdapAttributesConstants.JPEG_PHOTO].Value != null
            ? userEntry.Properties[LdapAttributesConstants.JPEG_PHOTO].Value as byte[]
            : null;
    }

    public async Task SetUserImageAsync(string accountName, byte[] image)
    {
        using DirectoryEntry entry = GetDirectoryEntry();
        DirectorySearcher searcher = new(entry)
        {
            Filter = $"{LdapAttributesConstants.ACCOUNT_NAME}={accountName}"
        };

        SearchResult results = await Task.Run(searcher.FindOne);
        DirectoryEntry userEntry = results.GetDirectoryEntry();
        await Task.Run(() => userEntry.InvokeSet(LdapAttributesConstants.JPEG_PHOTO, image));
        userEntry.CommitChanges();
        userEntry.Close();
    }

    public async Task<UserInfo> GetUserInfoAsync(string accountName) => await Task.Run(() => GetUserInfo(accountName));
}
