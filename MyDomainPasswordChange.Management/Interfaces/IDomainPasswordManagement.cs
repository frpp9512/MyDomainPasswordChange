using MyDomainPasswordChange.Management.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Management.Interfaces;

public interface IDomainPasswordManagement
{
    bool AuthenticateUser(string accountName, string password);
    void ChangeUserPassword(string accountName, string password, string newPassword);
    void SetUserPassword(string accountName, string newPassword);
    void ResetPassword(string accountName, string tempPassword);
    Task<UserInfo> GetUserInfo(string accountName);
    Task<UserInfo> GetUserInfoAsync(string accountName);
    bool UserExists(string accountName);
    Task<List<UserInfo>> GetAllActiveUsersInfo();
    Task<List<UserInfo>> GetActiveUsersInfoFromGroupAsync(GroupInfo group);
    Task<GroupInfo> GetGroupInfoByNameAsync(string groupName);
    Task<Image> GetUserImage(string accountName);
    Task<byte[]> GetUserImageBytesAsync(string accountName);
    Task<bool> CreateNewUserAsync(UserInfo userInfo, string password, string dependecyOU, string areaOU, params string[] groups);
}