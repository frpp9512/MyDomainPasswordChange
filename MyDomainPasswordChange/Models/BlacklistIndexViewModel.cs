using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Models
{
    public class BlacklistIndexViewModel
    {
        public IEnumerable<BlacklistedIpViewModel> BlacklistedIpAddresses { get; set; }
    }
}
