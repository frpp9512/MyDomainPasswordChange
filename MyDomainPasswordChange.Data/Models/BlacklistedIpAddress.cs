using System;
using System.ComponentModel.DataAnnotations;

namespace MyDomainPasswordChange.Data.Models;

public record BlacklistedIpAddress
{
    [Key]
    public Guid Id { get; set; }
    public DateTimeOffset AddedInBlacklist { get; set; } = DateTime.Now;
    public string IpAddress { get; set; }
    public string Reason { get; set; }
}
