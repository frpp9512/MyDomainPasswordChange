using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Models
{
    public class UsersManagementViewModel
    {
        public List<DependencyGroupViewModel> Groups { get; set; } = new List<DependencyGroupViewModel>();

        public int TotalUsers => Groups.Sum(g => g.Users.Count);

        public int GetExpiredPasswordUserCount(int passwordExpirationDays)
            => Groups.Sum(g => g.GetExpiredPasswordUserCount(passwordExpirationDays));

        public int GetCloseToExpirePasswordUserCount(int passwordExpirationDays)
            => Groups.Sum(g => g.GetCloseToExpirePasswordUserCount(passwordExpirationDays));

        public int GetNeverExpiresPasswordUserCount()
            => Groups.Sum(g => g.GetNeverExpiresPasswordUserCount());

        public int GetPendingToSetPasswordUserCount()
            => Groups.Sum(g => g.GetPendingToSetPasswordUserCount());
    }
}