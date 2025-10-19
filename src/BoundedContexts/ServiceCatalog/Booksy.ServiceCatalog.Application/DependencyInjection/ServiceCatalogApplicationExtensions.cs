// ========================================
// Booksy.ServiceCatalog.Application/DependencyInjection/ServiceCatalogApplicationExtensions.cs
// ========================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Booksy.ServiceCatalog.Application.Mappings;
using Booksy.Infrastructure.Core.CQRS;
using MediatR;
using Booksy.Infrastructure.Core.EventBus.Abstractions;

namespace Booksy.ServiceCatalog.Application.DependencyInjection
{
    public static class ServiceCatalogApplicationExtensions
    {
        public static IServiceCollection AddServiceCatalogApplication(this IServiceCollection services)
        {
            var assembly = typeof(ServiceCatalogApplicationExtensions).Assembly;

            // Register MediatR ONLY for CQRS (Commands/Queries), NOT for domain events
            services.AddMediatorWithBehaviors(assembly);

            // Register domain event handlers explicitly (NO MediatR!)
            RegisterDomainEventHandlers(services, assembly);

            // Register AutoMapper
            services.AddAutoMapper(cfg => { }, typeof(PriceRequest));

            return services;
        }

        /// <summary>
        /// Registers all domain event handlers for SimpleDomainEventDispatcher
        /// Scans assembly and registers each handler with its IDomainEventHandler interface
        /// </summary>
        private static void RegisterDomainEventHandlers(IServiceCollection services, Assembly assembly)
        {
            var domainEventHandlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
                .ToList();

            foreach (var handlerType in domainEventHandlerTypes)
            {
                // Find all IDomainEventHandler<TEvent> interfaces this type implements
                var domainEventHandlerInterfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

                foreach (var domainEventHandlerInterface in domainEventHandlerInterfaces)
                {
                    // Register the handler with its interface
                    // SimpleDomainEventDispatcher will resolve these via serviceProvider.GetServices()
                    services.AddScoped(domainEventHandlerInterface, handlerType);
                }
            }
        }
    }
}