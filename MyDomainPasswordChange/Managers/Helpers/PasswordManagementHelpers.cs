using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
using MyDomainPasswordChange.Managers.Services;

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
        _ = services.AddSingleton<IBindCredentialsProvider>(services => new BindCredentialsProvider(services.GetService<IConfiguration>()));
        _ = services.AddTransient<IDomainPasswordManagement, MyDomainPasswordManagement>();
        return services;
    }
}
