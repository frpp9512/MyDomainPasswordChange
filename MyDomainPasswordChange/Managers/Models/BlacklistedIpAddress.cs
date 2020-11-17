using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class BlacklistedIpAddress
    {
        public DateTime AddedInBlacklist { get; set; }
        public string IpAddress { get; set; }
        public string Reason { get; set; }
    }
}
