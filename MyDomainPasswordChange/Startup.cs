using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyDomainPasswordChange.Management;
using MyDomainPasswordChange.Managers;

namespace MyDomainPasswordChange
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<IBindCredentialsProvider>(services => new BindCredentialsProvider(services.GetService<IConfiguration>()));
            services.AddTransient<MyDomainPasswordManagement>();
            services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
            services.AddSingleton<IMyMailService, MyMailService>();
            services.AddTransient<IMailNotificator, MailNotificator>();
            services.AddTransient<IChallenger, Challenger>();
            services.AddSingleton<IIpAddressBlacklist, IpAddressBlacklist>();
            services.AddSingleton<IAlertCountingManagement, AlertCountingManagement>();
            services.AddScoped<BlacklistFilter>();
            services.AddHostedService<TimedHostedService>();
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || true)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
