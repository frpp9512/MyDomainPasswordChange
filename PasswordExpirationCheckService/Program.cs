using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
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
                _ = services.AddTransient<IBindCredentialsProvider>(services => new BindCredentialsProvider(services.GetService<IConfiguration>()));
                _ = services.AddTransient<MyDomainPasswordManagement>();
                _ = services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
                _ = services.AddSingleton<IMyMailService, MyMailService>();
                _ = services.AddTransient<IMailNotificator, MailNotificator>();
                _ = services.AddHostedService<Worker>();

            });
}
