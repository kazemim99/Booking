

using Booksy.Core.Application.Behaviors;
using Booksy.Infrastructure.Core.CQRS;
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
            // Add MediatR
            services.AddMediatorWithBehaviors(typeof(UserManagementApplicationExtensions).Assembly);

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