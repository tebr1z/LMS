using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LMS.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LmsDbContext>
{
    public LmsDbContext CreateDbContext(string[] args)
    {
        // For design-time operations (migrations), we use a SQL Server connection string
        // This will be overridden by the actual connection string from appsettings.json at runtime
        // Default SQL Server connection string for local development
        var connectionString = "Server=TABRIZ\\SQLEXPRESS;Database=LmsDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<LmsDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            })
        .UseLazyLoadingProxies();

        return new LmsDbContext(optionsBuilder.Options);
    }
}

