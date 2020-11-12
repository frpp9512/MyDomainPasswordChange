using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class MyDomainPasswordManagement
    {
        private readonly IBindCredentialsProvider _credentialsProvider;

        public MyDomainPasswordManagement(IBindCredentialsProvider credentialsProvider)
        {
            _credentialsProvider = credentialsProvider;
        }

        public bool AuthenticateUser(string accountName, string password)
        {
            var context = new PrincipalContext(ContextType.Domain,
                                               _credentialsProvider.GetLdapServer(),
                                               _credentialsProvider.GetLdapSearchBase(),
                                               _credentialsProvider.GetBindUsername(),
                                               _credentialsProvider.GetBindPassword());
            return context.ValidateCredentials(accountName, password);
        }

        public bool UserExists(string accountName) => GetUser(accountName) != null;

        public void ChangeUserPassword(string accountName, string password, string newPassword)
        {
            var user = GetUser(accountName);
            if (user != null)
            {
                if (!AuthenticateUser(accountName, password))
                {
                    throw new PasswordChangeException($"La contraseña escrita no es correcta.");
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

            var info = new UserInfo
            {
                AccountName = accountName,
                Company = userEntry.Properties[LdapAttributes.COMPANY].Value?.ToString(),
                Department = userEntry.Properties[LdapAttributes.DEPARTMENT].Value?.ToString(),
                DisplayName = userEntry.Properties[LdapAttributes.DISPLAYNAME].Value?.ToString(),
                Email = userEntry.Properties[LdapAttributes.EMAIL].Value?.ToString(),
                Title = userEntry.Properties[LdapAttributes.TITLE].Value?.ToString(),
            };
            
            return info;
        }

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

            if (userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value != null)
            {
                return userEntry.Properties[LdapAttributes.JPEG_PHOTO].Value as byte[];
            }

            return null;
        }
    }
}
