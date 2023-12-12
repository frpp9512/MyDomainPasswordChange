using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
using MyDomainPasswordChange.Management.Models;
using PasswordExpirationCheckService.Services;

namespace PasswordExpirationCheckService;

public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService(configure => configure.ServiceName = "PasswordExpirationCheck")
            .ConfigureServices((hostContext, services) =>
            {
                var config = services.BuildServiceProvider().GetService<IConfiguration>();
                _ = services.Configure<LdapConnectionConfiguration>(config.GetSection("LdapConnectionConfiguration"));
                _ = services.AddTransient<MyDomainPasswordManagement>();
                _ = services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
                _ = services.AddSingleton<IMyMailService, MyMailService>();
                _ = services.AddTransient<IMailNotificator, MailNotificator>();
                _ = services.AddHostedService<Worker>();

            });
}
