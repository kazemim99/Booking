// ========================================
// Booksy.ServiceCatalog.Application/DependencyInjection/ServiceCatalogApplicationExtensions.cs
// ========================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using System.Reflection;
using Booksy.ServiceCatalog.Application.Mappings;

namespace Booksy.ServiceCatalog.Application.DependencyInjection
{
    public static class ServiceCatalogApplicationExtensions
    {
        public static IServiceCollection AddServiceCatalogApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

            // Register FluentValidation
            services.AddValidatorsFromAssembly(assembly);

            // Register Application Services
            //services.AddScoped<IServiceCatalogIntegrationService, ServiceCatalogIntegrationService>();

            // Register AutoMapper
            services.AddAutoMapper(cfg=> { 
            
            },typeof(PriceRequest));

            return services;
        }
    }
}