using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MyDomainPasswordChange.Api.Endpoints.HealthChecks;

public class BasicHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
                HealthCheckResult.Healthy("A healthy result."));

        // or
        // return Task.FromResult(
        // new HealthCheckResult(
        //    context.Registration.FailureStatus, "An unhealthy result."));
    }
}
