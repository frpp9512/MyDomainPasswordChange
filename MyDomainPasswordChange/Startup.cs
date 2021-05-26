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
using MyDomainPasswordChange.Data.AspNetExtensions;
using MyDomainPasswordChange.Data.Interfaces;
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
            services.AddSqliteDataContext()
                    .AddPasswordHistoryManager();
            services.AddPasswordManagement();
            services.AddMailNotifications();
            services.AddTransient<IChallenger, Challenger>();
            services.AddSingleton<IIpAddressBlacklist, IpAddressBlacklist>();
            services.AddSingleton<IAlertCountingManagement, AlertCountingManagement>();
            services.AddScoped<BlacklistFilter>();

            services.AddLogging();
            services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", config =>
                {
                    config.Cookie.Name = "CookieAuth";
                    config.LoginPath = "/Auth/Login";
                    config.LogoutPath = "/Auth/Logout";
                    config.AccessDeniedPath = "/Auth/AccessDenied";
                });
            services.AddAutoMapper(typeof(Startup).Assembly);
            services.AddTransient<IDependenciesGroupsManagement, DependenciesGroupsManagement>();
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

            app.UseAuthentication();
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
