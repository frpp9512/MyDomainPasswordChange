namespace MyDomainPasswordChange.Management.Helpers;

/// <summary>
/// A set of LDAP attributes names.
/// </summary>
public static class LdapAttributesConstants
{
    public static readonly string GUID = "objectGUID";
    public static readonly string COMPANY = "company";
    public static readonly string DEPARTMENT = "department";
    public static readonly string DESCRIPTION = "description";
    public static readonly string DISPLAYNAME = "displayName";
    public static readonly string GIVEN_NAME = "givenName";
    public static readonly string EMAIL = "mail";
    public static readonly string NAME = "name";
    public static readonly string ACCOUNT_NAME = "sAMAccountName";
    public static readonly string SURNAME = "sn";
    public static readonly string TITLE = "title";
    public static readonly string THUMBNAIL_LOGO = "thumbnailLogo";
    public static readonly string THUMBNAIL_PHOTO = "thumbnailPhoto";
    public static readonly string PRINCIPAL_NAME = "userPrincipalName";
    public static readonly string STREET = "street";
    public static readonly string JPEG_PHOTO = "jpegPhoto";
    public static readonly string PASSWORD_LAST_SET = "pwdLastSet";
    public static readonly string OBJECT_CLASS = "objectClass";
    public static readonly string PO_BOX = "postOfficeBox";
    public static readonly string ADDRESS = "streetAddress";
    public static readonly string GID_NUMBER = "gidNumber";
    public static readonly string OFFICE = "physicalDeliveryOfficeName";
}
