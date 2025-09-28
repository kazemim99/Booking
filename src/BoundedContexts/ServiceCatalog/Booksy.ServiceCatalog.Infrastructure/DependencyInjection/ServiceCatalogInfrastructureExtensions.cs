﻿using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Infrastructure.Core.DependencyInjection;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
using Booksy.ServiceCatalog.Application.Services.Implementations;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Services;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders;
using Booksy.ServiceCatalog.Infrastructure.Queries;
using Booksy.ServiceCatalog.Infrastructure.Services.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.DependencyInjection
{
    public static class ServiceCatalogInfrastructureExtensions
    {
        public static IServiceCollection AddServiceCatalogInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddInfrastructureCore(configuration);

            // Database Context
            services.AddDbContext<ServiceCatalogDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("ServiceCatalog")
                    ?? configuration.GetConnectionString("DefaultConnection");

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ServiceCatalogDbContext).Assembly.FullName);
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "user_management");
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });

                // Enable logging in development
                if (configuration.GetValue<bool>("DatabaseSettings:EnableSensitiveDataLogging"))
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);

                }
            });

            services.AddScoped<DbContext>();

            services.AddScoped<ISeeder, ServiceCatalogDatabaseSeeder>();
            // Unit of Work
            services.AddScoped<IUnitOfWork>(provider =>
                new EfCoreUnitOfWork<ServiceCatalogDbContext>(
                    provider.GetRequiredService<ServiceCatalogDbContext>(),
                    provider.GetRequiredService<ILogger<EfCoreUnitOfWork<ServiceCatalogDbContext>>>()));

            // Repositories
            services.AddScoped<IProviderReadRepository, ProviderReadRepository>();
            services.AddScoped<IProviderWriteRepository, ProviderWriteRepository>();
            services.AddScoped<IServiceReadRepository, ServiceReadRepository>();
            services.AddScoped<IServiceWriteRepository, ServiceWriteRepository>();


            services.AddScoped<IProviderApplicationService, ProviderApplicationService>();
            services.AddScoped<IServiceApplicationService, ServiceApplicationService>();
            services.AddScoped<IBusinessValidationService, BusinessValidationService>();
            services.AddScoped<IProviderRegistrationService, ProviderRegistrationService>();
            services.AddScoped<IServiceQueryRepository, ServiceQueryRepository>();


            // Domain Services
            services.AddScoped<IBusinessRuleService, BusinessRuleService>();

            // Application Services
            services.AddScoped<IProviderApplicationService, ProviderApplicationService>();

            // Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<ServiceCatalogDbContext>(
                    name: "servicecatalog-db",
                    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                    tags: new[] { "db", "servicecatalog" });


            return services;
        }

        public static IServiceCollection AddServiceCatalogInfrastructureWithCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddServiceCatalogInfrastructure(configuration);

            // Add caching decorators for read repositories
            services.Decorate<IProviderReadRepository, CachedProviderReadRepository>();
            services.Decorate<IServiceReadRepository, CachedServiceReadRepository>();

            return services;
        }

        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();

            // Apply migrations
            await context.Database.MigrateAsync();

            // Seed data
            await seeder.SeedAsync();
        }
    }
}
