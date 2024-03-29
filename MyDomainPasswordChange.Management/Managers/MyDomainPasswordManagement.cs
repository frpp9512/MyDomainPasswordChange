﻿using MyDomainPasswordChange.Management.Excepetions;
using MyDomainPasswordChange.Management.Helpers;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management.Managers;

/// <summary>
/// Manages the LDAP user accounts for getting information and changing passwords.
/// </summary>
public class MyDomainPasswordManagement : IDomainPasswordManagement
{
    private readonly IBindCredentialsProvider _credentialsProvider;

    /// <summary>
    /// Creates a new instance of <see cref="MyDomainPasswordManagement"/>.
    /// </summary>
    /// <param name="credentialsProvider">The implementation of <see cref="IBindCredentialsProvider"/> to access the bind credential info.</param>
    public MyDomainPasswordManagement(IBindCredentialsProvider credentialsProvider) => _credentialsProvider = credentialsProvider;

    public async Task<bool> CreateNewUserAsync(UserInfo userInfo, string password, string dependecyOU, string areaOU, params string[] groups)
    {
        var context = GenerateContext();

        // Create the user.
        var userPrincipal = new UserPrincipal(context, userInfo.AccountName, password, userInfo.Enabled)
        {
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
        using var directoryEntry = GetDirectoryEntry();
        var userSearcher = new DirectorySearcher(directoryEntry)
        {
            Filter = $"{LdapAttributes.ACCOUNT_NAME}={userPrincipal.SamAccountName}"
        };

        var userResult = await Task.Run(userSearcher.FindOne);
        var userEntry = userResult.GetDirectoryEntry();

        // Setting the other values.
        userEntry.Properties[LdapAttributes.GIVEN_NAME].Value = userInfo.FirstName;
        userEntry.Properties[LdapAttributes.SURNAME].Value = userInfo.LastName;
        userEntry.Properties[LdapAttributes.PO_BOX].Value = userInfo.MailboxCapacity;
        userEntry.Properties[LdapAttributes.GID_NUMBER].Value = userInfo.PersonalId;
        userEntry.Properties[LdapAttributes.ADDRESS].Value = userInfo.Address;
        userEntry.Properties[LdapAttributes.TITLE].Value = userInfo.JobTitle;
        userEntry.Properties[LdapAttributes.OFFICE].Value = userInfo.Office;
        userEntry.CommitChanges();

        // Finding Dependency OU
        var dependencyOUSearcher = new DirectorySearcher(directoryEntry)
        {
            Filter = $"(&(objectClass=organizationalUnit)(name={dependecyOU}))",
            SearchScope = SearchScope.Subtree,
        };
        var dependencyOUResult = await Task.Run(dependencyOUSearcher.FindOne);
        var dependencyOUEntry = dependencyOUResult.GetDirectoryEntry();

        // Finding Area OU
        var areaOUSearcher = new DirectorySearcher(directoryEntry)
        {
            Filter = $"(&(objectClass=organizationalUnit)(name={areaOU}))",
            SearchScope = SearchScope.Subtree,
            SearchRoot = dependencyOUEntry
        };
        var areaOUResult = await Task.Run(areaOUSearcher.FindOne);
        var areaOUEntry = areaOUResult.GetDirectoryEntry();

        userEntry.MoveTo(areaOUEntry);
        userEntry.CommitChanges();

        return true;
    }

    /// <summary>
    /// Authenticates the specified user credentials.
    /// </summary>
    /// <param name="accountName">The account name of the user.</param>
    /// <param name="password">The password of the user account.</param>
    /// <returns><see langword="true"/> if the authentication succeeded.</returns>
    public bool AuthenticateUser(string accountName, string password)
    {
        var context = GenerateContext();
        return context.ValidateCredentials(accountName, password, ContextOptions.ServerBind);
    }

    private PrincipalContext GenerateContext() => new PrincipalContext(ContextType.Domain,
                                               _credentialsProvider.GetLdapServer(),
                                               _credentialsProvider.GetLdapSearchBase(),
                                               _credentialsProvider.GetBindUsername(),
                                               _credentialsProvider.GetBindPassword());
    /// <summary>
    /// Determines if exists an user account with the provided name.
    /// </summary>
    /// <param name="accountName">The account name to determine if exists an user account with.</param>
    /// <returns><see langword="true"/> if exists an user account with the provided name.</returns>
    public bool UserExists(string accountName)
    {
        using var context = GetPrincipalContext();
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
    public void ChangeUserPassword(string accountName, string password, string newPassword)
        => SetPassword(accountName, password, newPassword);

    public void SetUserPassword(string accountName, string newPassword)
        => SetPassword(accountName, "", newPassword, false);

    public void ResetPassword(string accountName, string tempPassword)
        => SetPassword(accountName, "", tempPassword, false, true);

    private void SetPassword(string accountName, string password, string newPassword, bool authenticate = true, bool setAsTempPassword = false)
    {
        using var context = GetPrincipalContext();
        var user = GetUserPrincipal(context, accountName) ?? throw new UserNotFoundException($"El usuario {accountName} no existe en el dominio.");

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
                                                            _credentialsProvider.GetLdapServer(),
                                                            _credentialsProvider.GetLdapSearchBase(),
                                                            _credentialsProvider.GetBindUsername(),
                                                            _credentialsProvider.GetBindPassword());

    /// <summary>
    /// Get the directory entry with the configured credentials.
    /// </summary>
    /// <returns></returns>
    private DirectoryEntry GetDirectoryEntry() => new($"LDAP://{_credentialsProvider.GetLdapServer()}",
                                                        _credentialsProvider.GetBindUsername(),
                                                        _credentialsProvider.GetBindPassword());

    /// <summary>
    /// Gets the LDAP user info with the specified account name.
    /// </summary>
    /// <param name="accountName">The account name of the user to search for.</param>
    /// <returns>An instance of <see cref="UserInfo"/> with the info the LDAP user founded.</returns>
    public async Task<UserInfo> GetUserInfo(string accountName)
    {
        using var context = GetPrincipalContext();
        var userPrincipal = GetUserPrincipal(context, accountName) ?? throw new UserNotFoundException($"The user {accountName} is not registered in the domain.");

        var info = await GetUserInfoFromPrincipal(userPrincipal, true);

        return info;
    }

    /// <summary>
    /// Gets the info of the group with the specified name.
    /// </summary>
    /// <param name="groupName">The name of the group to obtain the <see cref="GroupInfo"/> instance from.</param>
    /// <returns></returns>
    public async Task<GroupInfo> GetGroupInfoByNameAsync(string groupName)
    {
        using var context = GetPrincipalContext();
        var principal = await Task.Run(() => GroupPrincipal.FindByIdentity(context, groupName));
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
        using var context = GetPrincipalContext();
        var principal = await Task.Run(() => GroupPrincipal.FindByIdentity(context, group.AccountName));
        var users = new List<UserInfo>();
        foreach (var member in principal.GetMembers().Where(member => member is UserPrincipal).Cast<UserPrincipal>())
        {
            if (member.Enabled.GetValueOrDefault())
            {
                var user = await GetUserInfoFromPrincipal(member, true);
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
        using var context = GetPrincipalContext();
        var searcher = new PrincipalSearcher(new UserPrincipal(context) { Enabled = true });
        var users = new List<UserInfo>();
        var principals = await Task.Run(searcher.FindAll);
        foreach (var result in principals.Where(principal => principal is UserPrincipal).Cast<UserPrincipal>())
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
        var userInfo = new UserInfo
        {
            AccountName = principal.SamAccountName,
            DisplayName = principal.DisplayName,
            Email = principal.EmailAddress,
            Description = principal.Description,
            LastPasswordSet = principal.LastPasswordSet.GetValueOrDefault(),
            Enabled = principal.Enabled.GetValueOrDefault(),
            PasswordNeverExpires = principal.PasswordNeverExpires,
            AllowedWorkstations = principal.PermittedWorkstations.ToList()
        };

        // Move it to the organizational unit.
        using var directoryEntry = GetDirectoryEntry();
        var userSearcher = new DirectorySearcher(directoryEntry)
        {
            Filter = $"{LdapAttributes.ACCOUNT_NAME}={userInfo.AccountName}"
        };

        var userResult = await Task.Run(userSearcher.FindOne);
        var userEntry = userResult.GetDirectoryEntry();

        // Setting the other values.
        userInfo.FirstName = userEntry.Properties[LdapAttributes.GIVEN_NAME].Value?.ToString() ?? "";
        userInfo.LastName = userEntry.Properties[LdapAttributes.SURNAME].Value?.ToString() ?? "";
        userInfo.MailboxCapacity = userEntry.Properties[LdapAttributes.PO_BOX].Value?.ToString() ?? "";
        userInfo.PersonalId = userEntry.Properties[LdapAttributes.GID_NUMBER].Value?.ToString() ?? "";
        userInfo.Address = userEntry.Properties[LdapAttributes.ADDRESS].Value?.ToString() ?? "";
        userInfo.JobTitle = userEntry.Properties[LdapAttributes.TITLE].Value?.ToString() ?? "";
        userInfo.Office = userEntry.Properties[LdapAttributes.OFFICE].Value?.ToString() ?? "";

        if (!loadGroups)
        {
            return userInfo;
        }

        foreach (var group in principal.GetAuthorizationGroups())
        {
            var gi = new GroupInfo
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

    /// <summary>
    /// Gets the user image stored in the LDAP (in attribute "jpegPhoto").
    /// </summary>
    /// <param name="accountName">The account name of the user to search for the image.</param>
    /// <returns>A instance of <see cref="Image"/> with the founded user image, otherwise <see langword="null"/>.</returns>
    public async Task<Image> GetUserImage(string accountName)
    {
        using var entry = GetDirectoryEntry();
        var searcher = new DirectorySearcher(entry)
        {
            Filter = $"{LdapAttributes.ACCOUNT_NAME}={accountName}"
        };

        var results = await Task.Run(searcher.FindOne);
        var userEntry = results.GetDirectoryEntry();

        if (userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value != null)
        {
            var photo = userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value as byte[];
            var ms = new MemoryStream(photo);
            var image = Image.FromStream(ms);

            return image;
        }

        return null;
    }

    /// <summary>
    /// Gets the user image stored in the LDAP (in attribute "jpegPhoto").
    /// </summary>
    /// <param name="accountName">The account name of the user to search for the image.</param>
    /// <returns>The image <see cref="byte"/> array of the founded user image, otherwise <see langword="null"/>.</returns>
    public async Task<byte[]> GetUserImageBytesAsync(string accountName)
    {
        using var entry = GetDirectoryEntry();
        var searcher = new DirectorySearcher(entry)
        {
            Filter = $"{LdapAttributes.ACCOUNT_NAME}={accountName}"
        };

        var results = await Task.Run(searcher.FindOne);
        var userEntry = results.GetDirectoryEntry();

        return userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value != null
            ? userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value as byte[]
            : null;
    }

    public async Task<UserInfo> GetUserInfoAsync(string accountName)
        => await Task.Run(() => GetUserInfo(accountName));
}