namespace MyDomainPasswordChange.Management.Models;

/// <summary>
/// Represents a email send data.
/// </summary>
public class MailRequest
{
    /// <summary>
    /// The email address of the recipient.
    /// </summary>
    public string MailTo { get; set; }

    /// <summary>
    /// The email address of the carbon copy recipient.
    /// </summary>
    public string Cc { get; set; }

    /// <summary>
    /// The subject of the email.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Defines if the email should be marked as !important
    /// </summary>
    public bool Important { get; set; }

    /// <summary>
    /// The HTML body content of the mail.
    /// </summary>
    public string Body { get; set; }
}
