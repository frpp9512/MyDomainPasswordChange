using System;

namespace MyDomainPasswordChange
{
    public class UserViewModel
    {
        public string AccountName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Email { get; set; }
        public DateTime LastPasswordSet { get; set; }
        public int PasswordExpirationDays { get; set; }
    }
}