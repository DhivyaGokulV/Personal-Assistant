using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PersonalAssistant.Infrastructure.Persistence;

namespace PersonalAssistant.Migrations.SqlServer;

public class AppDbContextSqlServerFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PA_SQLSERVER_CS")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=PersonalAssistant;Trusted_Connection=True;Encrypt=False;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, x => x.MigrationsAssembly(typeof(AppDbContextSqlServerFactory).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }
}
