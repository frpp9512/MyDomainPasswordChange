﻿using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Management.Interfaces;

namespace PasswordExpirationCheckService.Services;

public class BindCredentialsProvider : IBindCredentialsProvider
{
    private readonly IConfiguration _configuration;

    public BindCredentialsProvider(IConfiguration configuration) => _configuration = configuration;

    public string GetBindPassword() => _configuration["LdapBindPassword"];
    public string GetBindUsername() => _configuration["LdapBindUsername"];
    public string GetLdapSearchBase() => _configuration["LdapSearchBase"];
    public string GetLdapServer() => _configuration["LdapServer"];
}
