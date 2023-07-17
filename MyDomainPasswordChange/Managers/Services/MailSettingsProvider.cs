using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;

namespace MyDomainPasswordChange.Managers.Services;

public class MailSettingsProvider : IMailSettingsProvider
{
    private readonly IConfiguration _configuration;

    public MailSettingsProvider(IConfiguration configuration) => _configuration = configuration;

    public MailSettings GetMailSettings() => _configuration.GetSection("MailSettings").Get<MailSettings>();
}
