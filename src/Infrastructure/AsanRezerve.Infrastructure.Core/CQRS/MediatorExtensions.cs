// ========================================
// CQRS/MediatorExtensions.cs
// ========================================
using AsanRezerve.Core.Application.Behaviors;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.Core.Application.Validators;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AsanRezerve.Infrastructure.Core.CQRS;


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
            // Resource-ownership enforcement (C1). After validation, before Transaction
            // so a 403 never opens a DB transaction or triggers a side effect (e.g. refund).
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AsanRezerve.Core.Application.Authorization.AuthorizationBehavior<,>));
            // C2 §2 atomic idempotency (IRequireIdempotency commands only). After authorization (only authorized
            // requests reserve a key), before Transaction so a duplicate is rejected before any handler side effect.
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AsanRezerve.Core.Application.Behaviors.IdempotencyBehavior<,>));
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
