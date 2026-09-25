
using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Infrastructure.Core.CQRS;
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Infrastructure.Core.EventBus;
using AsanRezerve.Infrastructure.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // Add Caching: HybridCache (L1 in-process + L2 Redis) for read models, over the host's one Redis connection
        services.AddAsanRezerveCaching(configuration);

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
}
