// ========================================
// Booksy.ServiceCatalog.Application/DependencyInjection/ServiceCatalogApplicationExtensions.cs
// ========================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Booksy.ServiceCatalog.Application.Mappings;
using Booksy.ServiceCatalog.Application.Services.BackgroundServices;
using Booksy.Infrastructure.Core.CQRS;
using MediatR;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Core.Application.Authorization;
using Booksy.ServiceCatalog.Application.Authorization;
using Booksy.ServiceCatalog.Application.Commands.Booking.CancelBooking;
using Booksy.ServiceCatalog.Application.Commands.Booking.RescheduleBooking;
using Booksy.ServiceCatalog.Application.Commands.Payment.RefundPayment;

namespace Booksy.ServiceCatalog.Application.DependencyInjection
{
    public static class ServiceCatalogApplicationExtensions
    {
        public static IServiceCollection AddServiceCatalogApplication(this IServiceCollection services)
        {
            var assembly = typeof(ServiceCatalogApplicationExtensions).Assembly;

            // Register MediatR ONLY for CQRS (Commands/Queries), NOT for domain events
            services.AddMediatorWithBehaviors(assembly);

            // Resource-ownership resolvers consumed by AuthorizationBehavior (C1
            // harden-resource-authorization). One generic resolver per resource,
            // registered per state-changing command that carries an ownership marker.
            services.AddScoped<IResourceOwnershipResolver<CancelBookingCommand>, BookingOwnershipResolver<CancelBookingCommand>>();
            services.AddScoped<IResourceOwnershipResolver<RescheduleBookingCommand>, BookingOwnershipResolver<RescheduleBookingCommand>>();
            services.AddScoped<IResourceOwnershipResolver<RefundPaymentCommand>, PaymentOwnershipResolver<RefundPaymentCommand>>();
            // Provider settings: only the owning provider (or an admin) may change the booking/deposit policy.
            services.AddScoped<
                IResourceOwnershipResolver<Commands.Provider.UpdateBookingPreferences.UpdateBookingPreferencesCommand>,
                ProviderOwnershipResolver<Commands.Provider.UpdateBookingPreferences.UpdateBookingPreferencesCommand>>();

            // Register domain event handlers explicitly (NO MediatR!)
            RegisterDomainEventHandlers(services, assembly);

            // Register AutoMapper
            services.AddAutoMapper(cfg => { }, typeof(PriceRequest));

            return services;
        }

        /// <summary>
        /// Adds background services for notification processing
        /// </summary>
        public static IServiceCollection AddNotificationBackgroundServices(this IServiceCollection services)
        {
            // Background services for notification processing
            services.AddHostedService<NotificationProcessorService>();
            services.AddHostedService<ScheduledNotificationService>();
            services.AddHostedService<NotificationCleanupService>();

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