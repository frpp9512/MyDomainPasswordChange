using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MyDomainPasswordChange.Data.Sqlite
{
    class SqliteDataContextFactory : IDesignTimeDbContextFactory<SqliteDataContext>
    {
        private readonly string _connectionStringFilePath = AppContext.BaseDirectory + "db.design.connectionstring.sqlite";

        public SqliteDataContext CreateDbContext(string[] args)
        {
            if (!File.Exists(_connectionStringFilePath))
            {
                File.WriteAllText(_connectionStringFilePath, "Data Source=data.db;Mode=ReadWriteCreate");
            }
            return new(File.ReadLines(_connectionStringFilePath).FirstOrDefault());
        }
    }
}
