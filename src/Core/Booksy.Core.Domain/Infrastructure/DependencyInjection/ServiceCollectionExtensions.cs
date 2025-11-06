using Microsoft.Extensions.DependencyInjection;
using Booksy.SharedKernel.Infrastructure.Persistence;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Booksy.Core.Domain.Domain.Exceptions;
using Booksy.Core.Domain.Application.Behaviors;
using Booksy.Core.Domain.Infrastructure.Services;
using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.Infrastructure.Configuration;
using Booksy.Core.Domain.Infrastructure.EventBus;
using Booksy.Core.Domain.Infrastructure.Middleware.Handlers;
using Booksy.Core.Application.Behaviors;

namespace Booksy.Core.Domain.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SharedKernel services
    /// </summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

            // Add behaviors (order matters - executed in registration order)
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        });


        services.AddHttpContextAccessor();


        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        // Event Bus
        services.AddScoped<IEventBus, InMemoryEventBus>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<DomainEventDispatcher>();
        
        // Unit of Work

        return services;
    }


 

    public static IServiceCollection AddApiResponseWrapper(
          this IServiceCollection services,
          IConfiguration configuration)
    {

        services.Configure<ApiResponseOptions>(configuration.GetSection("ApiResponse"));

        // Register exception handlers
        services.AddSingleton<IExceptionHandler, DomainExceptionHandler>();
        services.AddSingleton<IExceptionHandler, ValidationExceptionHandler>();

        // Register the chained handler strategy
        services.AddSingleton<IExceptionHandlerStrategy, ChainedExceptionHandler>();

        return services;
    }

    /// <summary>
    /// Registers in-memory repositories for development/testing
    /// </summary>
    public static IServiceCollection AddInMemoryRepositories(this IServiceCollection services)
    {
        services.AddSingleton(typeof(InMemoryRepository<,>));

        return services;
    }

    /// <summary>
    /// Registers Entity Framework repositories for production
    /// </summary>
    public static IServiceCollection AddEntityFrameworkRepositories(this IServiceCollection services, string connectionString)
    {
        // TODO: Add EF Core setup
        // services.AddDbContext<BookingDbContext>(options => options.UseSqlServer(connectionString));
        // services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        // services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        throw new NotImplementedException("EF repositories not implemented yet");
    }
}

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApiResponseWrapper(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiResponseMiddleware>();
    }
}