using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Authorization.Requirements;
using MyDomainPasswordChange.Api.Models;

namespace MyDomainPasswordChange.Api.Authorization.Handlers;

public class LocalizedAdminRequirementHandler(IHttpContextAccessor httpContextAccessor,
                                              IOptions<AuthenticationConfiguration> authConfiguration) : AuthorizationHandler<LocalizedAdminRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly AuthenticationConfiguration _authConfiguration = authConfiguration.Value;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalizedAdminRequirement requirement)
    {
        if (!context.User.Claims.Any(c => c.Type == "primaryGroup" && c.Value == "Domain Admins"))
        {
            context.Fail(new AuthorizationFailureReason(this, "The user must be a Domain Admin"));
            return Task.CompletedTask;
        }

        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null || httpContext.Request.RouteValues.TryGetValue("dependencyId", out var dependencyId) is false || dependencyId is null)
        {
            return Task.CompletedTask;
        }

        var groups = context.User.Claims.FirstOrDefault(c => c.Type == "groups")?.Value.Split(',') ?? [];
        if (groups.Contains(_authConfiguration.GlobalAdministratorsGroup))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!groups.Contains(dependencyId.ToString()))
        {
            context.Fail(new AuthorizationFailureReason(this, "The admin must be of the same group of the dependecy is calling."));
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
