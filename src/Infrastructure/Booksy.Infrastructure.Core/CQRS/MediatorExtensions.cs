// ========================================
// CQRS/MediatorExtensions.cs
// ========================================
using Booksy.Core.Application.Behaviors;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Application.Validators;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
        this IServiceCollection services,Assembly assembly)
    {

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        });

        // Register behaviors

        services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

        services.AddTransient<IValidator<PaginationRequest>, PaginationRequestValidator>();

        return services;
    }
}
