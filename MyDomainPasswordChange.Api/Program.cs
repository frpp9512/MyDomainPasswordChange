using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyDomainPasswordChange.Api.Authorization.Handlers;
using MyDomainPasswordChange.Api.Authorization.Helpers;
using MyDomainPasswordChange.Api.Authorization.Requirements;
using MyDomainPasswordChange.Api.Endpoints;
using MyDomainPasswordChange.Api.Endpoints.HealthChecks;
using MyDomainPasswordChange.Api.Models;
using MyDomainPasswordChange.Data.Contexts;
using MyDomainPasswordChange.Data.DataManagers;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Data.Sqlite;
using MyDomainPasswordChange.Management.Interfaces;
using MyDomainPasswordChange.Management.Managers;
using MyDomainPasswordChange.Management.Models;
using MyDomainPasswordChange.Managers.Services;
using System.Configuration;
using System.Text;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AuthenticationConfiguration>(builder.Configuration.GetSection("AuthenticationConfiguration"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, LocalizedAdminRequirementHandler>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, config =>
    {
        AuthenticationConfiguration authConfig = builder.Configuration.GetSection("AuthenticationConfiguration").Get<AuthenticationConfiguration>() ?? throw new ConfigurationErrorsException("Missing the authentication configuration.");
        config.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authConfig.ValidIssuer,
            ValidAudience = authConfig.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfig.Secret))
        };
    });

builder.Services.AddAuthorization(config =>
{
    AuthorizationPolicyBuilder defaultPolicyBuilder = new();
    _ = defaultPolicyBuilder.RequireAuthenticatedUser();
    config.FallbackPolicy = defaultPolicyBuilder.Build();

    AuthorizationPolicyBuilder domainAdminAccessPolicyBuilder = new();
    _ = domainAdminAccessPolicyBuilder.RequireClaim(AuthorizationConstants.Claims.ACCOUNT_NAME);
    _ = domainAdminAccessPolicyBuilder.RequireAssertion(
        context => context.User.Claims.Any(c => c.Type == AuthorizationConstants.Claims.PRIMARY_GROUP && c.Value == "Domain Admins"));

    config.AddPolicy(
        AuthorizationConstants.Policies.DOMAIN_ADMINS_POLICY,
        domainAdminAccessPolicyBuilder.Build());

    config.AddPolicy(
        AuthorizationConstants.Policies.LOCALIZED_DOMAIN_ADMINS_POLICY,
        policy => policy.Requirements.Add(new LocalizedAdminRequirement()));
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
    config.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below. \r\n\r\nExample: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = JwtBearerDefaults.AuthenticationScheme
        });

    config.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                },
                Scheme = "oauth2",
                Name = JwtBearerDefaults.AuthenticationScheme,
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IDomainPasswordManagement, MyDomainPasswordManagement>();
builder.Services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
builder.Services.AddSingleton<IMyMailService, MyMailService>();
builder.Services.AddTransient<IMailNotifier, MailNotifier>();

var connectionString = builder.Configuration.GetConnectionString("Sqlite");
SqliteDataContext dataContext = new(connectionString);
builder.Services.AddSingleton<DataContext>(dataContext);
builder.Services.AddScoped<IPasswordHistoryManager, PasswordHistoryManager>();

builder.Services.AddSingleton<IIpAddressBlacklist, IpAddressBlacklist>();
builder.Services.Configure<PasswordHistoryConfiguration>(builder.Configuration.GetSection("PasswordHistoryConfiguration"));
builder.Services.Configure<AdminInfoConfiguration>(builder.Configuration.GetSection("AdminInfoConfiguration"));
builder.Services.Configure<LdapConnectionConfiguration>(builder.Configuration.GetSection("LdapConnectionConfiguration"));
builder.Services.Configure<DependenciesConfiguration>(builder.Configuration.GetSection("DependenciesConfiguration"));
builder.Services.Configure<DefaultAccountConfiguration>(builder.Configuration.GetSection("DefaultAccountConfiguration"));

builder.Services.AddLogging();
builder.Services.AddHealthChecks()
    .AddCheck<LdapConnectionCheck>("ldap_connection")
    .AddDbContextCheck<DataContext>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapPasswordEndpoints();

app.MapAccountEndpoints();

app.MapGlobalEndpoints();

app.MapHealthChecks("/health");

app.Run();
