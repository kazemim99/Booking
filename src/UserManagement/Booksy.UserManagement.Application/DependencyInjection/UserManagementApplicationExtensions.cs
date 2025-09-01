

using Booksy.Core.Application.Behaviors;
using Booksy.UserManagement.Application.EventHandlers;
using Booksy.UserManagement.Application.Services.Implementations;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Booksy.UserManagement.Application.DependencyInjection
{
    public static class UserManagementApplicationExtensions
    {
        public static IServiceCollection AddUserManagementApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Add MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);

                // Add pipeline behaviors
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            });

            // Add FluentValidation
            services.AddValidatorsFromAssembly(assembly);

            // Add Application Services
            services.AddScoped<IUserRegistrationService, UserRegistrationService>();

            // Add Event Handlers
            services.AddScoped<UserRegisteredEventHandler>();
            services.AddScoped<UserActivatedEventHandler>();
            services.AddScoped<CreateProviderProfileEventHandler>();

            return services;
        }
    }
}