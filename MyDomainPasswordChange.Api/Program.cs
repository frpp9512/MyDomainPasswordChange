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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IDomainPasswordManagement, MyDomainPasswordManagement>();
builder.Services.AddTransient<IMailSettingsProvider, MailSettingsProvider>();
builder.Services.AddSingleton<IMyMailService, MyMailService>();
builder.Services.AddTransient<IMailNotificator, MailNotificator>();

var connectionString = builder.Configuration.GetConnectionString("Sqlite");
var dataContext = new SqliteDataContext(connectionString);
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/hello", () => Results.Ok("Hello World!"));

app.MapPasswordEndpoints();

app.MapAccountEndpoints();

app.MapGlobalEndpoints();

app.MapHealthChecks("/health");

app.Run();
