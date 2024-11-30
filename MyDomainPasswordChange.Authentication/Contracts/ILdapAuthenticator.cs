namespace MyDomainPasswordChange.Authentication.Contracts;

public interface ILdapAuthenticator
{
    bool Authenticate(string accountName, string password);

    Task<bool> AuthenticateAsync(string accountName, string password);
}
