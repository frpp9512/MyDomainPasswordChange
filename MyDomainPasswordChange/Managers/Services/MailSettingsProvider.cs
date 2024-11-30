using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Models;

namespace MyDomainPasswordChange.Managers.Services;

public class MailSettingsProvider(IConfiguration configuration) : IMailSettingsProvider
{
    private readonly IConfiguration _configuration = configuration;
    public MailSettings GetMailSettings() => _configuration.GetSection("MailSettings").Get<MailSettings>();
}
