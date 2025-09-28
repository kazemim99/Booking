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
// Example 7: Using API Endpoint Helpers
// ========================================
public class ServiceEndpointTests : ServiceCatalogIntegrationTestBase
{
    public ServiceEndpointTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetServiceById_WithValidId_ShouldReturnService()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var service = await CreateServiceForProviderAsync(provider, "Test Service");

        // Act - Using endpoint helper from base class
        var response = await GetServiceByIdAsync(service.Id.Value);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ServiceResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(service.Id.Value);
        result.Name.Should().Be("Test Service");
    }

    [Fact]
    public async Task GetProviderServices_WithMultipleServices_ShouldReturnAll()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 5);

        // Act - Using endpoint helper from base class
        var response = await GetProviderServicesAsyncApi(provider.Id.Value);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await GetResponseAsync<List<ServiceResponse>>(response);
        results.Should().HaveCount(5);
    }
}
