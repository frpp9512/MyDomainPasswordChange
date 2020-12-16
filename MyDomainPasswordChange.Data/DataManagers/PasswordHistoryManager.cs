using Microsoft.EntityFrameworkCore;
using MyDomainPasswordChange.Data.Contexts;
using MyDomainPasswordChange.Data.Interfaces;
using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.DataManagers
{
    /// <summary>
    /// Manages the password's history of the users.
    /// </summary>
    public class PasswordHistoryManager : IPasswordHistoryManager
    {
        private readonly DataContext _dataContext;

        public PasswordHistoryManager(DataContext dataContext)
        {
            _dataContext = dataContext;
            _dataContext.Database.EnsureCreated();
        }

        public async Task RegisterPasswordAsync(string accountName, string password)
        {
            var entry = new PasswordHistoryEntry
            {
                AccountName = accountName,
                Updated = DateTime.Now,
                Password = YpSecurity.SecurityUtil.B64HashEncrypt(accountName, password)
            };
            _dataContext.HistoryEntries.Add(entry);
            await _dataContext.SaveChangesAsync();
        }

        public async Task<bool> CheckPasswordHistoryAsync(string accountName, string password, int passwordHistoryCount)
        {
            var history = await LoadPasswordHistoryForUserAsync(accountName, passwordHistoryCount);
            if (history is not null && history.Any())
            {
                foreach (var entry in history)
                {
                    if (YpSecurity.AuthUtil.TryAuth(accountName, ref password, YpSecurity.SecurityUtil.SecureString(entry.Password), false))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<IEnumerable<PasswordHistoryEntry>> LoadPasswordHistoryForUserAsync(string accountName, int passwordHistoryCount)
        {
            if (_dataContext.HistoryEntries.Any())
            {
                var results = await _dataContext.HistoryEntries
                                            .Where(e => e.AccountName == accountName)
                                            .OrderByDescending(e => e.Updated)
                                            .Take(passwordHistoryCount)
                                            .ToListAsync();

                return results;
            }
            return null;
        }

        public async Task<bool> AccountHasEntries(string accountName) 
            => await _dataContext.HistoryEntries.Where(e => e.AccountName == accountName).AnyAsync();
    }
}