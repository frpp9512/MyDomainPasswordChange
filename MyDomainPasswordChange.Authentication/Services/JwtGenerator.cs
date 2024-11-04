using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyDomainPasswordChange.Authentication.Configuration;
using MyDomainPasswordChange.Authentication.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyDomainPasswordChange.Authentication.Services;

public class JwtGenerator(IOptions<JwtGeneratorOptions> options, LdapHelper ldapHelper) : IJwtGenerator
{
    private readonly JwtGeneratorOptions _options = options.Value;
    private readonly LdapHelper _ldapHelper = ldapHelper;

    public string GenerateJwt(string accountName, string password)
    {
        List<Claim> claims = [new Claim("accountName", accountName), .. _options.ExtraClaims.Select(ec => new Claim(ec.Key, ec.Value))];
        var userPrimaryGroup = _ldapHelper.GetPrimaryGroup(accountName, password);
        if (userPrimaryGroup is not null)
        {
            claims.Add(new Claim("primaryGroup", userPrimaryGroup));
        }

        IEnumerable<string?> userGroups = _ldapHelper.GetUserGroups(accountName, password).Except([userPrimaryGroup]);
        if (userGroups.Any())
        {
            claims = [.. claims, new Claim("groups", string.Join(",", userGroups))];
        }

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_options.Secret));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(_options.Expires),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
