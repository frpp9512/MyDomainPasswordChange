using Microsoft.EntityFrameworkCore;
using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Contexts
{
    public abstract class DataContext : DbContext
    {
        public DbSet<PasswordHistoryEntry> HistoryEntries;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordHistoryEntry>().HasKey(p => p.Id);

            base.OnModelCreating(modelBuilder);
        }
    }
}
