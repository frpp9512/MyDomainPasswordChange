using Microsoft.Extensions.DependencyInjection;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
using MyDomainPasswordChange.Managers.Services;

namespace MyDomainPasswordChange.Managers.Helpers;

/// <summary>
/// A set of extensions methods for the Mail Notification management services.
/// </summary>
public static class MailNotificationsHelper
{
    /// <summary>
    /// Adds all the dependencies needed for the Mail Notification management.
    /// </summary>
    /// <param name="services">The dependency injection container.</param>
    /// <returns>The configured dependency injection container.</returns>
    public static IServiceCollection AddMailNotifications(this IServiceCollection services)
    {
        _ = services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
        _ = services.AddSingleton<IMyMailService, MyMailService>();
        _ = services.AddTransient<IMailNotificator, MailNotificator>();
        return services;
    }
}
