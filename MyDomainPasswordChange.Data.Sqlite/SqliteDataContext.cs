using Microsoft.EntityFrameworkCore;
using MyDomainPasswordChange.Data.Contexts;

namespace MyDomainPasswordChange.Data.Sqlite;

public class SqliteDataContext : DataContext
{
    private readonly string _connectionString;

    public SqliteDataContext(string connectionString) => _connectionString = connectionString;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder.UseSqlite(_connectionString);
        base.OnConfiguring(optionsBuilder);
    }
}
