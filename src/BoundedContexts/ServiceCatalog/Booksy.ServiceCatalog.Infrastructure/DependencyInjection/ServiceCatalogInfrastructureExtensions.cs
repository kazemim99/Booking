using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Infrastructure.Core.Caching;
using Booksy.Infrastructure.Core.EventBus;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Implementations;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.Services;
using Booksy.ServiceCatalog.Infrastructure.Notifications;
using Booksy.ServiceCatalog.Infrastructure.Notifications.Email;
using Booksy.ServiceCatalog.Infrastructure.Notifications.Push;
using Booksy.ServiceCatalog.Infrastructure.Notifications.Sms;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Repositories;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders;
using Booksy.ServiceCatalog.Infrastructure.Queries;
using Booksy.ServiceCatalog.Infrastructure.Services.Application;
using Booksy.ServiceCatalog.Infrastructure.Services.Domain;
using Booksy.ServiceCatalog.Infrastructure.Services.Images;
using Booksy.ServiceCatalog.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booksy.ServiceCatalog.Infrastructure.DependencyInjection
{
    public static class ServiceCatalogInfrastructureExtensions
    {
        public static IServiceCollection AddServiceCatalogInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
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
                    provider.GetRequiredService<ILogger<EfCoreUnitOfWork<ServiceCatalogDbContext>>>(),
                    provider.GetRequiredService<IDomainEventDispatcher>()));

            // Repositories
            services.AddScoped<IProviderReadRepository, ProviderReadRepository>();
            services.AddScoped<IProviderWriteRepository, ProviderWriteRepository>();
            services.AddScoped<IServiceReadRepository, ServiceReadRepository>();
            services.AddScoped<IServiceWriteRepository, ServiceWriteRepository>();
            services.AddScoped<IBookingReadRepository, BookingReadRepository>();
            services.AddScoped<IBookingWriteRepository, BookingWriteRepository>();

            // Notification Repositories
            services.AddScoped<INotificationReadRepository, NotificationReadRepository>();
            services.AddScoped<INotificationWriteRepository, NotificationWriteRepository>();
            services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
            services.AddScoped<IUserNotificationPreferencesRepository, UserNotificationPreferencesRepository>();

            // Notification Services
            services.AddNotificationServices();

            services.AddScoped<IProviderApplicationService, ProviderApplicationService>();
            services.AddScoped<IServiceApplicationService, ServiceApplicationService>();
            services.AddScoped<IBusinessValidationService, BusinessValidationService>();
            services.AddScoped<IProviderRegistrationService, ProviderRegistrationService>();
            services.AddScoped<IServiceQueryRepository, ServiceQueryRepository>();
            services.AddScoped<ITokenService, TokenService>();


            // Domain Services
            services.AddScoped<IBusinessRuleService, BusinessRuleService>();
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IImageOptimizationService, ImageSharpOptimizationService>();
            services.AddScoped<Application.Services.IImageStorageService, Infrastructure.Services.ImageStorageService>();
            services.AddScoped<Domain.DomainServices.IAvailabilityService, Application.Services.AvailabilityService>();

            // Application Services
            services.AddScoped<IProviderApplicationService, ProviderApplicationService>();

            // CAP Event Bus with Outbox Pattern
            services.AddCapEventBus<ServiceCatalogDbContext>(configuration, "ServiceCatalog");

            // HTTP Client for UserManagement API
            services.AddHttpClient("UserManagementAPI", client =>
            {
                var baseUrl = configuration["Services:UserManagement:BaseUrl"]
                    ?? "https://localhost:5021/api";
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // Optional: Add API key for service-to-service authentication
                var apiKey = configuration["Services:UserManagement:ApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                }
            });

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

            // Add generic caching decorators for read repositories using Redis/ICacheService
            // This uses the generic CachedRepositoryDecorator which supports both Redis and In-Memory caching
            services.Decorate<IProviderReadRepository>((inner, provider) =>
                new CachedRepositoryDecorator<Provider, ProviderId>(
                    inner,
                    provider.GetRequiredService<ICacheService>(),
                    provider.GetRequiredService<IOptions<CacheSettings>>(),
                    provider.GetRequiredService<ILogger<CachedRepositoryDecorator<Provider, ProviderId>>>()));

            services.Decorate<IServiceReadRepository>((inner, provider) =>
                new CachedRepositoryDecorator<Service, ServiceId>(
                    inner,
                    provider.GetRequiredService<ICacheService>(),
                    provider.GetRequiredService<IOptions<CacheSettings>>(),
                    provider.GetRequiredService<ILogger<CachedRepositoryDecorator<Service, ServiceId>>>()));

            return services;
        }

        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            try
            {


            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();

            // Apply migrations
            await context.Database.MigrateAsync();

            // Seed data
            await seeder.SeedAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Adds notification services to the service collection.
        /// This respects bounded context architecture by keeping ServiceCatalog
        /// notification implementations within the ServiceCatalog bounded context.
        /// </summary>
        public static IServiceCollection AddNotificationServices(this IServiceCollection services)
        {
            // Template Engine & Services
            services.AddSingleton<ITemplateEngine, TemplateEngine>();
            services.AddScoped<INotificationTemplateService, NotificationTemplateService>();

            // Multi-Channel Notification Services
            services.AddScoped<IEmailNotificationService, SendGridEmailNotificationService>();
            services.AddScoped<ISmsNotificationService, RahyabSmsNotificationService>();
            services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
            services.AddScoped<IInAppNotificationService, InAppNotificationService>();

            // HTTP Clients for notification services
            services.AddHttpClient<SendGridEmailNotificationService>();
            services.AddHttpClient<RahyabSmsNotificationService>();

            return services;
        }
    }
}
