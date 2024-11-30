using System;
using System.ComponentModel.DataAnnotations;

namespace MyDomainPasswordChange.Data.Models;

/// <summary>
/// Represents a password used by an user.
/// </summary>
public record PasswordHistoryEntry
{
    /// <summary>
    /// The unique identifier of the entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// When the password was registered.
    /// </summary>
    public DateTime Updated { get; set; }

    /// <summary>
    /// The name of the user's account.
    /// </summary>
    public string AccountName { get; set; }

    /// <summary>
    /// The password used by the user.
    /// 
    /// REMEMBER! This must always be stored encrypted.
    /// </summary>
    public string Password { get; set; }
}
