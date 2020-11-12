using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
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

        public bool UserExists(string accountName) => GetUser(accountName) != null;

        public void ChangeUserPassword(string accountName, string password, string newPassword)
        {
            var user = GetUser(accountName);
            if (user != null)
            {
                try
                {
                    user.ChangePassword(password, newPassword);
                }
                catch (PasswordException ex)
                {
                    if (ex.Message.Contains("0x80070056"))
                    {
                        throw new PasswordChangeException($"La contraseña escrita no es correcta.");
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

            return null;
        }
    }
}
