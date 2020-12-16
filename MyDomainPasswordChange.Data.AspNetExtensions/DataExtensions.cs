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
    public static class DataExtensions
    {
        public static IServiceCollection AddSqliteDataContext(this IServiceCollection services)
        {
            var connectionString = services.BuildServiceProvider()
                                           .GetService<IConfiguration>()
                                           .GetConnectionString("Sqlite");
            var dataContext = new SqliteDataContext(connectionString);
            services.AddSingleton<DataContext>(dataContext);
            return services;
        }

        public static IServiceCollection AddPasswordHistoryManager(this IServiceCollection services)
        {
            services.AddScoped<IPasswordHistoryManager, PasswordHistoryManager>();
            return services;
        }
    }
}
