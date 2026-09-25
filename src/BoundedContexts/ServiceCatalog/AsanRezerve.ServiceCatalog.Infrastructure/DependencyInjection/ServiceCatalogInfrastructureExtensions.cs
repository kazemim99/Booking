using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Infrastructure.Core.EventBus;
using AsanRezerve.Infrastructure.Core.EventBus.Abstractions;
using AsanRezerve.Infrastructure.Core.Persistence.Base;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Queries;
using AsanRezerve.ServiceCatalog.Application.Services.Implementations;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.Services;
using AsanRezerve.ServiceCatalog.Infrastructure.Notifications;
using AsanRezerve.ServiceCatalog.Infrastructure.Notifications.Email;
using AsanRezerve.ServiceCatalog.Infrastructure.Notifications.Push;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Repositories;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Seeders;
using AsanRezerve.ServiceCatalog.Infrastructure.Queries;
using AsanRezerve.ServiceCatalog.Infrastructure.Services.Application;
using AsanRezerve.ServiceCatalog.Infrastructure.Services.Domain;
using AsanRezerve.ServiceCatalog.Infrastructure.Services.Images;
using AsanRezerve.ServiceCatalog.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SendGrid;
using AsanRezerve.Infrastructure.External.Payment;
using AsanRezerve.Infrastructure.External.Payment.ZarinPal;
using AsanRezerve.Infrastructure.External.Payment.IDPay;
using AsanRezerve.Infrastructure.External.Payment.Behpardakht;
using AsanRezerve.Infrastructure.External.Payment.Parsian;
using AsanRezerve.Infrastructure.External.Payment.Saman;
using System.Threading;
using AsanRezerve.ServiceCatalog.Application.Abstractions;
using AsanRezerve.ServiceCatalog.Infrastructure.Services;
using AsanRezerve.Infrastructure.External.OTP;
using AsanRezerve.Infrastructure.External.Notifications.Sms;

using AsanRezerve.ServiceCatalog.Application.Services;
using AsanRezerve.ServiceCatalog.Infrastructure.Services.Geocoding;
namespace AsanRezerve.ServiceCatalog.Infrastructure.DependencyInjection
{
    public static class ServiceCatalogInfrastructureExtensions
    {
        public static IServiceCollection AddServiceCatalogInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // HTTP Context Accessor (needed for URL generation)
            services.AddHttpContextAccessor();

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

            // Register ServiceCatalogDbContext as DbContext for OutboxProcessor
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ServiceCatalogDbContext>());

            services.AddScoped<ServiceCatalogDatabaseSeederOrchestrator>();
            services.AddScoped<ISeeder, ServiceCatalogDatabaseSeederOrchestrator>();
            // Unit of Work
            services.AddScoped<IUnitOfWork>(provider =>
                new EfCoreUnitOfWork<ServiceCatalogDbContext>(
                    provider.GetRequiredService<ServiceCatalogDbContext>(),
                    provider.GetRequiredService<ILogger<EfCoreUnitOfWork<ServiceCatalogDbContext>>>(),
                    provider.GetRequiredService<IDomainEventDispatcher>()));

            // Context-scoped Unit of Work (monolith): handlers inject this marker so they
            // always commit against the ServiceCatalog DbContext, never another context's.
            services.AddScoped<AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence.IServiceCatalogUnitOfWork, ServiceCatalogUnitOfWork>();

            // Repositories
            services.AddScoped<IProviderReadRepository, ProviderReadRepository>();
            services.AddScoped<IProviderWriteRepository, ProviderWriteRepository>();
            services.AddScoped<IServiceReadRepository, ServiceReadRepository>();
            services.AddScoped<IServiceWriteRepository, ServiceWriteRepository>();
            services.AddScoped<IBookingReadRepository, BookingReadRepository>();
            services.AddScoped<IBookingWriteRepository, BookingWriteRepository>();

            // C2 payment reconciliation (invariants I1/I3): converge stuck Pending payments.
            services.AddScoped<Payments.IPaymentReconciler, Payments.PaymentReconciler>();
            services.AddHostedService<Payments.PaymentReconciliationBackgroundService>();
            services.AddHostedService<Payments.LedgerMaintenanceBackgroundService>();
            services.AddScoped<
                Application.Queries.Provider.GetProviderClients.IProviderClientsReadService,
                ProviderClientsReadService>();
            services.AddScoped<
                Application.Abstractions.Identity.IPersonDirectory,
                PersonDirectoryReadService>();
            services.AddScoped<IProviderAvailabilityReadRepository, ProviderAvailabilityReadRepository>();
            services.AddScoped<IProviderAvailabilityWriteRepository, ProviderAvailabilityWriteRepository>();
            services.AddScoped<IReviewReadRepository, ReviewReadRepository>();
            services.AddScoped<IReviewWriteRepository, ReviewWriteRepository>();
            // Called inline by review commands, on the command's own DbContext (design D6) — scoped for that reason.
            services.AddScoped<
                Application.Services.Reviews.IProviderRatingRecomputer,
                Reviews.ProviderRatingRecomputer>();

