using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Management.Interfaces;

namespace MyDomainPasswordChange.Api.Endpoints.HealthChecks;

public class LdapConnectionCheck(IDomainPasswordManagement passwordManagement, IOptions<AdminInfoConfiguration> adminConfig) : IHealthCheck
{
    private readonly IDomainPasswordManagement _passwordManagement = passwordManagement;
    private readonly IOptions<AdminInfoConfiguration> _adminConfig = adminConfig;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return _passwordManagement.AuthenticateUser(_adminConfig.Value.AccountName, _adminConfig.Value.Password)
                ? Task.FromResult(new HealthCheckResult(HealthStatus.Healthy, "The ldap connection is working properly."))
                : Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, "Error on LDAP connection.", data: new Dictionary<string, object> { { "ldap_account_name", _adminConfig.Value.AccountName } }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(HealthStatus.Unhealthy, "Error on LDAP connection.", ex, new Dictionary<string, object> { { "ldap_account_name", _adminConfig.Value.AccountName } }));
        }
    }
}
