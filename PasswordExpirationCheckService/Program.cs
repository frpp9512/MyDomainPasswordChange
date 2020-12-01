using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyDomainPasswordChange.Management;
using MyDomainPasswordChange.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordExpirationCheckService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(configure => configure.ServiceName = "PasswordExpirationCheck")
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<IBindCredentialsProvider>(services => new BindCredentialsProvider(services.GetService<IConfiguration>()));
                    services.AddTransient<MyDomainPasswordManagement>();
                    services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
                    services.AddSingleton<IMyMailService, MyMailService>();
                    services.AddTransient<IMailNotificator, MailNotificator>();
                    services.AddHostedService<Worker>();
                    
                });
    }
}
