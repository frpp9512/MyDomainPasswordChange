using System;
using System.Collections.Generic;

namespace MyDomainPasswordChange.Models;

public class UserViewModel
{
    public string AccountName { get; set; }

    public string DisplayName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Description { get; set; }

    public string Email { get; set; }

    public string PersonalId { get; set; }

    public string JobTitle { get; set; }

    public string Office { get; set; }

    public string Address { get; set; }

    public DateTime LastPasswordSet { get; set; }

    public bool PasswordNeverExpires { get; set; }

    public List<string> AllowedWorkstations { get; set; } = new List<string> { "NONE" };

    public string MailboxCapacity { get; set; }

    public bool Enabled { get; init; }

    public int PasswordExpirationDays { get; set; }

    public bool PendingToSetPassword => new DateTime(1, 1, 1, 0, 0, 0) == LastPasswordSet;

    public InternetAccess InternetAccess { get; set; } = InternetAccess.None;
}

public enum InternetAccess
{
    None,
    National,
    Restricted,
    Full
}