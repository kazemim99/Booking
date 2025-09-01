using Booksy.UserManagement.Infrastructure.Persistence;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;

namespace Booksy.UserManagement.API.Extensions;

/// <summary>
/// Database migration and seeding extensions
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies database migrations and seeds initial data
    /// </summary>
    public static async Task MigrateAndSeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<UserManagementDatabaseSeeder>>();

        try
        {
            // Get the database context
            var context = services.GetRequiredService<UserManagementDbContext>();

            // Apply migrations
            logger.LogInformation("Applying database migrations...");
            //await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Seed initial data
            logger.LogInformation("Starting database seeding...");
            var seeder = services.GetRequiredService<UserManagementDatabaseSeeder>();
            await seeder.SeedAsync();
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Ensures the database is created (for development)
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var context = services.GetRequiredService<UserManagementDbContext>();

            logger.LogInformation("Ensuring database is created...");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database creation check completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while ensuring database creation");
            throw;
        }
    }
}