// ========================================
// EventBus/CapEventBusExtensions.cs
// ========================================
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Booksy.Infrastructure.Core.EventBus;

/// <summary>
/// Extension methods for configuring CAP event bus
/// </summary>
public static class CapEventBusExtensions
{
    /// <summary>
    /// Adds CAP event bus with a PostgreSQL outbox and the in-process in-memory transport.
    /// The modular monolith runs CAP in-process — there is no message broker.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type for outbox pattern</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="contextName">Bounded context name (e.g., "ServiceCatalog", "UserManagement")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCapEventBus<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string contextName)
        where TDbContext : DbContext
    {
        // Get connection strings
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is required for CAP");

        // CAP can only be registered ONCE per process. In the modular monolith every
        // bounded context's AddXInfrastructure() calls this method, so guard against a
        // second registration: the first caller configures the bus, later callers only
        // ensure the publisher is present. [CapSubscribe] handlers from every referenced
        // assembly are discovered regardless of which context registered the bus.
        if (services.Any(d => d.ServiceType == typeof(ICapPublisher)))
        {
            services.TryAddScoped<IIntegrationEventPublisher, CapIntegrationEventPublisher>();
            return services;
        }

        services.AddCap(options =>
        {
            // ========================================
            // PostgreSQL Outbox Configuration
            // ========================================
            options.UsePostgreSql(postgresOptions =>
            {
                postgresOptions.ConnectionString = connectionString;

                postgresOptions.Schema = "cap";

            });

            // ========================================
            // Transport: in-process in-memory queue (no external broker)
            // ========================================
            options.UseInMemoryMessageQueue();

            // ========================================
            // CAP General Configuration
            // ========================================

            // Consumer group: a single shared group for the monolith process. All
            // [CapSubscribe] handlers run in-process; each integration-event topic has a
            // single subscriber, so one consumer group is correct and avoids the
            // order-dependent group naming that a per-context group would introduce.
            options.ConsumerThreadCount = 1; // Single thread per consumer for ordering
            options.DefaultGroupName = "booksy";

            // Versioning and serialization
            options.Version = "v1";
            options.UseDashboard(); // Enable CAP dashboard at /cap

            // Retry policy
            options.FailedRetryInterval = 60; // Retry every 60 seconds
            options.FailedRetryCount = 3; // Retry 3 times before moving to failed

            // Succeeded messages cleanup
            options.SucceedMessageExpiredAfter = 3600; // Keep for 1 hour (3600 seconds)

            // Failed messages cleanup (keep longer for investigation)
            options.FailedMessageExpiredAfter = 7 * 24 * 3600; // Keep for 7 days

            // Collection interval for cleanup
            options.CollectorCleaningInterval = 300; // Run cleanup every 5 minutes
        });

        // Register the CAP-based publisher
        services.AddScoped<IIntegrationEventPublisher, CapIntegrationEventPublisher>();

        return services;
    }
}