            // Payment and Payout Repositories
            services.AddScoped<IPaymentReadRepository, PaymentReadRepository>();
            services.AddScoped<IPaymentWriteRepository, PaymentWriteRepository>();
            services.AddScoped<IPayoutReadRepository, PayoutReadRepository>();
            services.AddScoped<IPayoutWriteRepository, PayoutWriteRepository>();

            // C5 financial-ledger: append-only double-entry ledger + reconciliation
            services.AddScoped<Domain.Repositories.ILedgerRepository, Persistence.Repositories.LedgerRepository>();
            services.AddScoped<Payments.ILedgerReconciler, Payments.LedgerReconciler>();

            // C2 §2 atomic idempotency reservation (money commands are processed at-most-once per key)
            services.AddScoped<AsanRezerve.Core.Application.Abstractions.Idempotency.IIdempotencyStore,
                Persistence.Idempotency.IdempotencyStore>();

            // Notification Repositories
            services.AddScoped<INotificationReadRepository, NotificationReadRepository>();
            services.AddScoped<INotificationWriteRepository, NotificationWriteRepository>();
            services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
            services.AddScoped<IUserNotificationPreferencesRepository, UserNotificationPreferencesRepository>();

            // Provider Hierarchy Repositories (Invitations & Join Requests)
            services.AddScoped<IProviderInvitationReadRepository, ProviderInvitationReadRepository>();
            services.AddScoped<IProviderInvitationWriteRepository, ProviderInvitationWriteRepository>();
            services.AddScoped<IProviderCustomerRepository, ProviderCustomerRepository>();

            // Organization Membership (Person ↔ Organization link; supersedes ParentProviderId staff)
            services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
            // Append-only audit trail for membership lifecycle events.
            services.AddScoped<IMembershipAuditRepository, MembershipAuditRepository>();

            // Notification Services
            services.AddNotificationServices(configuration);

            services.AddScoped<IProviderApplicationService, ProviderApplicationService>();
            services.AddScoped<IServiceApplicationService, ServiceApplicationService>();
            services.AddScoped<IBusinessValidationService, BusinessValidationService>();
            services.AddScoped<IProviderRegistrationService, ProviderRegistrationService>();
            services.AddScoped<IServiceQueryRepository, ServiceQueryRepository>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUrlService, UrlService>();

            // Invitation & Registration Services
            services.AddScoped<IInvitationRegistrationService, InvitationRegistrationService>();
            services.AddScoped<IDataCloningService, DataCloningService>();


            // Domain Services
            services.AddScoped<IBusinessRuleService, BusinessRuleService>();
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IImageOptimizationService, ImageSharpOptimizationService>();
            services.AddScoped<Application.Services.IImageStorageService, Infrastructure.Services.ImageStorageService>();
            services.AddScoped<Domain.DomainServices.IAvailabilityService, Application.Services.AvailabilityService>();
            // Members are bookable resources keyed by MembershipId (no shadow staff provider).
            services.AddScoped<Application.Services.Interfaces.IMemberBookabilityService,
                Application.Services.MemberBookabilityService>();
            // The single answer to "what is this booking held against, and how are its slots
            // keyed" — shared by booking creation and rescheduling so they cannot drift.
            services.AddScoped<Application.Services.IBookableResourceResolver,
                Application.Services.BookableResourceResolver>();
            // Names a booking's staff member for the customer's own views (never a placeholder or a phone).
            services.AddScoped<Application.Services.IBookingStaffNames,
                Application.Services.BookingStaffNames>();

            // Application Services
            services.AddScoped<IProviderApplicationService, ProviderApplicationService>();

            // External Services (Payment Gateways, SMS, Email, Analytics, Storage, etc.)
            // This registers: IPaymentGateway, IPaymentGatewayFactory, IZarinPalService,
            // IIDPayService, IBehpardakhtService, ISendGridClient, and other external services
            services.AddExternalServices(configuration);

            // CAP Event Bus with Outbox Pattern
            services.AddCapEventBus<ServiceCatalogDbContext>(configuration, "ServiceCatalog");

