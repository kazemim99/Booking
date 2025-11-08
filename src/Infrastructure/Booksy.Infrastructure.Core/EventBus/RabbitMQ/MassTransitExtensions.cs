// ========================================
// EventBus/RabbitMQ/MassTransitExtensions.cs
// ========================================
using Booksy.Infrastructure.Core.EventBus.RabbitMQ.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Booksy.Infrastructure.Core.EventBus.RabbitMQ;

/// <summary>
/// Extension methods for configuring MassTransit with RabbitMQ
/// </summary>
public static class MassTransitExtensions
{
    /// <summary>
    /// Adds MassTransit with RabbitMQ configuration to the service collection
    /// </summary>
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        // Bind RabbitMQ settings
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

        // Add the RabbitMqEventBus as a service
        services.AddScoped<RabbitMqEventBus>();

        // Configure MassTransit
        services.AddMassTransit(x =>
        {
            // Allow custom consumer registration
            configureConsumers?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                var settings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

                cfg.Host(settings.Host, settings.VirtualHost, h =>
                {
                    h.Username(settings.Username);
                    h.Password(settings.Password);
                    h.Heartbeat(TimeSpan.FromSeconds(settings.HeartbeatInterval));

                        h.ConnectionName(settings.ConnectionName);
                    if (settings.Port != 5672) // Only set if not default port
                    {
                        // Note: Port configuration might need to be set via connection string
                        // For custom ports, use connection string format: rabbitmq://host:port/vhost
                    }

                    if (settings.UseSsl)
                    {
                        h.UseSsl(s =>
                        {
                            s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                        });
                    }
                });

                // Configure retry policy
                cfg.UseMessageRetry(r =>
                {
                    r.Incremental(
                        settings.RetryCount,
                        TimeSpan.FromMilliseconds(settings.RetryDelayMs),
                        TimeSpan.FromMilliseconds(settings.RetryDelayMs * 2));
                });

                // Configure error handling
                if (settings.UseDeadLetterQueue)
                {
                    cfg.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(30)));
                }

                // Configure message serialization
                cfg.ConfigureJsonSerializerOptions(options =>
                {
                    options.PropertyNameCaseInsensitive = true;
                    return options;
                });

                // Set concurrent consumer limit
                cfg.ConcurrentMessageLimit = settings.ConcurrentMessageLimit;

                // Configure all endpoints
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Registers a domain event consumer with MassTransit
    /// </summary>
    public static void AddDomainEventConsumer<TEvent>(
        this IBusRegistrationConfigurator configurator,
        string? queueName = null)
        where TEvent : class, Booksy.Core.Domain.Abstractions.Events.IDomainEvent
    {
        configurator.AddConsumer<DomainEventConsumer<TEvent>>(cfg =>
        {
            // Configure endpoint for this consumer
            cfg.UseConcurrentMessageLimit(16);
        });

        if (!string.IsNullOrEmpty(queueName))
        {
            configurator.AddConfigureEndpointsCallback((name, cfg) =>
            {
                if (cfg is IRabbitMqReceiveEndpointConfigurator rabbitCfg)
                {
                    rabbitCfg.PrefetchCount = 16;
                    rabbitCfg.Durable = true;
                    rabbitCfg.AutoDelete = false;
                }
            });
        }
    }
}
