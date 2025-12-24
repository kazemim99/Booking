// ========================================
// EventBus/CapEventBusExtensions.cs
// ========================================
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Savorboard.CAP.InMemoryMessageQueue;

namespace Booksy.Infrastructure.Core.EventBus;

/// <summary>
/// Extension methods for configuring CAP event bus
/// </summary>
public static class CapEventBusExtensions
{
    /// <summary>
    /// Adds CAP event bus with RabbitMQ and PostgreSQL outbox
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

        var rabbitMqConnection = configuration.GetConnectionString("RabbitMQ")
            ?? configuration["EventBus:RabbitMQ:ConnectionString"]
            ?? "amqp://booksy_admin:Booksy@2024!@localhost:56721";

        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

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
            // RabbitMQ Transport Configuration
            // ========================================
            if (isDevelopment)
            {
                // Use InMemory queue for development/testing (optional)
                // Uncomment to use RabbitMQ even in development
                options.UseInMemoryMessageQueue();

                // Uncomment below to use RabbitMQ in development
                /*
                options.UseRabbitMQ(rabbitOptions =>
                {
                    rabbitOptions.ConnectionFactoryOptions = factory =>
                    {
                        factory.HostName = "localhost";
                        factory.Port = 5672;
                        factory.UserName = "booksy_admin";
                        factory.Password = "Booksy@2024!";

                        factory.AutomaticRecoveryEnabled = true;
                        factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
                        factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);
                    };

                    // Exchange name: booksy.events
                    rabbitOptions.ExchangeName = "booksy.events";

                    // Queue name per context: booksy.servicecatalog.queue
                    rabbitOptions.QueueArguments = new RabbitMQOptions.QueueArgumentsOptions
                    {
                        MessageTTL = 7 * 24 * 60 * 60 * 1000,
                        QueueMode = "lazy",
                    };


                });
                */
            }
            else
            {
                options.UseRabbitMQ(rabbitOptions =>
                {
                    rabbitOptions.ConnectionFactoryOptions = factory =>
                    {
                        factory.Uri = new Uri(rabbitMqConnection);
                        factory.AutomaticRecoveryEnabled = true;
                        factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
                        factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);
                    };

                    rabbitOptions.ExchangeName = "booksy.events";
                    rabbitOptions.QueueArguments = new RabbitMQOptions.QueueArgumentsOptions
                    {
                        MessageTTL = 7 * 24 * 60 * 60 * 1000,
                        QueueMode = "lazy",
                    };
                });
            }

            // ========================================
            // CAP General Configuration
            // ========================================

            // Consumer group (one per bounded context)
            options.ConsumerThreadCount = 1; // Single thread per consumer for ordering
            options.DefaultGroupName = $"booksy.{contextName.ToLower()}";

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
