// ========================================
// Example 1: Basic Test Using ServiceCatalogIntegrationTestBase
// ========================================
using Booksy.API;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Services;

/// <summary>
/// Your test class inherits from ServiceCatalogIntegrationTestBase
/// </summary>
public class CreateServiceTests : ServiceCatalogIntegrationTestBase
{
    public CreateServiceTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateService_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange - Using helper method from base class
        var provider = await CreateAndAuthenticateAsProviderAsync(
            businessName: "Test Salon",
            email: "provider@salon.com"
        );

        var request = new CreateServiceRequest
        {
            ProviderId = provider.Id.Value,
            Name = "Standard Haircut",
            Description = "Professional haircut service",
            CategoryName = "Hair Services",
            ServiceType = ServiceType.Standard,
            BasePrice = 75.00m,
            Currency = "USD",
            DurationMinutes = 45
        };
     

        // Act
        var response = await PostAsJsonAsync("/api/v1/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await GetResponseAsync<ServiceResponse>(response);
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);

        // Use assertion helper from base class
        await AssertServiceExistsAsync(result.Id);
        await AssertProviderServiceCountAsync(provider.Id.Value, 1);
    }
}

// ========================================
// Example 2: Using Setup Helpers
// ========================================
public class UpdateServiceTests : ServiceCatalogIntegrationTestBase
{
    public UpdateServiceTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UpdateService_AsOwner_ShouldSucceed()
    {
        // Arrange - Create provider with services using helper
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 3);
        var serviceToUpdate = services[0];

        // Authenticate as provider owner
        AuthenticateAsProviderOwner(provider);

        var updateRequest = new UpdateServiceRequest
        {
            Name = "Updated Service Name",
            Description = "Updated description",
             //= 100.00m,
            DurationMinutes = 90
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/services/{serviceToUpdate.Id.Value}",
            updateRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database using helper
        var updatedService = await FindServiceAsync(serviceToUpdate.Id.Value);
        updatedService.Should().NotBeNull();
        updatedService!.Name.Should().Be("Updated Service Name");
        updatedService.BasePrice.Amount.Should().Be(100.00m);
    }

    [Fact]
    public async Task UpdateService_AsNonOwner_ShouldReturn403()
    {
        // Arrange - Create two providers
        var ownerProvider = await CreateAndAuthenticateAsProviderAsync("Owner Provider", "owner@test.com");
        var service = await CreateServiceForProviderAsync(ownerProvider, "Owner's Service");

        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Provider", "other@test.com");

        // Authenticate as the OTHER provider (not the owner)
        AuthenticateAsProviderOwner(otherProvider);

        var updateRequest = new UpdateServiceRequest
        {
            Name = "Trying to update",
            Description = "Should fail",
            //BasePrice = 50.00m,
            DurationMinutes = 60
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/services/{service.Id.Value}",
            updateRequest
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

// ========================================
// Example 3: Using Authentication Helpers
// ========================================
public class ServiceAuthorizationTests : ServiceCatalogIntegrationTestBase
{
    public ServiceAuthorizationTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetMyServices_AsVerifiedProvider_ShouldIncludeAllServices()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        await CreateServiceForProviderAsync(provider, "Service 1");
        await CreateServiceForProviderAsync(provider, "Service 2");
        await CreateServiceForProviderAsync(provider, "Service 3");

        // Authenticate as verified provider (has additional role)
        AuthenticateAsProvider(provider.ContactInfo.Email,provider.Id.Value.ToString());

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var services = await GetResponseAsync<List<ServiceResponse>>(response);
        services.Should().HaveCount(3);
    }

  
}
