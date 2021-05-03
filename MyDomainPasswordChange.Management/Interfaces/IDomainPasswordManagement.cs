using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management
{
    public interface IDomainPasswordManagement
    {
        bool AuthenticateUser(string accountName, string password);
        void ChangeUserPassword(string accountName, string password, string newPassword);
        Task<List<UserInfo>> GetAllActiveUsersInfo();
        Task<Image> GetUserImage(string accountName);
        Task<byte[]> GetUserImageBytesAsync(string accountName);
        UserInfo GetUserInfo(string accountName);
        bool UserExists(string accountName);
    }
}