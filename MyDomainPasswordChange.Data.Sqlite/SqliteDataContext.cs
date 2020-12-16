using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyDomainPasswordChange.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDomainPasswordChange.Data.Sqlite
{
    public class SqliteDataContext : DataContext
    {
        private readonly string _connectionString;

        public SqliteDataContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
            base.OnConfiguring(optionsBuilder);
        }
    }
}
