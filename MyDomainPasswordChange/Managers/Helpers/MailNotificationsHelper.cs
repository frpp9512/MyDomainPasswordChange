using Microsoft.Extensions.DependencyInjection;
using MyDomainPasswordChange.Management;
using MyDomainPasswordChange.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    /// <summary>
    /// A set of extensions methods for the Mail Notification managament services.
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
            services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
            services.AddSingleton<IMyMailService, MyMailService>();
            services.AddTransient<IMailNotificator, MailNotificator>();
            return services;
        }
    }
}
