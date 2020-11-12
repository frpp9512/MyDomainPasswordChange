namespace MyDomainPasswordChange
{
    public class UserViewModel
    {
        public string AccountName { get; set; }
        public string DisplayName { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public int PasswordExpirationDays { get; set; }
    }
}