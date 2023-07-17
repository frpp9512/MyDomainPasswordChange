using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Interfaces;

public interface IIpAddressBlacklist
{
    Task<IEnumerable<BlacklistedIpAddress>> GetIpAddressesAsync();
    Task AddIpAddressAsync(string ipAddress, string reason);
    Task AddIpAddressAsync(BlacklistedIpAddress blacklistedIp);
    Task RemoveIpAddressAsync(string ipAddress);
    Task<bool> IsBlacklistedAsync(string ipAddress);
    Task<BlacklistedIpAddress> GetBlacklistedIpAddressAsync(Guid id);
    Task<bool> ExistsBlacklistedAddressAsync(Guid id);
    Task RemoveBlacklistedAddressAsync(BlacklistedIpAddress blacklistedIp);
}