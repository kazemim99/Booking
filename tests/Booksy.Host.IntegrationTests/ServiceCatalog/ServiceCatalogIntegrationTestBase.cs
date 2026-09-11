using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Caching;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.Host.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
// This file's bare "Domain.Enums.X" / "Domain.Aggregates.X" references used to resolve through
// C#'s enclosing-namespace lookup: the old namespace (Booksy.ServiceCatalog.IntegrationTests.*)
// had Booksy.ServiceCatalog as an ENCLOSING namespace, which has a nested Domain namespace of its
// own. Moving this file's namespace to Booksy.Host.IntegrationTests.ServiceCatalog (slice 4) broke
// that implicit path; this alias restores every existing "Domain.Xyz" reference in the file below
// without touching the ~900 lines that use it.
using Domain = Booksy.ServiceCatalog.Domain;

namespace Booksy.Host.IntegrationTests.ServiceCatalog;

/// <summary>
/// Base class for Service Catalog integration tests
/// Inherits from generic IntegrationTestBase and provides Service Catalog-specific helpers
/// </summary>
public abstract class ServiceCatalogIntegrationTestBase
    : IntegrationTestBase<ServiceCatalogDbContext>
{
    public ServiceCatalogIntegrationTestBase(BooksyHostFactory factory)
        : base(factory)
    {
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
        global::Booksy.UserManagement.Domain.Enums.UserType type = global::Booksy.UserManagement.Domain.Enums.UserType.Provider)
    {
        // Bare "UserManagement.X" below would resolve to THIS project's own
        // Booksy.Host.IntegrationTests.UserManagement namespace (the base class lives there) rather
        // than the bounded context — enclosing-namespace lookup finds that nested "UserManagement"
        // before it ever considers a using-alias, so full qualification is the only reliable fix
        // (see the Domain alias above, which has no such collision and works fine unqualified).
        var userDbContext = Scope.ServiceProvider
            .GetRequiredService<global::Booksy.UserManagement.Infrastructure.Persistence.Context.UserManagementDbContext>();

        // The profile-carrying Register overload, deliberately: GetUserByIdQueryHandler dereferences
        // user.Profile unconditionally, so a user created through the profile-less overload NREs there.
        // (That asymmetry is a real robustness gap in production code — recorded in FOLLOW-UPS — but a
        // test fixture should create the complete user a real sign-up produces, not the degenerate one.)
        var user = global::Booksy.UserManagement.Domain.Aggregates.User.Register(
            Core.Domain.ValueObjects.Email.Create(email),
            global::Booksy.UserManagement.Domain.ValueObjects.HashedPassword.FromHash("$2a$11$integrationtestplaceholderhash"),
            global::Booksy.UserManagement.Domain.Entities.UserProfile.Create("Test", "Owner"),
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

        // This overload never set business hours at all, so its salon was closed every day
        // of the week. Open all seven, exactly as the parameterless overload does.
        var hours = new Dictionary<Domain.Enums.DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();
        foreach (Domain.Enums.DayOfWeek day in Enum.GetValues<Domain.Enums.DayOfWeek>())
            hours[day] = (new TimeOnly(9, 0), new TimeOnly(17, 0));
        provider.SetBusinessHours(hours);

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

        // Same reason as the parameterless overload: a Provider plus Services is not a
        // bookable salon under the membership model. Both overloads must produce the same
        // shape, or which one a caller happens to bind to silently changes the outcome.
        await MakeBookableAsync(provider);

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
                "Test City",
                "TS",
                "12345",
                "USA"
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

        // Open every day, so a test that picks "three days from now" is never accidentally
        // landing on a closed day. This used to be Monday-Friday, which made the fixture
        // silently date-dependent.
        var hours = new Dictionary<Domain.Enums.DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();
        foreach (Domain.Enums.DayOfWeek day in Enum.GetValues<Domain.Enums.DayOfWeek>())
            hours[day] = (new TimeOnly(9, 0), new TimeOnly(17, 0));
        provider.SetBusinessHours(hours);

        await DbContext.SaveChangesAsync();

        // Create a service
        await CreateServiceForProviderAsync(provider, "Test Service", 50.00m, 60);

        // ...and make the salon actually bookable. See MakeBookableAsync: business hours and
        // a service are no longer enough on their own.
        await MakeBookableAsync(provider);

        return provider;
    }

    /// <summary>
    /// Gives a test salon a service-providing OWNER MEMBERSHIP and, through it, qualified
    /// services and availability — i.e. everything the booking engine needs beyond a
    /// Provider row.
    /// </summary>
    /// <remarks>
    /// Under the membership model a salon is not bookable just because it has business hours
    /// and services. Availability is generated per member (<c>ProviderAvailability.StaffId =
    /// MembershipId</c>) and a service stays in <c>Draft</c> until at least one member is
    /// qualified for it, so a fixture that creates only a Provider + Services produces a salon
    /// nobody works at: no slots, no bookable service, and every availability/booking test
    /// failing for a reason that has nothing to do with what it is testing.
    ///
    /// <para>This deliberately runs the PRODUCTION path (<see cref="IMemberBookabilityService"/>)
    /// rather than hand-inserting membership, qualification and slot rows. Hand-rolled fixture
    /// data is exactly how this fixture drifted away from the domain in the first place; going
    /// through the real service means the fixture cannot claim a salon is bookable in a way the
    /// application would not.</para>
    /// </remarks>
    /// <returns>The owner's membership — the id that availability and bookings use as StaffId.</returns>
    public async Task<OrganizationMembership> MakeBookableAsync(Provider provider)
    {
        var membership = OrganizationMembership.CreateOwner(
            provider.OwnerId, provider.Id, providesServices: true);

        await CreateEntityAsync(membership);

        // Qualify + activate the salon's services for this member, service by service, rather
        // than calling SyncAsync.
        //
        // SyncAsync would ALSO generate a rolling 30 days of ProviderAvailability rows —
        // roughly 480 inserts per fixture call. Availability is computed live from business
        // hours, members and existing bookings (see AvailabilityService), so those rows buy
        // these tests nothing; what they do buy is heavy write contention on the one
        // Testcontainers database that every test class shares in parallel, which is how a
        // fixture change in one class started reddening unrelated classes.
        var bookability = Scope.ServiceProvider.GetRequiredService<IMemberBookabilityService>();
        var services = Scope.ServiceProvider.GetRequiredService<IServiceWriteRepository>();
        foreach (var service in await services.GetServicesByProviderIdAsync(provider.Id))
        {
            await bookability.SyncServiceAsync(service);
        }

        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // IProviderReadRepository is decorated by CachedProviderReadRepository, and SyncAsync
        // READ the provider before it activated the services — so the cache now holds a
        // provider whose Services are still Draft. In production that entry is invalidated by
        // ProviderCacheInvalidationEventHandler, but this fixture commits through DbContext
        // directly rather than the unit of work that dispatches domain events, so nothing
        // invalidates it here. Without this the salon is bookable in the database and NOT
        // bookable over HTTP, which is precisely the split that made these failures so hard
        // to read: the fixture's own assertions pass while every test through the API fails.
        var cache = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.RemoveAsync($"Provider:{provider.Id.Value}");
        await cache.RemoveAsync($"Provider:owner:{provider.OwnerId.Value}");

        await AssertSalonIsBookableAsync(provider);

        return membership;
    }

    /// <summary>
    /// The id a booking for this salon must carry as its StaffId: its service-providing
    /// member's MembershipId.
    /// </summary>
    /// <remarks>
    /// Availability is resolved and booking conflicts are checked PER BOOKABLE RESOURCE
    /// (<c>GetStaffBookingsInDateRangeAsync(resource.Id, ...)</c>), and a salon with members
    /// resolves to those members — never to itself. A test that holds its booking against
    /// <c>provider.Id</c> therefore creates a booking no member can see, and the slot it was
    /// meant to occupy still reads as free. Booking the member is also what production does:
    /// the organization is only its own bookable resource when no member can serve.
    /// </remarks>
    public async Task<Guid> GetBookableMemberIdAsync(Provider provider)
    {
        var member = await DbContext.Set<OrganizationMembership>()
            .Where(m => m.OrganizationId == provider.Id)
            .ToListAsync();

        var bookable = member.FirstOrDefault(m => m.ProvidesServices);
        if (bookable is null)
            throw new InvalidOperationException(
                $"Fixture: provider {provider.Id.Value} has no service-providing member " +
                $"({member.Count} membership(s)). Build it with CreateTestProviderWithServicesAsync.");

        return bookable.Id;
    }

    /// <summary>
    /// Verifies the arrange step actually produced a bookable salon, reading back through the
    /// SAME repositories an HTTP request uses (a fresh scope, the cached provider decorator,
    /// the service read repository) rather than through the test's own DbContext.
    /// </summary>
    /// <remarks>
    /// Asserting via the test's DbContext is not good enough: it can see tracked or
    /// uncommitted state that a real request never would, which is exactly how a salon ends
    /// up bookable in the fixture's opinion and closed in the API's. Failing here, loudly, in
    /// arrange beats fifteen tests failing in assert with "no slots".
    /// </remarks>
    private async Task AssertSalonIsBookableAsync(Provider provider)
    {
        using var scope = Factory.Services.CreateScope();
        var providers = scope.ServiceProvider.GetRequiredService<IProviderReadRepository>();
        var services = scope.ServiceProvider.GetRequiredService<IServiceReadRepository>();

        var reloaded = await providers.GetByIdAsync(provider.Id)
            ?? throw new InvalidOperationException(
                $"Fixture: provider {provider.Id.Value} is not readable through IProviderReadRepository.");

        var openDays = reloaded.BusinessHours.Count(h => h.IsOpen);
        if (openDays == 0)
            throw new InvalidOperationException(
                $"Fixture: provider {provider.Id.Value} has no OPEN business hours as seen by the " +
                $"read repository ({reloaded.BusinessHours.Count} rows). Availability will be empty.");

        var providerServices = await services.GetByProviderIdAsync(provider.Id);
        var bookable = providerServices.Where(s => s.CanBeBooked()).ToList();
        if (bookable.Count == 0)
            throw new InvalidOperationException(
                $"Fixture: provider {provider.Id.Value} has {providerServices.Count} service(s) but " +
                $"none that CanBeBooked (statuses: " +
                $"{string.Join(", ", providerServices.Select(s => $"{s.Status}/{s.QualifiedStaff.Count} staff"))}). " +
                "MemberBookabilityService.SyncAsync should have qualified and activated them.");
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