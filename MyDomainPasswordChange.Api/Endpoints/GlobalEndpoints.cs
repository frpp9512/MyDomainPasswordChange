using AutoMapper;
using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Shared.DTO;

namespace MyDomainPasswordChange.Api.Endpoints;

public static class GlobalEndpoints
{
    public static WebApplication MapGlobalEndpoints(this WebApplication app)
    {
        var global = app.MapGroup("/global");

        global.MapGet("dependencies", GetDependencies);

        return app;
    }

    private static IResult GetDependencies(bool? includeGlobal,
                                           IOptions<DependenciesConfiguration> dependenciesConfigOptions,
                                           IMapper mapper,
                                           ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("GetDependencies");
        var dependenciesConfig = dependenciesConfigOptions.Value;
        var dtos = dependenciesConfig.Definitions.Where(dep => (dep.Type != "global") || (dep.Type == "global" && includeGlobal is true)).Select(dep => mapper.Map<DependencyDto>(dep)).ToList();
        return Results.Ok(dtos);
    }
}
