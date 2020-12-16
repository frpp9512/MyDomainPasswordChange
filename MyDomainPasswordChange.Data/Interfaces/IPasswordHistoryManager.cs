using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Interfaces
{
    public interface IPasswordHistoryManager
    {
        Task<bool> CheckPasswordHistory(string accountName, string password, int passwordHistoryCount);
        Task RegisterPassword(string accountName, string password);
    }
}