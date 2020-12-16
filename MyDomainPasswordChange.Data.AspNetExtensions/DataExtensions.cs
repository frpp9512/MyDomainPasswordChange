using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyDomainPasswordChange.Data.Contexts;
using MyDomainPasswordChange.Data.DataManagers;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyDomainPasswordChange.Data.AspNetExtensions
{
    /// <summary>
    /// A set of helpers methods to configure the <see cref="IServiceCollection"/> of the Asp.Net application for using the Data Management of the application.
    /// </summary>
    public static class DataExtensions
    {
        /// <summary>
        /// Configures the Data Context of the aplication with a SQLite database.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> implementation of the Asp.Net Core application.</param>
        /// <returns>The configured <see cref="IServiceCollection"/> implementation of the Asp.Net Core application.</returns>
        public static IServiceCollection AddSqliteDataContext(this IServiceCollection services)
        {
            var connectionString = services.BuildServiceProvider()
                                           .GetService<IConfiguration>()
                                           .GetConnectionString("Sqlite");
            var dataContext = new SqliteDataContext(connectionString);
            services.AddSingleton<DataContext>(dataContext);
            return services;
        }

        /// <summary>
        /// Configures the <see cref="IPasswordHistoryManager"/> implementation for the application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> implementation of the Asp.Net Core application.</param>
        /// <returns>The configured <see cref="IServiceCollection"/> implementation of the Asp.Net Core application.</returns>
        public static IServiceCollection AddPasswordHistoryManager(this IServiceCollection services)
        {
            services.AddScoped<IPasswordHistoryManager, PasswordHistoryManager>();
            return services;
        }
    }
}
