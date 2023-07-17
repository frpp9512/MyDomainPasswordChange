using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Interfaces;

/// <summary>
/// Represents a manager of the user's passwords.
/// </summary>
public interface IPasswordHistoryManager
{
    /// <summary>
    /// Determines if the user with the specified account name has used the specified password in the last determined changes.
    /// </summary>
    /// <param name="accountName">The name of the user's account.</param>
    /// <param name="password">The password to check if has been used.</param>
    /// <param name="passwordHistoryCount">The amount of used passwords to check.</param>
    /// <returns><see langword="true"/> if the given password has been used by the user.</returns>
    Task<bool> CheckPasswordHistoryAsync(string accountName, string password, int passwordHistoryCount);

    /// <summary>
    /// Register a password used by the user with the specified account name.
    /// </summary>
    /// <param name="accountName">The account name of the user who used the password.</param>
    /// <param name="password">The password used by the user.</param>
    /// <returns></returns>
    Task RegisterPasswordAsync(string accountName, string password);

    /// <summary>
    /// Determines if the user with the specified account name has any password registered.
    /// </summary>
    /// <param name="accountName">The name of the user's account.</param>
    /// <returns><see langword="true"/> if the user has passwords registered.</returns>
    Task<bool> AccountHasEntries(string accountName);
}