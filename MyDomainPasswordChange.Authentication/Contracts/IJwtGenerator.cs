namespace MyDomainPasswordChange.Authentication.Contracts;

public interface IJwtGenerator
{
    public string GenerateJwt(string accountName, string password);
}
