using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyDomainPasswordChange.Data.AspNetExtensions;
using MyDomainPasswordChange.Data.DataManagers;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Filters;
using MyDomainPasswordChange.Managers.Helpers;
using MyDomainPasswordChange.Managers.Interfaces;
using MyDomainPasswordChange.Managers.Services;

namespace MyDomainPasswordChange;

public class Startup
{
    public Startup(IConfiguration configuration) => Configuration = configuration;

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(Startup));
        _ = services.AddHttpContextAccessor();
        _ = services.AddSqliteDataContext()
                .AddPasswordHistoryManager();
        _ = services.AddPasswordManagement();
        _ = services.AddMailNotifications();
        _ = services.AddTransient<IChallenger, Challenger>();
        _ = services.AddSingleton<IIpAddressBlacklist, IpAddressBlacklist>();
        _ = services.AddSingleton<IAlertCountingManagement, AlertCountingManagement>();
        _ = services.AddScoped<BlacklistFilter>();

        _ = services.AddLogging();
        _ = services.AddAuthentication("CookieAuth")
            .AddCookie("CookieAuth", config =>
            {
                config.Cookie.Name = "CookieAuth";
                config.LoginPath = "/Auth/Login";
                config.LogoutPath = "/Auth/Logout";
                config.AccessDeniedPath = "/Auth/AccessDenied";
            });
        //services.AddAutoMapper(typeof(Startup).Assembly);
        _ = services.AddTransient<IDependenciesGroupsManagement, DependenciesGroupsManagement>();
        _ = services.AddControllersWithViews();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || true)
        {
            _ = app.UseDeveloperExceptionPage();
        }
        else
        {
            _ = app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            _ = app.UseHsts();
        }

        _ = app.UseHsts();
        _ = app.UseHttpsRedirection();
        _ = app.UseStaticFiles();

        _ = app.UseRouting();

        _ = app.UseAuthentication();
        _ = app.UseAuthorization();

        _ = app.UseEndpoints(endpoints => endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"));
    }
}