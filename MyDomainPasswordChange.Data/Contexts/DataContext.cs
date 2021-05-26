using Microsoft.EntityFrameworkCore;
using MyDomainPasswordChange.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Contexts
{
    /// <summary>
    /// The Data Context definition with all the entities managed in the application.
    /// </summary>
    public abstract class DataContext : DbContext
    {
        /// <summary>
        /// The history of passwords used by the users.
        /// </summary>
        public DbSet<PasswordHistoryEntry> HistoryEntries { get; set; }

        /// <summary>
        /// The IP address blocked for attempting offense the service.
        /// </summary>
        public DbSet<BlacklistedIpAddress> BlacklistedIps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordHistoryEntry>().HasKey(p => p.Id);
 
            modelBuilder.Entity<BlacklistedIpAddress>().HasKey(b => b.Id);
            
            base.OnModelCreating(modelBuilder);
        }
    }
}