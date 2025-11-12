using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Extensions;

public static class DatabaseExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<LmsDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Checking database connection...");
            
            // Retry logic for database connection
            var maxRetries = 10;
            var delay = TimeSpan.FromSeconds(5);
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (await context.Database.CanConnectAsync())
                    {
                        logger.LogInformation("Database connection successful!");
                        
                        // Check if database needs migration
                        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                        if (pendingMigrations.Any())
                        {
                            logger.LogInformation("Applying database migrations...");
                            await context.Database.MigrateAsync();
                            logger.LogInformation("Database migrations applied successfully!");
                        }
                        else
                        {
                            logger.LogInformation("Database is up to date.");
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Database connection attempt {Attempt} failed. Retrying in {Delay} seconds...", i + 1, delay.TotalSeconds);
                    
                    if (i == maxRetries - 1)
                    {
                        logger.LogError(ex, "Failed to connect to database after {MaxRetries} attempts.", maxRetries);
                        throw;
                    }
                    
                    await Task.Delay(delay);
                }
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}