            // Geocoding for the map picker. Clients call OUR API and the server calls Nominatim:
            // a browser-side call fails wherever the user's network cannot reach the host, and would
            // make every visitor an unidentified client of a shared free service.
            services.AddHttpClient<IGeocodingProvider, NominatimGeocodingProvider>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["Geocoding:BaseUrl"] ?? "https://nominatim.openstreetmap.org");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    "User-Agent", NominatimGeocodingProvider.UserAgent);
            });

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

        /// <param name="seedDemoData">Fake providers/staff/services/bookings for local development.
        /// Never true in production.</param>
        /// <param name="seedReferenceData">Province/city hierarchy and notification templates — needed
        /// in every environment. Idempotent. Test hosts turn it off only to keep startup fast.</param>
        public static async Task InitializeDatabaseAsync(
            this IServiceProvider serviceProvider, bool seedDemoData, bool seedReferenceData = true)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();

            // Resolve the concrete orchestrator, not ISeeder: UserManagement registers its own ISeeder
            // too, so resolving the interface returns whichever bounded context registered last.
            var seeder = scope.ServiceProvider.GetRequiredService<ServiceCatalogDatabaseSeederOrchestrator>();

            await context.Database.MigrateAsync();

            if (seedReferenceData)
            {
                await seeder.SeedReferenceDataAsync();
            }

            if (seedDemoData)
            {
                // Also re-runs the two reference seeders, which is harmless: both are idempotent.
                await seeder.SeedAsync();
            }
        }

        /// <summary>
        /// Adds notification services to the service collection.
        /// This respects bounded context architecture by keeping ServiceCatalog
        /// notification implementations within the ServiceCatalog bounded context.
        /// </summary>
        public static IServiceCollection AddNotificationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // The one SMS gateway for the process (see SmsNotificationServiceExtensions). Idempotent:
            // UserManagement.Infrastructure calls it too, and only the first call registers.
            services.AddSmsNotificationService(configuration);

            // Template Engine & Services
            services.AddSingleton<ITemplateEngine, TemplateEngine>();
            services.AddScoped<INotificationTemplateService, NotificationTemplateService>();

            // Multi-Channel Notification Services
            services.AddScoped<IEmailNotificationService, SendGridEmailNotificationService>();
            services.AddScoped<IPushNotificationService, FirebasePushNotificationService>();
            services.AddScoped<IInAppNotificationService, InAppNotificationService>();

            // Reliable dispatch: one send path enforcing preferences, de-duplication and the delivery log,
            // plus the CAP subscriber that retries failed sends out of the durable outbox.
            services.AddSingleton(new NotificationDispatchOptions
            {
                ReliableDispatch = configuration.GetValue(NotificationDispatchOptions.ReliableDispatchKey, true)
            });
            services.AddScoped<INotificationDeliveryLog, Persistence.Notifications.NotificationDeliveryLog>();
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

            // The outbox: business code records an intent (INotificationRaiser) on its own unit of work, and
            // the sweep turns claimed rows into notifications for the dispatcher above. See FOLLOW-UPS #66
            // for why this does not go through domain event handlers.
            services.AddScoped<Persistence.Notifications.INotificationOutboxStore,
                Persistence.Notifications.NotificationOutboxStore>();
            services.AddScoped<AsanRezerve.ServiceCatalog.Application.Services.Notifications.INotificationRaiser,
                Notifications.NotificationRaiser>();
            services.AddSingleton<AsanRezerve.ServiceCatalog.Application.Services.Notifications.INotificationCopyWriter,
                AsanRezerve.ServiceCatalog.Application.Services.Notifications.PersianNotificationCopyWriter>();
            services.AddScoped<BackgroundJobs.ProcessNotificationOutboxJob>();
            services.AddHostedService<BackgroundJobs.NotificationOutboxService>();

            // The one notification nothing in the business causes: it is caused by the morning arriving, so
            // a job goes looking for it instead of a handler raising it.
            services.AddScoped<BackgroundJobs.DailyScheduleDigestJob>();
            services.AddHostedService<BackgroundJobs.DailyScheduleDigestService>();

            // Push: the registry is the address book, the gateway is the boundary with Firebase. The gateway
            // is a singleton because FirebaseApp is process-wide and refuses to be created twice.
            services.AddScoped<Persistence.Notifications.IDeviceTokenRegistry,
                Persistence.Notifications.DeviceTokenRegistry>();
            services.AddSingleton<Notifications.Push.IFirebaseMessagingGateway,
                Notifications.Push.FirebaseMessagingGateway>();

            // Recomputes a notification's tap target when the inbox is read, so history stays immutable
            // while links never go stale.
            services.AddScoped<AsanRezerve.ServiceCatalog.Application.Services.Notifications.INotificationDestinationResolver,
                Notifications.NotificationDestinationResolver>();
            services.AddTransient<EventHandlers.NotificationRetrySubscriber>();

            // HTTP Clients for notification services
            services.AddHttpClient<SendGridEmailNotificationService>();

            return services;
        }
    }
}
