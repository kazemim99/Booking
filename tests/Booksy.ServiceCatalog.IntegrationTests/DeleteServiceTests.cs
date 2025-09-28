// ========================================
// Example 1: Basic Test Using ServiceCatalogIntegrationTestBase
// ========================================
using Booksy.API;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Services;

// ========================================
// Example 5: Using Assertion Helpers
// ========================================
public class DeleteServiceTests : ServiceCatalogIntegrationTestBase
{
    public DeleteServiceTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task DeleteService_AsOwner_ShouldRemoveFromDatabase()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var service = await CreateServiceForProviderAsync(provider, "Service to Delete");

        AuthenticateAsProviderOwner(provider);

        // Verify service exists
        await AssertServiceExistsAsync(service.Id.Value);

        // Act
        var response = await DeleteServiceAsync(service.Id.Value);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Use assertion helper - should not exist anymore
        await AssertServiceNotExistsAsync(service.Id.Value);
        await AssertProviderServiceCountAsync(provider.Id.Value, 0);
    }

    [Fact]
    public async Task DeleteService_AsAdmin_ShouldSucceedRegardlessOfOwnership()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var service = await CreateServiceForProviderAsync(provider);

        // Authenticate as Admin (not the provider owner)
        AuthenticateAsAdmin("admin@booksy.com");

        await AssertServiceExistsAsync(service.Id.Value);

        // Act
        var response = await DeleteServiceAsync(service.Id.Value);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AssertServiceNotExistsAsync(service.Id.Value);
    }
}
