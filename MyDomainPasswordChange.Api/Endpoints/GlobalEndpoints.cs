using AutoMapper;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Shared.DTO;

namespace MyDomainPasswordChange.Api.Endpoints;

public static class GlobalEndpoints
{
    public static WebApplication MapGlobalEndpoints(this WebApplication app)
    {
        RouteGroupBuilder global = app.MapGroup("/global");

        _ = global.MapGet("dependencies", GetDependencies);

        return app;
    }

    private static IResult GetDependencies(bool? includeGlobal,
                                           IOptions<DependenciesConfiguration> dependenciesConfigOptions,
                                           IMapper mapper,
                                           ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("GetDependencies");
        DependenciesConfiguration dependenciesConfig = dependenciesConfigOptions.Value;
        var dtos = dependenciesConfig.Definitions.Where(dep => (dep.Type != "global") || (dep.Type == "global" && includeGlobal is true)).Select(mapper.Map<DependencyDto>).ToList();
        return Results.Ok(dtos);
    }
}
