using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
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

        /// <summary>
        /// Authenticates the specified user credentials.
        /// </summary>
        /// <param name="accountName">The account name of the user.</param>
        /// <param name="password">The password of the user account.</param>
        /// <returns><see langword="true"/> if the authentication succeeded.</returns>
        public bool AuthenticateUser(string accountName, string password)
        {
            var context = new PrincipalContext(ContextType.Domain,
                                               _credentialsProvider.GetLdapServer(),
                                               _credentialsProvider.GetLdapSearchBase(),
                                               _credentialsProvider.GetBindUsername(),
                                               _credentialsProvider.GetBindPassword());
            return context.ValidateCredentials(accountName, password);
        }

        /// <summary>
        /// Determines if exists an user account with the provided name.
        /// </summary>
        /// <param name="accountName">The account name to determine if exists an user account with.</param>
        /// <returns><see langword="true"/> if exists an user account with the provided name.</returns>
        public bool UserExists(string accountName) => GetUser(accountName) is not null;

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
        {
            var user = GetUser(accountName);
            if (user != null)
            {
                if (!AuthenticateUser(accountName, password))
                {
                    throw new BadPasswordException($"La contraseña escrita no es correcta.");
                }
                try
                {
                    user.SetPassword(newPassword);
                }
                catch (PasswordException ex)
                {
                    if (ex.Message.Contains("0x800708C5"))
                    {
                        throw new PasswordChangeException($"La nueva contraseña no cumple con los requisitos de seguridad requeridas. Siempre incluya mayúsculas, números y símbolos para hacer su contraseña más segura.");
                    }
                    throw new PasswordChangeException($"Ocurrió un problema a la hora de cambiar la contraseña. El mensaje de error fue: { ex.Message }.");
                }
            }
            else
            {
                throw new UserNotFoundException($"El usuario { accountName } no existe en el dominio.");
            }
        }

        /// <summary>
        /// Gets the LDAP user with the provided account name.
        /// </summary>
        /// <param name="accountName">The account name of the user to search for.</param>
        /// <returns>An instance of <see cref="UserPrincipal"/> that represents the LDAP user founded, otherwise <see langword="null"/>.</returns>
        private UserPrincipal GetUser(string accountName)
        {
            var context = new PrincipalContext(ContextType.Domain,
                                               _credentialsProvider.GetLdapServer(),
                                               _credentialsProvider.GetLdapSearchBase(),
                                               _credentialsProvider.GetBindUsername(),
                                               _credentialsProvider.GetBindPassword());
            var searchUser = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, accountName);
            return searchUser;
        }

        /// <summary>
        /// Gets the LDAP user info with the specified account name.
        /// </summary>
        /// <param name="accountName">The account name of the user to search for.</param>
        /// <returns>An instance of <see cref="UserInfo"/> with the info the LDAP user founded.</returns>
        public UserInfo GetUserInfo(string accountName)
        {
            var entry = new DirectoryEntry($"LDAP://{_credentialsProvider.GetLdapServer()}",
                                           _credentialsProvider.GetBindUsername(),
                                           _credentialsProvider.GetBindPassword());
            var searcher = new DirectorySearcher(entry)
            {
                Filter = $"{LdapAttributes.ACCOUNT_NAME}={accountName}"
            };

            var results = searcher.FindOne();

            var userEntry = results.GetDirectoryEntry();

            var principal = GetUser(accountName);
            bool domainAdmin = false;

            foreach (var group in principal.GetAuthorizationGroups())
            {
                if (group.SamAccountName == "Domain Admins" && group.ContextType == ContextType.Domain)
                {
                    domainAdmin = true;
                    break;
                }
            }


            var info = new UserInfo
            {
                AccountName = accountName,
                Company = userEntry.Properties[LdapAttributes.COMPANY].Value?.ToString(),
                Department = userEntry.Properties[LdapAttributes.DEPARTMENT].Value?.ToString(),
                DisplayName = userEntry.Properties[LdapAttributes.DISPLAYNAME].Value?.ToString(),
                Email = userEntry.Properties[LdapAttributes.EMAIL].Value?.ToString(),
                Title = userEntry.Properties[LdapAttributes.TITLE].Value?.ToString(),
                LastPasswordSet = GetUser(accountName).LastPasswordSet.GetValueOrDefault(),
                Enabled = principal.Enabled.GetValueOrDefault(false),
                IsDomainAdmin = domainAdmin
            };

            return info;
        }

        /// <summary>
        /// Gets all LDAP users info.
        /// </summary>
        /// <returns>The list.</returns>
        public async Task<List<UserInfo>> GetAllActiveUsersInfo()
        {
            var entry = new DirectoryEntry($"LDAP://{_credentialsProvider.GetLdapServer()}",
                                           _credentialsProvider.GetBindUsername(),
                                           _credentialsProvider.GetBindPassword());
            var searcher = new DirectorySearcher(entry)
            {
                Filter = $"{LdapAttributes.OBJECT_CLASS}=user"
            };

            var results = await Task.Run(() => searcher.FindAll());

            var users = new List<UserInfo>();

            foreach (SearchResult result in results)
            {
                var userEntry = result.GetDirectoryEntry();
                var principal = GetUser(userEntry.Properties["sAMAccountName"].Value?.ToString());
                if (principal is not null)
                {
                    if (principal.Enabled.GetValueOrDefault())
                    {
                        var info = new UserInfo
                        {
                            AccountName = principal.SamAccountName,
                            Company = userEntry.Properties[LdapAttributes.COMPANY].Value?.ToString(),
                            Department = userEntry.Properties[LdapAttributes.DEPARTMENT].Value?.ToString(),
                            DisplayName = userEntry.Properties[LdapAttributes.DISPLAYNAME].Value?.ToString(),
                            Email = userEntry.Properties[LdapAttributes.EMAIL].Value?.ToString(),
                            Title = userEntry.Properties[LdapAttributes.TITLE].Value?.ToString(),
                            LastPasswordSet = principal.LastPasswordSet.GetValueOrDefault()
                        };
                        users.Add(info);
                    }
                }
            }
            return users;
        }

        /// <summary>
        /// Gets the user image stored in the LDAP (in attribute "jpegPhoto").
        /// </summary>
        /// <param name="accountName">The account name of the user to search for the image.</param>
        /// <returns>A instance of <see cref="Image"/> with the founded user image, otherwise <see langword="null"/>.</returns>
        public async Task<Image> GetUserImage(string accountName)
        {
            var entry = new DirectoryEntry($"LDAP://{_credentialsProvider.GetLdapServer()}",
                                          _credentialsProvider.GetBindUsername(),
                                          _credentialsProvider.GetBindPassword());
            var searcher = new DirectorySearcher(entry)
            {
                Filter = $"{LdapAttributes.ACCOUNT_NAME}={accountName}"
            };

            var results = await Task.Run(() => searcher.FindOne());
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
            var entry = new DirectoryEntry($"LDAP://{_credentialsProvider.GetLdapServer()}",
                                          _credentialsProvider.GetBindUsername(),
                                          _credentialsProvider.GetBindPassword());
            var searcher = new DirectorySearcher(entry)
            {
                Filter = $"{LdapAttributes.ACCOUNT_NAME}={accountName}"
            };

            var results = await Task.Run(() => searcher.FindOne());
            var userEntry = results.GetDirectoryEntry();

            return userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value != null
                ? userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value as byte[]
                : null;
        }
    }
}
