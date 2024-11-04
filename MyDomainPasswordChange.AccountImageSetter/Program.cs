using Microsoft.Extensions.Options;
using MyDomainPasswordChange.Management.Managers;
using MyDomainPasswordChange.Management.Models;

string userName = Environment.UserName;
string imagePath = @$"C:\Users\{userName}\AppData\Roaming\Microsoft\Windows\AccountPictures\{userName}.png";

System.Drawing.Image image = await new MyDomainPasswordManagement(Options.Create(new LdapConnectionConfiguration
{
    LdapServer = "dc.ingeco.cu",
    LdapSearchBase = "OU=INGECO,dc=ingeco,dc=cu",
    LdapBindUsername = "administrator",
    LdapBindPassword = "Asd123/*-",
})).GetUserImage(userName);

image.Save(imagePath);