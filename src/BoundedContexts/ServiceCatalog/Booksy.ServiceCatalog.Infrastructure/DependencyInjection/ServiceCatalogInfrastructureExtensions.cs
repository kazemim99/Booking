using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Infrastructure.Core.EventBus;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
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
using SendGrid;
using Booksy.Infrastructure.External.Payment;
using Booksy.Infrastructure.External.Payment.ZarinPal;
using Booksy.Infrastructure.External.Payment.IDPay;
using Booksy.Infrastructure.External.Payment.Behpardakht;
using Booksy.Infrastructure.External.Payment.Parsian;
using Booksy.Infrastructure.External.Payment.Saman;
using Booksy.ServiceCatalog.Infrastructure.ExternalServices.Sms;
using System.Threading;

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
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ServiceCatalog");
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

            services.AddScoped<ISeeder, ServiceCatalogDatabaseSeederOrchestrator>();
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
            services.AddScoped<IProviderAvailabilityReadRepository, ProviderAvailabilityReadRepository>();
            services.AddScoped<IProviderAvailabilityWriteRepository, ProviderAvailabilityWriteRepository>();
            services.AddScoped<IReviewReadRepository, ReviewReadRepository>();
            services.AddScoped<IReviewWriteRepository, ReviewWriteRepository>();

            // Payment and Payout Repositories
            services.AddScoped<IPaymentReadRepository, PaymentReadRepository>();
            services.AddScoped<IPaymentWriteRepository, PaymentWriteRepository>();
            services.AddScoped<IPayoutReadRepository, PayoutReadRepository>();
            services.AddScoped<IPayoutWriteRepository, PayoutWriteRepository>();

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

            // External Services (Payment Gateways, SMS, Email, Analytics, Storage, etc.)
            // This registers: IPaymentGateway, IPaymentGatewayFactory, IZarinPalService,
            // IIDPayService, IBehpardakhtService, ISendGridClient, and other external services
            services.AddExternalServices(configuration);

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

            // Add caching decorators for read repositories using Redis distributed cache
            // These decorators implement the full repository interfaces and use ICacheService (Redis/InMemory)
            services.Decorate<IProviderReadRepository, CachedProviderReadRepository>();
            services.Decorate<IServiceReadRepository, CachedServiceReadRepository>();

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
                //await context.Database.MigrateAsync();

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

            // Register old ISmsNotificationService for booking event handlers
            services.AddHttpClient<ExternalServices.Sms.KavenegarSmsService>();
            services.AddScoped<Application.Services.ISmsNotificationService, ExternalServices.Sms.KavenegarSmsService>();

            // HTTP Clients for notification services
            services.AddHttpClient<SendGridEmailNotificationService>();
            services.AddHttpClient<RahyabSmsNotificationService>();

            return services;
        }
    }
}
