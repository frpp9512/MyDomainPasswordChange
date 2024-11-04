using Microsoft.AspNetCore.Mvc;
using MyDomainPasswordChange.Authentication.Configuration;
using MyDomainPasswordChange.Authentication.Contracts;
using MyDomainPasswordChange.Authentication.Services;
using Novell.Directory.Ldap;
using System.ComponentModel.DataAnnotations;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<LdapAuthenticationOptions>(builder.Configuration.GetSection("LdapAuthentication"));
builder.Services.Configure<JwtGeneratorOptions>(builder.Configuration.GetSection("JwtGenerator"));
builder.Services.AddTransient<LdapHelper>();
builder.Services.AddTransient<IJwtGenerator, JwtGenerator>();
builder.Services.AddTransient<ILdapAuthenticator, LdapAuthenticator>();
builder.Services.AddHttpContextAccessor();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/login", (string username,
                       [DataType(DataType.Password)] string password,
                       [FromServices] ILdapAuthenticator authenticator,
                       [FromServices] IJwtGenerator jwtGenerator,
                       [FromServices] ILoggerFactory loggerFactory,
                       [FromServices] IHttpContextAccessor httpContextAccessor) =>
{
    ILogger logger = loggerFactory.CreateLogger("LoginEndpoint");
    HttpContext? context = httpContextAccessor.HttpContext;
    logger.LogInformation(
        "[{traceId}] Login requested to account {accountName} with password {password} from source {sourceIp}",
        context?.TraceIdentifier,
        username,
        !string.IsNullOrEmpty(password),
        context?.Connection.RemoteIpAddress);

    try
    {
        if (!authenticator.Authenticate(username, password))
        {
            logger.LogWarning("[{traceId}] Login failed for account {accountName}", context?.TraceIdentifier, username);
            return Results.Unauthorized();
        }

        logger.LogInformation("[{traceId}] Login successful for account {accountName}. Generating JWT.", context?.TraceIdentifier, username);
        var token = jwtGenerator.GenerateJwt(username, password);

        logger.LogInformation("[{traceId}] JWT generated for account {accountName}.", context?.TraceIdentifier, username);
        return Results.Ok(new { token });
    }
    catch (Exception ex)
    {
        logger.LogError("[{traceId}] An error occurred when trying to login account {accountName}. Error message: {errorMessage}", context?.TraceIdentifier, username, ex.Message);
        return Results.Problem(ex.Message);
    }
})
    .WithOpenApi()
    .WithName("Login")
    .WithDescription("Authenticates an account of the configured ldap server");

app.Run();
