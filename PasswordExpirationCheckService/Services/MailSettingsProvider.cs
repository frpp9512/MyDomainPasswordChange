using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordExpirationCheckService
{
    public class MailSettingsProvider : IMailSettingsProvider
    {
        private readonly IConfiguration _configuration;

        public MailSettingsProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public MailSettings GetMailSettings() => _configuration.GetSection("MailSettings").Get<MailSettings>();
    }
}
