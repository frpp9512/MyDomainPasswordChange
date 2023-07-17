namespace MyDomainPasswordChange.Management.Interfaces;

/// <summary>
/// Defines a class with give access to the ldap bind credentials data.
/// </summary>
public interface IBindCredentialsProvider
{
    /// <summary>
    /// Provides the name of the ldap server to bind to.
    /// </summary>
    /// <returns>The name of the ldap server.</returns>
    string GetLdapServer();

    /// <summary>
    /// Provides the search base of objects in the ldap server.
    /// </summary>
    /// <returns>The object's search base in the ldap server (Ex. "ou=Enterprise,dc=mycompany,dc=com").</returns>
    string GetLdapSearchBase();

    /// <summary>
    /// Provides the username (account name) of the credentials to bind to the server.
    /// </summary>
    /// <returns>The username of the bind's credentials.</returns>
    string GetBindUsername();

    /// <summary>
    /// Provides the password of the credentials to bind to the server.
    /// </summary>
    /// <returns>The password of the bind's credentials.</returns>
    string GetBindPassword();
}
