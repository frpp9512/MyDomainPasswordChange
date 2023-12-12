using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MyDomainPasswordChange;

CreateHostBuilder(args).Build().Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            _ = webBuilder.UseIISIntegration();
            _ = webBuilder.UseStartup<Startup>();
        });
