﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Models
{
    public class DependencyGroupViewModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public int GetExpiredPasswordUserCount(int passwordExpirationDays)
            => Users.Count(u => u.LastPasswordSet.AddDays(passwordExpirationDays) <= DateTime.Now);

        public int GetCloseToExpirePasswordUserCount(int passwordExpirationDays)
            => Users.Count(u => (u.LastPasswordSet.AddDays(passwordExpirationDays) - DateTime.Now).TotalDays <= 7 && u.LastPasswordSet.AddDays(passwordExpirationDays) > DateTime.Now);
    }
}
