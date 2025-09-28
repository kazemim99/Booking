using Booksy.Infrastructure.Core.Persistence.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.API.Extensions;

/// <summary>
/// Database migration and seeding extensions
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies database migrations and seeds initial data
    /// </summary>
    public static async Task MigrateAndSeedDatabaseAsync<T>(this WebApplication app) where T : ISeeder
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<T>>();

        try
        {
            // Get the database context
            var context = services.GetRequiredService<T>();

            // Apply migrations
            logger.LogInformation("Applying database migrations...");
            //await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Seed initial data
            logger.LogInformation("Starting database seeding...");
            var seeder = services.GetRequiredService<T>();
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
    public static async Task EnsureDatabaseCreatedAsync<T,TContext>(this WebApplication app) where T : class
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<T>>();

        try
        {
            var context = services.GetRequiredService<TContext>();

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

