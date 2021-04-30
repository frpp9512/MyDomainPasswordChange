using System.Security;

namespace YpSecurity
{
    public class AuthInfo
    {
        public AuthInfo(string host, string db_name, string user, string password)
        {
            this.Host = host;
            this.DB_Name = db_name;
            this.User = user;
            this.Password = SecurityUtil.SecureString(password);
            SecurityUtil.ReleaseFromMemory(ref password);
        }

        public string Host { get; }

        public string DB_Name { get; }

        public string User { get; }

        public SecureString Password { get; }
    }
}
