using System.Threading.Tasks;

namespace MyDomainPasswordChange.Managers.Interfaces;

public interface IAlertCountingManagement
{
    public Task CountChallengeFailAsync();
    public Task CountPasswordFailAsync(string accountName);
    public Task CountManagementAuthFailAsync();
}
