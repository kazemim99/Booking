using Booksy.Infrastructure.Core.Persistence.Base;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Booksy.API.Extensions;

/// <summary>
/// Database migration and seeding extensions
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies database migrations and seeds initial data for a bounded context
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    /// <typeparam name="TSeeder">The seeder type</typeparam>
    /// <param name="app">The application builder</param>
    /// <param name="seedData">Whether to seed data (typically true for development/test environments)</param>
    public static async Task MigrateAndSeedDatabaseAsync<TContext, TSeeder>(
        this WebApplication app,
        bool seedData = false)
        where TContext : DbContext
        where TSeeder : ISeeder
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TContext>>();
        var contextName = typeof(TContext).Name;

        try
        {
            var context = services.GetRequiredService<TContext>();

            logger.LogInformation("[{Context}] Starting database initialization", contextName);

            try
            {
                // Check if database can be connected
                if (await context.Database.CanConnectAsync())
                {
                    logger.LogInformation("[{Context}] Database connection successful", contextName);

                    // Get pending migrations
                    var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();

                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("[{Context}] Applying {Count} pending migrations: {Migrations}",
                            contextName,
                            pendingMigrations.Count,
                            string.Join(", ", pendingMigrations));

                        await context.Database.MigrateAsync();

                        logger.LogInformation("[{Context}] Migrations applied successfully", contextName);
                    }
                    else
                    {
                        logger.LogInformation("[{Context}] No pending migrations", contextName);
                    }
                }
                else
                {
                    logger.LogWarning("[{Context}] Cannot connect to database, skipping migration", contextName);
                    return;
                }
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "42P07") // duplicate_table
            {
                logger.LogWarning("[{Context}] Table already exists, continuing: {Message}",
                    contextName, pgEx.Message);
                // Continue - table already exists, which is acceptable
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "42710") // duplicate_object
            {
                logger.LogWarning("[{Context}] Database object already exists, continuing: {Message}",
                    contextName, pgEx.Message);
                // Continue - object already exists
            }

            // Seed initial data if requested
            if (seedData)
            {
                logger.LogInformation("[{Context}] Starting database seeding", contextName);
                var seeder = services.GetRequiredService<TSeeder>();
                await seeder.SeedAsync();
                logger.LogInformation("[{Context}] Database seeding completed successfully", contextName);
            }
            else
            {
                logger.LogInformation("[{Context}] Skipping database seeding (seedData=false)", contextName);
            }

            logger.LogInformation("[{Context}] Database initialization completed", contextName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Context}] An error occurred while initializing the database", contextName);
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

