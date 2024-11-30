using System.DirectoryServices.ActiveDirectory;

namespace MyDomainPasswordChange.Api.Authorization.Helpers;

public static class AuthorizationConstants
{
    public static class Policies
    {
        public const string DOMAIN_ADMINS_POLICY = "Domain Admins";
        public const string LOCALIZED_DOMAIN_ADMINS_POLICY = "Localized Domain Admins";
    }

    public static class Claims
    {
        public const string ACCOUNT_NAME = "accountName";
        public const string PRIMARY_GROUP = "primaryGroup";
    }
}
