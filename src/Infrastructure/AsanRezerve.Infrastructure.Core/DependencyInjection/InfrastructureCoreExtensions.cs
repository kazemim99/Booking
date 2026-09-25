
using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Infrastructure.Core.CQRS;
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Infrastructure.Core.EventBus;
using AsanRezerve.Infrastructure.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.Infrastructure.Core.Persistence.Outbox;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Infrastructure.Core.Persistence.Base;

namespace AsanRezerve.Infrastructure.Core.DependencyInjection;



/// <summary>
/// Extension methods for registering infrastructure core services
/// </summary>
public static class InfrastructureCoreExtensions
{
    public static IServiceCollection AddInfrastructureCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add CQRS
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<IQueryBus, QueryBus>();

        // Add Core Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Use SimpleDomainEventDispatcher instead of MediatR-based dispatcher
        services.AddScoped<IDomainEventDispatcher, SimpleDomainEventDispatcher>();

        services.AddHttpContextAccessor();

        // Add Event Bus
        services.AddEventBus(configuration);

        // Add Caching
        services.AddCaching(configuration);

        // Add Persistence
        services.AddScoped(typeof(IQueryRepositoryBase<,>), typeof(QueryRepositoryBase<,>));
        //services.AddScoped(typeof(IWriteRepository<,>), typeof(EfWriteRepositoryBase<,,>));
        //services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        //// Add Outbox
        services.AddScoped(typeof(IOutboxProcessor<>), typeof(OutboxProcessor<>));

        return services;
    }

    private static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheSettings = configuration.GetSection("Cache").Get<CacheSettings>() ?? new CacheSettings();

        services.Configure<CacheSettings>(configuration.GetSection("Cache"));

        switch (cacheSettings.Provider?.ToLower())
        {
            case "redis":
                if (!string.IsNullOrEmpty(cacheSettings.RedisConnectionString))
                {
                    services.AddSingleton<IConnectionMultiplexer>(
                        ConnectionMultiplexer.Connect(cacheSettings.RedisConnectionString));
                    services.AddSingleton<ICacheService, RedisCacheService>();
                }
                else
                {
                    services.AddMemoryCache();
                    services.AddSingleton<ICacheService, InMemoryCacheService>();
                }
                break;

            case "inmemory":
            default:
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, InMemoryCacheService>();
                break;
        }

        return services;
    }
}
