using Microsoft.EntityFrameworkCore;
using MyDomainPasswordChange.Data.Contexts;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public class IpAddressBlacklist : IIpAddressBlacklist
    {
        private readonly DataContext _dataContext;

        public IpAddressBlacklist(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task AddIpAddressToBlacklistAsync(string ipAddress, string reason)
        {
            if (await IsIpAddressBlacklistedAsync(ipAddress))
            {
                var blacklisted = await _dataContext.BlacklistedIps.FirstOrDefaultAsync(b => b.IpAddress == ipAddress);
                blacklisted.AddedInBlacklist = DateTime.Now;
                blacklisted.Reason = reason;
                _dataContext.Update(blacklisted);
            }
            else
            {
                await _dataContext.BlacklistedIps.AddAsync(new BlacklistedIpAddress
                {
                    AddedInBlacklist = DateTime.Now,
                    IpAddress = ipAddress,
                    Reason = reason
                });
            }
            await _dataContext.SaveChangesAsync();
        }

        public async Task<List<BlacklistedIpAddress>> GetBlacklistedIpAddressesAsync() 
            => await _dataContext.BlacklistedIps.ToListAsync();

        public async Task<bool> IsIpAddressBlacklistedAsync(string ipAddress) 
            => await _dataContext.BlacklistedIps.AnyAsync(b => b.IpAddress == ipAddress) == true;

        public async Task RemoveIpAddressFromBlacklistAsync(string ipAddress)
        {
            if (await IsIpAddressBlacklistedAsync(ipAddress))
            {
                _dataContext.BlacklistedIps.Remove(_dataContext.BlacklistedIps.First(b => b.IpAddress == ipAddress));
            }
            await _dataContext.SaveChangesAsync();
        }
    }
}