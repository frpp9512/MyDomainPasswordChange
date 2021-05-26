using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Interfaces
{
    public interface IIpAddressBlacklist
    {
        Task<List<BlacklistedIpAddress>> GetBlacklistedIpAddressesAsync();
        Task AddIpAddressToBlacklistAsync(string ipAddress, string reason);
        Task RemoveIpAddressFromBlacklistAsync(string ipAddress);
        Task<bool> IsIpAddressBlacklistedAsync(string ipAddress);
    }
}