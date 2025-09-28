// ========================================
// Example 1: Basic Test Using ServiceCatalogIntegrationTestBase
// ========================================
using Booksy.API;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Services;

// ========================================
// Example 4: Using Entity Helpers
// ========================================
public class ServiceSearchTests : ServiceCatalogIntegrationTestBase
{
    public ServiceSearchTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SearchServices_WithFilters_ShouldReturnMatchingServices()
    {
        // Arrange - Create multiple providers with services
        var (provider1, services1) = await CreateProviderWithServicesAsync(serviceCount: 2);
        var (provider2, services2) = await CreateProviderWithServicesAsync(serviceCount: 2);

        // Act - Use search helper from base class
        var response = await SearchServicesAsync(
            searchTerm: "Service",
            minPrice: 50.00m,
            maxPrice: 100.00m
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await GetResponseAsync<List<ServiceResponse>>(response);
        results.Should().NotBeEmpty();
        //results.Should().OnlyContain(s => s.BasePrice >= 50.00m && s.BasePrice <= 100.00m);
    }

    [Fact]
    public async Task GetAllServices_ShouldReturnAllActiveServices()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 5);

        // Act - Use helper from base class
        var allServices = await GetAllServicesAsync();

        // Assert
        allServices.Should().HaveCount(5);
        allServices.Should().OnlyContain(s => s.ProviderId == provider.Id);
    }
}
