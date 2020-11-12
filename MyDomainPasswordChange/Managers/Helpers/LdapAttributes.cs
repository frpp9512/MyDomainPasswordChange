using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public static class LdapAttributes
    {
        public static string GUID = "objectGUID";
        public static string COMPANY = "company";
        public static string DEPARTMENT = "department";
        public static string DESCRIPTION = "description";
        public static string DISPLAYNAME = "displayName";
        public static string GIVEN_NAME = "givenName";
        public static string EMAIL = "mail";
        public static string NAME = "name";
        public static string ACCOUNT_NAME = "sAMAccountName";
        public static string SURNAME = "sn";
        public static string TITLE = "title";
        public static string THUMBNAIL_LOGO = "thumbnailLogo";
        public static string THUMBNAIL_PHOTO = "thumbnailPhoto";
        public static string PRINCIPAL_NAME = "userPrincipalName";
        public static string STREET = "street";
        public static string JPEG_PHOTO = "jpegPhoto";
    }
}
