// ========================================
// Booksy.ServiceCatalog.IntegrationTests/Infrastructure/ServiceCatalogIntegrationTestBase.cs
// ========================================
using Booksy.API;
using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.API;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.Tests.Common.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    protected ServiceCatalogIntegrationTestBase(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    // ================================================
    // DATABASE CLEANUP (Service Catalog Specific)
    // ================================================

    protected override async Task CleanDatabaseAsync()
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
    /// Find a Provider by ID
    /// </summary>
    protected async Task<Provider?> FindProviderAsync(Guid providerId)
    {
        return await DbContext.Providers
            .Include(p => p.BusinessHours)
            .Include(p => p.Holidays)
            .Include(p => p.Exceptions)
            .Include(p => p.Staff)
            .FirstOrDefaultAsync(p => p.Id == ProviderId.From(providerId));
    }

    /// <summary>
    /// Find a Provider by ID with predicate
    /// </summary>
    protected async Task<Provider?> FindProviderAsync(Expression<Func<Provider, bool>> predicate)
    {
        return await DbContext.Providers
            .FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Find a Service by ID
    /// </summary>
    protected async Task<Service?> FindServiceAsync(Guid serviceId)
    {
        return await DbContext.Services
            .AsNoTracking()
            .Include(s => s.Options)
            .Include(s => s.PriceTiers)
            .FirstOrDefaultAsync(s => s.Id == ServiceId.Create(serviceId));
    }

    /// <summary>
    /// Find a Service by predicate
    /// </summary>
    protected async Task<Service?> FindServiceAsync(Expression<Func<Service, bool>> predicate)
    {
        return await DbContext.Services
            .Include(s => s.Options)
            .Include(s => s.PriceTiers)
            .FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Get all services for a provider
    /// </summary>
    protected async Task<List<Service>> GetProviderServicesAsync(Guid providerId)
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
    protected async Task<List<Provider>> GetAllProvidersAsync()
    {
        return await DbContext.Providers.ToListAsync();
    }

    /// <summary>
    /// Get all services
    /// </summary>
    protected async Task<List<Service>> GetAllServicesAsync()
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
    protected new void AuthenticateAsProviderOwner(Provider provider)
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
    protected void AuthenticateAsServiceOwner(Service service)
    {
        base.AuthenticateAsProvider(
            service.ProviderId.Value.ToString(),
            $"provider_{service.ProviderId.Value}@test.com"
        );
    }

    /// <summary>
    /// Authenticate as an admin user with default credentials
    /// </summary>
    protected void AuthenticateAsTestAdmin()
    {
        base.AuthenticateAsAdmin("admin@booksy.com");
    }

    /// <summary>
    /// Expose base class ClearAuthentication for derived classes
    /// </summary>
    protected new void ClearAuthenticationHeader()
    {
        base.ClearAuthentication();
    }

    /// <summary>
    /// Authenticate as a user (customer)
    /// </summary>
    protected void AuthenticateAsUser(Guid userId, string email = "user@test.com")
    {
        base.AuthenticateAsCustomer(userId, email);
    }

    // ================================================
    // ASSERTION HELPERS (Service Catalog Specific)
    // ================================================

    /// <summary>
    /// Assert that a service exists in the database
    /// </summary>
    protected async Task AssertServiceExistsAsync(Guid serviceId)
    {
        var service = await FindServiceAsync(serviceId);
        service.Should().NotBeNull($"Service with ID {serviceId} should exist");
    }

    /// <summary>
    /// Assert that a service does not exist in the database
    /// </summary>
    protected async Task AssertServiceNotExistsAsync(Guid serviceId)
    {
        var service = await FindServiceAsync(serviceId);
        service.Should().BeNull($"Service with ID {serviceId} should not exist");
    }

    /// <summary>
    /// Assert that a provider exists in the database
    /// </summary>
    protected async Task AssertProviderExistsAsync(Guid providerId)
    {
        var provider = await FindProviderAsync(providerId);
        provider.Should().NotBeNull($"Provider with ID {providerId} should exist");
    }

    /// <summary>
    /// Assert that a provider has a specific number of services
    /// </summary>
    protected async Task AssertProviderServiceCountAsync(Guid providerId, int expectedCount)
    {
        var services = await GetProviderServicesAsync(providerId);
        services.Should().HaveCount(expectedCount,
            $"Provider {providerId} should have {expectedCount} services");
    }

    /// <summary>
    /// Assert that a service has specific status
    /// </summary>
    protected async Task AssertServiceStatusAsync(Guid serviceId, Domain.Enums.ServiceStatus expectedStatus)
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
    protected async Task<Provider> CreateAndAuthenticateAsProviderAsync(
        string businessName = "Test Provider",
        string email = "provider@test.com")
    {
        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            businessName,
            $"Description for {businessName}",
            Domain.Enums.ProviderType.Individual,
            ContactInfo.Create(
                Email.Create(email),
                PhoneNumber.Create("+1234567890")
            ),
            BusinessAddress.Create(
                "123 Test St",
                "Test City",
                "TS",
                "12345",
                "USA"
            )
        );
        provider.AddStaff("John", "Doe", Domain.Enums.StaffRole.ServiceProvider, PhoneNumber.Create("09123131311"));
        provider.SetSatus(Domain.Enums.ProviderStatus.Active);
        provider.SetAllowOnlineBooking(true);

        await CreateEntityAsync(provider);
        AuthenticateAsProviderOwner(provider);

        return provider;
    }

    /// <summary>
    /// Create a service for a provider
    /// </summary>
    protected async Task<Service> CreateServiceForProviderAsync(
        Provider provider,
        string serviceName = "Test Service",
        decimal price = 50.00m,
        int durationMinutes = 60)
    {
        var service = Service.Create(
            provider.Id,
            serviceName,
            $"Description for {serviceName}",
            ServiceCategory.Create("Beauty"),
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
    protected async Task<(Provider Provider, List<Service> Services)> CreateProviderWithServicesAsync(
        int serviceCount = 3)
    {
        var provider = Provider.RegisterProvider(
            UserId.From(Guid.NewGuid()),
            "Test Provider",
            "Test provider description",
            Domain.Enums.ProviderType.Individual,
            ContactInfo.Create(
                Email.Create("provider@test.com"),
                PhoneNumber.Create("+1234567890")
            ),
            BusinessAddress.Create(
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

    // ================================================
    // VALUE OBJECT HELPERS
    // ================================================

    /// <summary>
    /// Create a test ProviderId
    /// </summary>
    protected ProviderId CreateProviderId(Guid? id = null)
    {
        return ProviderId.From(id ?? Guid.NewGuid());
    }

    /// <summary>
    /// Create a test ServiceId
    /// </summary>
    protected ServiceId CreateServiceId(Guid? id = null)
    {
        return ServiceId.Create(id ?? Guid.NewGuid());
    }

    /// <summary>
    /// Create a test Email
    /// </summary>
    protected Email CreateEmail(string email = "test@example.com")
    {
        return Email.Create(email);
    }

    /// <summary>
    /// Create a test Price
    /// </summary>
    protected Price CreatePrice(decimal amount = 50.00m, string currency = "USD")
    {
        return Price.Create(amount, currency);
    }

    /// <summary>
    /// Create a test Duration
    /// </summary>
    protected Duration CreateDuration(int minutes = 60)
    {
        return Duration.FromMinutes(minutes);
    }

    // ================================================
    // API ENDPOINT HELPERS
    // ================================================

    /// <summary>
    /// Get service by ID endpoint
    /// </summary>
    protected async Task<HttpResponseMessage> GetServiceByIdAsync(Guid serviceId)
    {
        return await GetAsync($"/api/v1/services/{serviceId}");
    }

    /// <summary>
    /// Get all services for a provider endpoint
    /// </summary>
    protected async Task<HttpResponseMessage> GetProviderServicesAsyncApi(Guid providerId)
    {
        return await GetAsync($"/api/v1/providers/{providerId}/services");
    }

    /// <summary>
    /// Get provider by ID endpoint
    /// </summary>
    protected async Task<HttpResponseMessage> GetProviderByIdAsync(Guid providerId)
    {
        return await GetAsync($"/api/v1/providers/{providerId}");
    }

    /// <summary>
    /// Delete service endpoint
    /// </summary>
    protected async Task<Core.Domain.Infrastructure.Middleware.ApiResponse> DeleteServiceAsync(Guid serviceId)
    {
        return await DeleteAsync($"/api/v1/services/{serviceId}");
    }

    /// <summary>
    /// Search services endpoint
    /// </summary>
    protected async Task<HttpResponseMessage> SearchServicesAsync(
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
    protected async Task<Provider> CreateProviderWithStatusAsync(
        Guid userId,
        string businessName,
        Domain.Enums.ProviderStatus status)
    {
        var provider = Provider.RegisterProvider(
            UserId.From(userId),
            businessName,
            $"Description for {businessName}",
            Domain.Enums.ProviderType.Individual,
            ContactInfo.Create(
                Email.Create($"{userId}@test.com"),
                PhoneNumber.Create("+1234567890")
            ),
            BusinessAddress.Create(
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

    /// <summary>
    /// Create a provider and authenticate as them with specific user ID
    /// </summary>
    protected async Task<Provider> CreateAndAuthenticateAsProviderAsync(
        string businessName,
        string email,
        Guid userId)
    {
        AuthenticateAsUser(userId, email);

        var provider = Provider.RegisterProvider(
            UserId.From(userId),
            businessName,
            $"Description for {businessName}",
            Domain.Enums.ProviderType.Individual,
            ContactInfo.Create(
                Email.Create(email),
                PhoneNumber.Create("+1234567890")
            ),
            BusinessAddress.Create(
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
}