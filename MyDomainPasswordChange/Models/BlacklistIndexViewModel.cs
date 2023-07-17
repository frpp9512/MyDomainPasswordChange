using System.Collections.Generic;

namespace MyDomainPasswordChange.Models;

public class BlacklistIndexViewModel
{
    public IEnumerable<BlacklistedIpViewModel> BlacklistedIpAddresses { get; set; }
}
