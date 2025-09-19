// ========================================
// CQRS/MediatorExtensions.cs
// ========================================
using Booksy.Core.Application.Behaviors;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Application.Validators;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Infrastructure.Core.CQRS;


/// <summary>
/// Extension methods for MediatR configuration
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// Adds MediatR with pipeline behaviors
    /// </summary>
    public static IServiceCollection AddMediatorWithBehaviors(
        this IServiceCollection services,
        params Type[] assemblyMarkerTypes)
    {
        services.AddMediatR(cfg =>
        {
            foreach (var markerType in assemblyMarkerTypes)
            {
                cfg.RegisterServicesFromAssembly(markerType.Assembly);
            }
        });

        // Register behaviors
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

        services.AddTransient<IValidator<PaginationRequest>, PaginationRequestValidator>();

        return services;
    }
}
