// ========================================
// Booksy.ServiceCatalog.Application/Specifications/Service/ServiceCatalogValidators.cs
// ========================================
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider;
using Booksy.ServiceCatalog.Application.Commands.Provider.UpdateBusinessProfile;
using Booksy.ServiceCatalog.Application.Commands.Service.CreateService;
using Booksy.ServiceCatalog.Application.Commands.Service.UpdateService;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.ServiceCatalog.Application.Validators.Common
{
    /// <summary>
    /// Registration of FluentValidation validators for ServiceCatalog
    /// </summary>
    public static class ServiceCatalogValidators
    {
        public static IServiceCollection AddServiceCatalogValidators(this IServiceCollection services)
        {
            // Command Validators
            services.AddScoped<IValidator<RegisterProviderCommand>, RegisterProviderCommandValidator>();
            services.AddScoped<IValidator<UpdateBusinessProfileCommand>, UpdateBusinessProfileCommandValidator>();
            services.AddScoped<IValidator<CreateServiceCommand>, CreateServiceCommandValidator>();
            services.AddScoped<IValidator<UpdateServiceCommand>, UpdateServiceCommandValidator>();

            // Domain Value Object Validators
            services.AddScoped<IValidator<OperatingHours>, OperatingHoursValidator>();
            services.AddScoped<IValidator<BusinessAddress>, BusinessAddressValidator>();
            services.AddScoped<IValidator<ContactInfo>, ContactInfoValidator>();
            services.AddScoped<IValidator<Price>, PriceValidator>();
            services.AddScoped<IValidator<Duration>, DurationValidator>();

            return services;
        }
    }
}