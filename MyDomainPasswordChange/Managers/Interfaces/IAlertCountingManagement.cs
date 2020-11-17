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
        public void CountChallengeFail();
        public void CountPasswordFail(string accountName);
    }
}
