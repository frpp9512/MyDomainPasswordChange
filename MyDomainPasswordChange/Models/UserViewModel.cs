using System;

namespace MyDomainPasswordChange.Models;

public class UserViewModel
{
    public string AccountName { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Email { get; set; }
    public DateTime LastPasswordSet { get; set; }
    public bool PendingToSetPassword => new DateTime(1, 1, 1, 0, 0, 0) == LastPasswordSet;
    public int PasswordExpirationDays { get; set; }
    public bool Enabled { get; set; }
    public bool PasswordNeverExpires { get; set; }
}