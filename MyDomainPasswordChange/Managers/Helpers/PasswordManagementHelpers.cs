using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Managers.Services;
using System.Configuration;

namespace MyDomainPasswordChange.Managers.Helpers;

/// <summary>
/// A set of extensions methods for the Password Management service.
/// </summary>
public static class PasswordManagementHelpers
{
    /// <summary>
    /// Adds all the dependencies needed for the Password Management.
    /// </summary>
    /// <param name="services">The dependency injection container.</param>
    /// <returns>The configured dependency injection container.</returns>
    public static IServiceCollection AddPasswordManagement(this IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
        _ = services.Configure<LdapConnectionConfiguration>(configuration.GetSection("LdapConnectionConfiguration"));
        _ = services.AddTransient<IDomainPasswordManagement, MyDomainPasswordManagement>();
        return services;
    }
}
