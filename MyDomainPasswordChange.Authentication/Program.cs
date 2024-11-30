using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using MyDomainPasswordChange.Authentication;
using MyDomainPasswordChange.Authentication.Configuration;
using MyDomainPasswordChange.Authentication.Contracts;
using MyDomainPasswordChange.Authentication.Services;
using Novell.Directory.Ldap;

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MapPost("/login", (AuthenticationRequest request,
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
        request.AccountName,
        !string.IsNullOrEmpty(request.Password),
        context?.Connection.RemoteIpAddress);

    try
    {
        if (!authenticator.Authenticate(request.AccountName, request.Password))
        {
            logger.LogWarning("[{traceId}] Login failed for account {accountName}", context?.TraceIdentifier, request.AccountName);
            return Results.Unauthorized();
        }

        logger.LogInformation("[{traceId}] Login successful for account {accountName}. Generating JWT.", context?.TraceIdentifier, request.AccountName);
        var token = jwtGenerator.GenerateJwt(request.AccountName, request.Password);

        logger.LogInformation("[{traceId}] JWT generated for account {accountName}.", context?.TraceIdentifier, request.AccountName);
        return Results.Ok(new { token });
    }
    catch (Exception ex)
    {
        logger.LogError("[{traceId}] An error occurred when trying to login account {accountName}. Error message: {errorMessage}", context?.TraceIdentifier, request.AccountName, ex.Message);
        return Results.Problem(ex.Message);
    }
})
    .WithOpenApi()
    .WithName("Login")
    .WithDisplayName("Login account")
    .WithDescription("Login an account granting an access token.");

app.MapPost("/auth", (AuthenticationRequest request,
                      [FromServices] ILdapAuthenticator authenticator,
                      [FromServices] ILoggerFactory loggerFactory,
                      [FromServices] IHttpContextAccessor httpContextAccessor) =>
{
    ILogger logger = loggerFactory.CreateLogger("AuthEndpoint");
    HttpContext? context = httpContextAccessor.HttpContext;
    logger.LogInformation(
        "[{traceId}] Authentication requested to account {accountName} with password {password} from source {sourceIp}",
        context?.TraceIdentifier,
        request.AccountName,
        !string.IsNullOrEmpty(request.Password),
        context?.Connection.RemoteIpAddress);

    try
    {
        if (!authenticator.Authenticate(request.AccountName, request.Password))
        {
            logger.LogWarning("[{traceId}] Authentication failed for account {accountName}", context?.TraceIdentifier, request.AccountName);
            return Results.Unauthorized();
        }

        logger.LogInformation("[{traceId}] Authentication successful for account {accountName}.", context?.TraceIdentifier, request.AccountName);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError("[{traceId}] An error occurred when trying to authenticate account {accountName}. Error message: {errorMessage}", context?.TraceIdentifier, request.AccountName, ex.Message);
        return Results.Problem(ex.Message);
    }
})
    .WithOpenApi()
    .WithName("Auth")
    .WithDisplayName("Authenticate account")
    .WithDescription("Authenticates an account to validate it credentials.");

app.Run();
