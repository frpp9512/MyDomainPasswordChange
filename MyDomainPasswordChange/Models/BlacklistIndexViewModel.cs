using System.Collections.Generic;

namespace MyDomainPasswordChange.Models;

public record BlacklistIndexViewModel
{
    public IEnumerable<BlacklistedIpViewModel> BlacklistedIpAddresses { get; set; }
}
