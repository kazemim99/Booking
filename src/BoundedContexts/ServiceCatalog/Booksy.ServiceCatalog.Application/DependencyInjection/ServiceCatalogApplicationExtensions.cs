// ========================================
// Booksy.ServiceCatalog.Application/DependencyInjection/ServiceCatalogApplicationExtensions.cs
// ========================================
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using System.Reflection;
using Booksy.ServiceCatalog.Application.Mappings;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
using Booksy.Infrastructure.Core.CQRS;

namespace Booksy.ServiceCatalog.Application.DependencyInjection
{
    public static class ServiceCatalogApplicationExtensions
    {
        public static IServiceCollection AddServiceCatalogApplication(this IServiceCollection services)
        {

            services.AddMediatorWithBehaviors(typeof(ServiceCatalogApplicationExtensions).Assembly);
         

            // Register AutoMapper
            services.AddAutoMapper(cfg=> { 
            
            },typeof(PriceRequest));

            return services;
        }
    }
}