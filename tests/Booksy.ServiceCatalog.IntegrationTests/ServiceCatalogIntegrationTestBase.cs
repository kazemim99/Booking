using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.Tests.Common.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for Service Catalog integration tests
/// Inherits from generic IntegrationTestBase and provides Service Catalog-specific helpers
/// </summary>
public abstract class ServiceCatalogIntegrationTestBase
    : IntegrationTestBase<
        ServiceCatalogTestWebApplicationFactory<Startup>, // Concrete factory
        ServiceCatalogDbContext,                  // Concrete DbContext
        Startup>                                  // Concrete Startup class
{
    public ServiceCatalogIntegrationTestBase(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    // ================================================
    // DATABASE CLEANUP (Service Catalog Specific)
    // ================================================

    public override async Task CleanDatabaseAsync()
    {
        // Clean tables in correct order (respecting foreign keys)
        //await DbContext.Database.ExecuteSqlRawAsync(@"
        //    DELETE FROM ""ServiceOptions"";
        //    DELETE FROM ""ServicePriceTiers"";
        //    DELETE FROM ""Services"";
        //    DELETE FROM ""Providers"";
        //");

        await base.CleanDatabaseAsync();
    }

    // ================================================
    // SERVICE CATALOG SPECIFIC ENTITY HELPERS
    // ================================================

    /// <summary>
    /// Creates a real user in the UserManagement schema and authenticates as them, returning the id.
    /// </summary>
    /// <remarks>
    /// Needed because the Host composes both bounded contexts, so a ServiceCatalog flow can legitimately
    /// call into UserManagement. Provider registration does exactly that: after saving the provider it
    /// mints a token carrying the new provider claims, which requires the owner to exist in
    /// <c>user_management.users</c>.
    ///
    /// <para>Authenticating as a bare <c>Guid.NewGuid()</c> fabricates an identity with no backing user
    /// row. That was invisible while the suite booted the ServiceCatalog-only host — nothing could reach
    /// UserManagement — and surfaced as "User with ID ... not found" once the suite was retargeted at the
    /// real Host. It is a gap in the test fixture, not a production defect: a real provider has always
    /// signed up before registering a business, so the user exists.</para>
    /// </remarks>
    public async Task<Guid> CreateAndAuthenticateAsRealUserAsync(
        string email = "provider-owner@test.com",
        UserManagement.Domain.Enums.UserType type = UserManagement.Domain.Enums.UserType.Provider)
    {
        var userDbContext = Scope.ServiceProvider
            .GetRequiredService<UserManagement.Infrastructure.Persistence.Context.UserManagementDbContext>();

        // The profile-carrying Register overload, deliberately: GetUserByIdQueryHandler dereferences
        // user.Profile unconditionally, so a user created through the profile-less overload NREs there.
        // (That asymmetry is a real robustness gap in production code — recorded in FOLLOW-UPS — but a
        // test fixture should create the complete user a real sign-up produces, not the degenerate one.)
        var user = UserManagement.Domain.Aggregates.User.Register(
            Core.Domain.ValueObjects.Email.Create(email),
            UserManagement.Domain.ValueObjects.HashedPassword.FromHash("$2a$11$integrationtestplaceholderhash"),
            UserManagement.Domain.Entities.UserProfile.Create("Test", "Owner"),
            type);

        userDbContext.Add(user);
        await userDbContext.SaveChangesAsync();

        AuthenticateAsUser(user.Id.Value, email);
        return user.Id.Value;
    }

    /// <summary>
    /// Find a Provider by ID, reading the database's current state.
    /// </summary>
    /// <remarks>
    /// The change tracker is cleared first. This helper exists to assert on what an HTTP call actually
    /// persisted, and that write happens in the request's own DI scope and <c>DbContext</c> — not this
    /// one. Without clearing, EF's identity map returns the instance this context loaded earlier,
    /// complete with its pre-request field values, so an assertion can pass or fail on stale data with
    /// no relation to what is in the database.
    ///
    /// <para>Concretely: <c>SetPrimaryGalleryImage_UpdatesBusinessProfileTimestamp</c> read a
    /// <c>Profile.LastUpdatedAt</c> byte-identical to the one captured before the request and reported a
    /// missing timestamp update, while the domain had updated and persisted it correctly all along.
    /// Callers only assert on the result, so returning fresh state is always what they want.</para>
    /// </remarks>
    public async Task<Provider?> FindProviderAsync(Guid providerId)
    {
        DbContext.ChangeTracker.Clear();

        return await DbContext.Providers
            .Include(p => p.BusinessHours)
            .Include(p => p.Holidays)
            .Include(p => p.Exceptions)
            .Include(c=>c.Profile).ThenInclude(c=>c.GalleryImages)
            .FirstOrDefaultAsync(p => p.Id == ProviderId.From(providerId));
    }

    /// <summary>
    /// Find a Provider by ID with predicate
    /// </summary>
    public async Task<Provider?> FindProviderAsync(Expression<Func<Provider, bool>> predicate)
    {
        return await DbContext.Providers
            .FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Find a Service by ID
    /// </summary>
    public async Task<Service?> FindServiceAsync(Guid serviceId)
    {
        return await DbContext.Services
            .AsNoTracking()
            .Include(s => s.Options)
            .Include(s => s.PriceTiers)
            .FirstOrDefaultAsync(s => s.Id == ServiceId.From(serviceId));
    }

    /// <summary>
    /// Find a Service by predicate
    /// </summary>
    public async Task<Service?> FindServiceAsync(Expression<Func<Service, bool>> predicate)
    {
        return await DbContext.Services
            .Include(s => s.Options)
            .Include(s => s.PriceTiers)
            .FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Get all services for a provider
    /// </summary>
    public async Task<List<Service>> GetProviderServicesAsync(Guid providerId)
    {
        return await DbContext.Services
            .Where(s => s.ProviderId == ProviderId.From(providerId))
            .Include(s => s.Options)
            .Include(s => s.PriceTiers)
            .ToListAsync();
    }

    /// <summary>
    /// Get all providers
    /// </summary>
    public async Task<List<Provider>> GetAllProvidersAsync()
    {
        return await DbContext.Providers.ToListAsync();
    }

    /// <summary>
    /// Get all services
    /// </summary>
    public async Task<List<Service>> GetAllServicesAsync()
    {
        return await DbContext.Services
            .Include(s => s.Options)
            .Include(s => s.PriceTiers)
            .ToListAsync();
    }

    // ================================================
    // AUTHENTICATION HELPERS (Service Catalog Specific)
    // ================================================

    /// <summary>
    /// Authenticate as the owner of a specific provider
    /// </summary>
    public new void AuthenticateAsProviderOwner(Provider provider)
    {
        // Create a custom test user where userId matches the provider's OwnerId
        var testUser = new TestUser
        {
            UserId = provider.OwnerId.Value.ToString(),
            Email = provider.ContactInfo.Email.Value,
            Name = provider.Profile.BusinessName,
            Role = "Provider",
            AdditionalClaims = new Dictionary<string, string>
            {
                { "providerId", provider.Id.Value.ToString() },
                { "user_type", "Provider" }
            }
        };

        _userContext.SetUser(testUser);
    }

    /// <summary>
    /// Authenticate as the provider who owns a service
    /// </summary>
    public void AuthenticateAsServiceOwner(Service service)
    {
        base.AuthenticateAsProvider(
            service.ProviderId.Value.ToString(),
            $"provider_{service.ProviderId.Value}@test.com"
        );
    }

    /// <summary>
    /// Authenticate as an admin user with default credentials
    /// </summary>
    public void AuthenticateAsTestAdmin()
    {
        base.AuthenticateAsAdmin("admin@booksy.com");
    }

    /// <summary>
    /// Expose base class ClearAuthentication for derived classes
    /// </summary>
    public new void ClearAuthenticationHeader()
    {
        base.ClearAuthentication();
    }

    /// <summary>
    /// Authenticate as a user (customer)
    /// </summary>
    public void AuthenticateAsUser(Guid userId, string email = "user@test.com")
    {
        base.AuthenticateAsCustomer(userId, email);
    }

    // ================================================
    // ASSERTION HELPERS (Service Catalog Specific)
    // ================================================

    /// <summary>
    /// Assert that a service exists in the database
    /// </summary>
    public async Task AssertServiceExistsAsync(Guid serviceId)
    {
        var service = await FindServiceAsync(serviceId);
        service.Should().NotBeNull($"Service with ID {serviceId} should exist");
    }

    /// <summary>
    /// Assert that a service does not exist in the database
    /// </summary>
    public async Task AssertServiceNotExistsAsync(Guid serviceId)
    {
        var service = await FindServiceAsync(serviceId);
        service.Should().BeNull($"Service with ID {serviceId} should not exist");
    }

    /// <summary>
    /// Assert that a provider exists in the database
    /// </summary>
    public async Task AssertProviderExistsAsync(Guid providerId)
    {
        var provider = await FindProviderAsync(providerId);
        provider.Should().NotBeNull($"Provider with ID {providerId} should exist");
    }

    /// <summary>
    /// Assert that a provider has a specific number of services
    /// </summary>
    public async Task AssertProviderServiceCountAsync(Guid providerId, int expectedCount)
    {
        var services = await GetProviderServicesAsync(providerId);
        services.Should().HaveCount(expectedCount,
            $"Provider {providerId} should have {expectedCount} services");
    }

    /// <summary>
    /// Assert that a service has specific status
    /// </summary>
    public async Task AssertServiceStatusAsync(Guid serviceId, Domain.Enums.ServiceStatus expectedStatus)
    {
        var service = await FindServiceAsync(serviceId);
        service.Should().NotBeNull();
        service!.Status.Should().Be(expectedStatus,
            $"Service {serviceId} should have status {expectedStatus}");
    }

    // ================================================
    // SETUP HELPERS (Service Catalog Specific)
    // ================================================

    /// <summary>
    /// Create a provider and authenticate as them in one call
    /// </summary>
    public async Task<Provider> CreateAndAuthenticateAsProviderAsync(
        string businessName = "Test Provider",
        string email = "provider@test.com")
    {
        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            businessName,
            $"Description for {businessName}",
            Domain.Enums.ServiceCategory.Barbershop,
            ContactInfo.Create(
                Email.Create(email),
                PhoneNumber.From("+1234567890")
            ),
            BusinessAddress.Create(
                "123 Test St",
                "123 Test St",
                "Test City",
                "TS",
                "12345",
                "USA"
            )
        );
        provider.SetSatus(Domain.Enums.ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);

        await CreateEntityAsync(provider);
        AuthenticateAsProviderOwner(provider);

        return provider;
    }

    /// <summary>
    /// Create a service for a provider
    /// </summary>
    public async Task<Service> CreateServiceForProviderAsync(
        Provider provider,
        string serviceName = "Test Service",
        decimal price = 50.00m,
        int durationMinutes = 60)
    {
        var service = Service.Create(
            provider.Id,
            serviceName,
            $"Description for {serviceName}",
            ServiceCategory.HairSalon,
            Domain.Enums.ServiceType.Standard,
            Price.Create(price, "USD"),
            Duration.FromMinutes(durationMinutes)
        );

        await CreateEntityAsync(service);
        return service;
    }

    /// <summary>
    /// Create a provider with services
    /// </summary>
    public async Task<(Provider Provider, List<Service> Services)> CreateProviderWithServicesAsync(
        int serviceCount = 3)
    {
        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "Test Provider",
            "Test provider description",
            Domain.Enums.ServiceCategory.Barbershop,
            ContactInfo.Create(
                Email.Create("provider@test.com"),
                PhoneNumber.From("+1234567890")
            ),
            BusinessAddress.Create(
                "123 Test St",
                "123 Test St",
                "Test City",
                "TS",
                "12345",
                "USA"
            )
        );

        provider.SetSatus(Domain.Enums.ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);
        await CreateEntityAsync(provider);

        var services = new List<Service>();
        for (int i = 0; i < serviceCount; i++)
        {
            var service = await CreateServiceForProviderAsync(
                provider,
                $"Service {i + 1}",
                50.00m + (i * 10),
                60 + (i * 15)
            );
            services.Add(service);
        }

        return (provider, services);
    }

    /// <summary>
    /// Create a test provider with services and staff (commonly used in tests)
    /// </summary>
    public async Task<Provider> CreateTestProviderWithServicesAsync(int serviceCount = 3)
    {
        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "Test Provider",
            "Test provider description",
            Domain.Enums.ServiceCategory.Barbershop,
            ContactInfo.Create(
                Email.Create("provider@test.com"),
                PhoneNumber.From("+1234567890")
            ),
            BusinessAddress.Create(
                "123 Test St",
                "123 Test St",
                "Test City",
                "TS",
                "12345",
                "USA"
            )
        );

        provider.SetSatus(Domain.Enums.ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);
        await CreateEntityAsync(provider);

        // Create services
        for (int i = 0; i < serviceCount; i++)
        {
            var service = await CreateServiceForProviderAsync(
                provider,
                $"Service {i + 1}",
                50.00m + (i * 10),
                60 + (i * 15)
            );
        }

        return provider;
    }

    /// <summary>
    /// Get the first service for a provider
    /// </summary>
    public async Task<Service> GetFirstServiceForProviderAsync(Guid providerId)
    {
        var services = await GetProviderServicesAsync(providerId);
        return services.First();
    }

    // ================================================
    // VALUE OBJECT HELPERS
    // ================================================

    /// <summary>
    /// Create a test ProviderId
    /// </summary>
    public ProviderId CreateProviderId(Guid? id = null)
    {
        return ProviderId.From(id ?? Guid.NewGuid());
    }

    /// <summary>
    /// Create a test ServiceId
    /// </summary>
    public ServiceId CreateServiceId(Guid? id = null)
    {
        return ServiceId.From(id ?? Guid.NewGuid());
    }

    /// <summary>
    /// Create a test Email
    /// </summary>
    public Email CreateEmail(string email = "test@example.com")
    {
        return Email.Create(email);
    }

    /// <summary>
    /// Create a test Price
    /// </summary>
    public Price CreatePrice(decimal amount = 50.00m, string currency = "USD")
    {
        return Price.Create(amount, currency);
    }

    /// <summary>
    /// Create a test Duration
    /// </summary>
    public Duration CreateDuration(int minutes = 60)
    {
        return Duration.FromMinutes(minutes);
    }

    // ================================================
    // API ENDPOINT HELPERS
    // ================================================

    /// <summary>
    /// Get service by ID endpoint
    /// </summary>
    public async Task<HttpResponseMessage> GetServiceByIdAsync(Guid serviceId)
    {
        return await GetAsync($"/api/v1/services/{serviceId}");
    }

    /// <summary>
    /// Get all services for a provider endpoint
    /// </summary>
    public async Task<HttpResponseMessage> GetProviderServicesAsyncApi(Guid providerId)
    {
        return await GetAsync($"/api/v1/providers/{providerId}/services");
    }

    /// <summary>
    /// Get provider by ID endpoint
    /// </summary>
    public async Task<HttpResponseMessage> GetProviderByIdAsync(Guid providerId)
    {
        return await GetAsync($"/api/v1/providers/{providerId}");
    }

    /// <summary>
    /// Delete service endpoint
    /// </summary>
    public async Task<Core.Domain.Infrastructure.Middleware.ApiResponse> DeleteServiceAsync(Guid serviceId)
    {
        return await DeleteAsync($"/api/v1/services/{serviceId}");
    }

    /// <summary>
    /// Search services endpoint
    /// </summary>
    public async Task<HttpResponseMessage> SearchServicesAsync(
        string searchTerm,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
    {
        var queryParams = new List<string> { $"searchTerm={searchTerm}" };

        if (category != null)
            queryParams.Add($"category={category}");

        if (minPrice.HasValue)
            queryParams.Add($"minPrice={minPrice}");

        if (maxPrice.HasValue)
            queryParams.Add($"maxPrice={maxPrice}");

        var query = string.Join("&", queryParams);
        return await GetAsync($"/api/v1/services/search?{query}");
    }

    // ================================================
    // PROVIDER STATUS HELPERS (for GetCurrentProviderStatus tests)
    // ================================================

    /// <summary>
    /// Create a provider with a specific status and user ID
    /// </summary>
    public async Task<Provider> CreateProviderWithStatusAsync(
        Guid userId,
        string businessName,
        Domain.Enums.ProviderStatus status)
    {
        var provider = Provider.RegisterProvider(
            UserId.From(userId),
            businessName,
            $"Description for {businessName}",
            Domain.Enums.ServiceCategory.Barbershop,
            ContactInfo.Create(
                Email.Create($"{userId}@test.com"),
                PhoneNumber.From("+1234567890")
            ),
            BusinessAddress.Create(
                "123 Test St",
                "123 Test St",
                "123 Test St",
                "123 Test St",
                "Test City",
                "TS"
            )
        );

        provider.SetSatus(status);
        provider.SetAllowOnlineBooking(true);

        await CreateEntityAsync(provider);

        return provider;
    }


    public async Task<Provider> CreateTestProviderWithServicesAsync()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "provider@test.com");

        // Add business hours (Monday-Friday, 9 AM - 5 PM)
        provider.SetBusinessHours(new Dictionary<Domain.Enums.DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
        {
            { Domain.Enums.DayOfWeek.Monday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Tuesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Wednesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Thursday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Friday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) }
        });

        

        await DbContext.SaveChangesAsync();

        // Create a service
        await CreateServiceForProviderAsync(provider, "Test Service", 50.00m, 60);

        return provider;
    }


    /// <summary>
    /// Create a provider and authenticate as them with specific user ID
    /// </summary>
    public async Task<Provider> CreateAndAuthenticateAsProviderAsync(
        string businessName,
        string email,
        Guid userId)
    {
        AuthenticateAsUser(userId, email);

        var provider = Provider.RegisterProvider(
            UserId.From(userId),
            businessName,
            $"Description for {businessName}",
            Domain.Enums.ServiceCategory.Barbershop,
            ContactInfo.Create(
                Email.Create(email),
                PhoneNumber.From("+1234567890")
            ),
            BusinessAddress.Create(
                "123 Test St",
                "123 Test St",
                "Test City",
                "TS",
                "12345",
                "USA"
            )
        );

        provider.SetSatus(Domain.Enums.ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);

        await CreateEntityAsync(provider);

        return provider;
    }

    // ================================================
    // NOTIFICATION HELPERS
    // ================================================

    /// <summary>
    /// Find a notification by ID
    /// </summary>
    public async Task<Domain.Aggregates.NotificationAggregate.Notification?> FindNotificationAsync(Guid notificationId)
    {
        return await DbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == Domain.ValueObjects.NotificationId.From(notificationId));
    }

    /// <summary>
    /// Get all notifications for a user
    /// </summary>
    public async Task<List<Domain.Aggregates.NotificationAggregate.Notification>> GetUserNotificationsAsync(Guid userId)
    {
        return await DbContext.Notifications
            .Where(n => n.RecipientId == UserId.From(userId))
            .ToListAsync();
    }

    /// <summary>
    /// Create a test notification for a user
    /// </summary>
    public async Task<Domain.Aggregates.NotificationAggregate.Notification> CreateTestNotificationAsync(
        Guid userId,
        NotificationType type = NotificationType.BookingConfirmation,
        NotificationChannel channel = NotificationChannel.Email,
        string subject = "Test Notification",
        string body = "Test notification body",
        string? recipientEmail = "test@test.com",
        string? recipientPhone = null)
    {
        var notification = Domain.Aggregates.NotificationAggregate.Notification.CreateImmediate(
            UserId.From(userId),
            type,
            channel,
            subject,
            body,
            NotificationPriority.Normal,
            recipientEmail: recipientEmail,
            recipientPhone: recipientPhone);

        await CreateEntityAsync(notification);
        return notification;
    }

    /// <summary>
    /// Create a scheduled test notification
    /// </summary>
    public async Task<Domain.Aggregates.NotificationAggregate.Notification> CreateScheduledTestNotificationAsync(
        Guid userId,
        DateTime scheduledFor,
        NotificationType type = NotificationType.BookingReminder,
        NotificationChannel channel = NotificationChannel.Email,
        string subject = "Scheduled Notification",
        string body = "Scheduled notification body",
        string? recipientEmail = "test@test.com")
    {
        var notification = Domain.Aggregates.NotificationAggregate.Notification.Schedule(
            UserId.From(userId),
            type,
            channel,
            subject,
            body,
            scheduledFor,
            NotificationPriority.Normal,
            recipientEmail: recipientEmail);

        await CreateEntityAsync(notification);
        return notification;
    }

    /// <summary>
    /// Assert that a notification exists in the database
    /// </summary>
    public async Task AssertNotificationExistsAsync(Guid notificationId)
    {
        var notification = await FindNotificationAsync(notificationId);
        notification.Should().NotBeNull($"Notification with ID {notificationId} should exist");
    }

    /// <summary>
    /// Assert that a notification has a specific status
    /// </summary>
    public async Task AssertNotificationStatusAsync(Guid notificationId, NotificationStatus expectedStatus)
    {
        var notification = await FindNotificationAsync(notificationId);
        notification.Should().NotBeNull();
        notification!.Status.Should().Be(expectedStatus,
            $"Notification {notificationId} should have status {expectedStatus}");
    }

    /// <summary>
    /// Assert that a user has a specific number of notifications
    /// </summary>
    public async Task AssertUserNotificationCountAsync(Guid userId, int expectedCount)
    {
        var notifications = await GetUserNotificationsAsync(userId);
        notifications.Should().HaveCount(expectedCount,
            $"User {userId} should have {expectedCount} notifications");
    }
}