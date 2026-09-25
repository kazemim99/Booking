

using AsanRezerve.Core.Application.Behaviors;
using AsanRezerve.Infrastructure.Core.CQRS;
using AsanRezerve.UserManagement.Application.Configuration;
using AsanRezerve.UserManagement.Application.EventHandlers;
using AsanRezerve.UserManagement.Application.Services.Implementations;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AsanRezerve.UserManagement.Application.DependencyInjection
{
    public static class UserManagementApplicationExtensions
    {
        public static IServiceCollection AddUserManagementApplication(
            this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            // Add MediatR
            services.AddMediatorWithBehaviors(typeof(UserManagementApplicationExtensions).Assembly);

            // OTP abuse limits. Registered unconditionally so the defaults apply even where no
            // configuration section exists — the protection must never depend on a host remembering
            // to wire it (it used to depend on the build configuration, which was worse).
            if (configuration is null)
            {
                services.AddOptions<OtpProtectionOptions>();
            }
            else
            {
                services.AddOptions<OtpProtectionOptions>()
                    .Bind(configuration.GetSection(OtpProtectionOptions.SectionName));
            }

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