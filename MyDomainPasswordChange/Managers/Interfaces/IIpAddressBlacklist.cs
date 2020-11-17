using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public interface IIpAddressBlacklist
    {
        public List<BlacklistedIpAddress> GetBlacklistedIpAddresses();
        public void AddIpAddressToBlacklist(string ipAddress, string reason);
        public void RemoveIpAddressFromBlacklist(string ipAddress);
        bool IsIpAddressBlacklisted(string ipAddress);
    }
}
