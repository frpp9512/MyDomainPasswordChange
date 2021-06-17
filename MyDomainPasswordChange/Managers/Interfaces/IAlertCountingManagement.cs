using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public interface IAlertCountingManagement
    {
        public Task CountChallengeFailAsync();
        public Task CountPasswordFailAsync(string accountName);
        public Task CountManagementAuthFailAsync();
    }
}
