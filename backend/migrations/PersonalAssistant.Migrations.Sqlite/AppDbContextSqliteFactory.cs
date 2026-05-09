using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Migrations.Sqlite;

public class AppDbContextSqliteFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PA_SQLITE_CS")
            ?? "Data Source=personal-assistant.dev.db";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connectionString, x => x.MigrationsAssembly(typeof(AppDbContextSqliteFactory).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }
}
