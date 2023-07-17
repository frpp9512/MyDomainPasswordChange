using Microsoft.EntityFrameworkCore;
using MyDomainPasswordChange.Data.Contexts;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.DataManagers;

public class IpAddressBlacklist : IIpAddressBlacklist
{
    private readonly DataContext _dataContext;

    public IpAddressBlacklist(DataContext dataContext) => _dataContext = dataContext;

    public async Task AddIpAddressAsync(string ipAddress, string reason)
    {
        if (await IsBlacklistedAsync(ipAddress))
        {
            var blacklisted = await _dataContext.BlacklistedIps.FirstOrDefaultAsync(b => b.IpAddress == ipAddress);
            blacklisted.AddedInBlacklist = DateTime.Now;
            blacklisted.Reason = reason;
            _ = _dataContext.Update(blacklisted);
        }
        else
        {
            _ = await _dataContext.BlacklistedIps.AddAsync(new BlacklistedIpAddress
            {
                AddedInBlacklist = DateTime.Now,
                IpAddress = ipAddress,
                Reason = reason
            });
        }

        _ = await _dataContext.SaveChangesAsync();
    }

    public async Task AddIpAddressAsync(BlacklistedIpAddress blacklistedIp)
    {
        if (await IsBlacklistedAsync(blacklistedIp.IpAddress))
        {
            var blacklisted = await _dataContext.BlacklistedIps.FirstOrDefaultAsync(b => b.IpAddress == blacklistedIp.IpAddress);
            blacklisted.AddedInBlacklist = DateTime.Now;
            blacklisted.Reason = blacklistedIp.Reason;
            _ = _dataContext.Update(blacklisted);
        }
        else
        {
            _ = await _dataContext.BlacklistedIps.AddAsync(blacklistedIp);
        }

        _ = await _dataContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<BlacklistedIpAddress>> GetIpAddressesAsync()
        => await _dataContext.BlacklistedIps.ToListAsync();

    public async Task<bool> IsBlacklistedAsync(string ipAddress)
        => await _dataContext.BlacklistedIps.AnyAsync(b => b.IpAddress == ipAddress) == true;

    public async Task RemoveIpAddressAsync(string ipAddress)
    {
        if (await IsBlacklistedAsync(ipAddress))
        {
            _ = _dataContext.BlacklistedIps.Remove(_dataContext.BlacklistedIps.First(b => b.IpAddress == ipAddress));
        }

        _ = await _dataContext.SaveChangesAsync();
    }

    public async Task<BlacklistedIpAddress> GetBlacklistedIpAddressAsync(Guid id)
    {
        var address = await _dataContext.BlacklistedIps.FirstOrDefaultAsync(b => b.Id == id);
        return address;
    }

    public async Task<bool> ExistsBlacklistedAddressAsync(Guid id)
        => await _dataContext.BlacklistedIps.AnyAsync(b => b.Id == id);

    public async Task RemoveBlacklistedAddressAsync(BlacklistedIpAddress blacklistedIp)
    {
        _ = _dataContext.BlacklistedIps.Remove(blacklistedIp);
        _ = await _dataContext.SaveChangesAsync();
    }
}