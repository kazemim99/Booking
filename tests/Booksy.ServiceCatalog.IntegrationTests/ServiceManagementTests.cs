using Booksy.API;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;
using static Booksy.ServiceCatalog.API.Controllers.V1.ProviderSettingsController;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Services;

/// <summary>
/// Integration tests for Service Management endpoints
/// Covers: CRUD operations for provider services
/// Endpoints: GET/POST/PUT/DELETE /api/v1/providers/{id}/services
/// </summary>
public class ServiceManagementTests : ServiceCatalogIntegrationTestBase
{
    public ServiceManagementTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }
    #region Create Service Tests

    [Fact]
    public async Task CreateService_AsProviderOwner_ShouldReturn201Created()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "salon@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new AddServiceRequest
        {
            ServiceName = "Haircut",
            Description = "Professional haircut service",
            DurationHours = 0,
            Duration = 30,
            BasePrice = 50.00m,
            Currency = "USD",
            Category = "Hair Services",
            IsMobileService = false
        };

        // Act
        var response = await PostAsJsonAsync<AddServiceRequest, ServiceDetailResponse>($"/api/v1/providers/{provider.Id.Value}/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().NotBeEmpty();
        response.Data.Name.Should().Be("Haircut");
        response.Data.DurationMinutes.Should().Be(30);
        response.Data.Price.Should().Be(50.00m);
    }

    [Fact]
    public async Task CreateService_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "validation@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new AddServiceRequest
        {
            ServiceName = "", // Invalid: empty name
            Description = "Test",
            DurationHours = 0,
            Duration = 30,
            BasePrice = 50.00m,
            Currency = "USD",
            Category = "Test",
            IsMobileService = false
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "owner@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "other@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new AddServiceRequest
        {
            ServiceName = "Unauthorized Service",
            Description = "Should fail",
            DurationHours = 0,
            Duration = 30,
            BasePrice = 50.00m,
            Currency = "USD",
            Category = "Test",
            IsMobileService = false
        };

        // Act - Try to create service for different provider
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateService_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "noauth@test.com");
        ClearAuthenticationHeader();

        var request = new AddServiceRequest
        {
            ServiceName = "Test Service",
            Description = "Test",
            DurationHours = 0,
            Duration = 30,
            BasePrice = 50.00m,
            Currency = "USD",
            Category = "Test",
            IsMobileService = false
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Services Tests

    [Fact]
    public async Task GetServices_AsProviderOwner_ShouldReturn200WithServices()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Service List Salon", "list@test.com");

        // Create multiple services
        var service1 = await CreateServiceForProviderAsync(provider, "Service 1", 30.00m, 30);
        var service2 = await CreateServiceForProviderAsync(provider, "Service 2", 50.00m, 60);
        var service3 = await CreateServiceForProviderAsync(provider, "Service 3", 75.00m, 90);

        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<List<ServiceDetailResponse>>(response);
        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result.Should().Contain(s => s.Name == "Service 1");
        result.Should().Contain(s => s.Name == "Service 2");
        result.Should().Contain(s => s.Name == "Service 3");
    }

    [Fact]
    public async Task GetServices_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "getowner@test.com");
        await CreateServiceForProviderAsync(provider, "Private Service", 50.00m, 60);

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "getother@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Update Service Tests

    [Fact]
    public async Task UpdateService_AsProviderOwner_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Update Salon", "update@test.com");
        var service = await CreateServiceForProviderAsync(provider, "Original Service", 50.00m, 60);

        AuthenticateAsProviderOwner(provider);

        var request = new UpdateProviderServiceRequest
        {
            ServiceName = "Updated Service",
            Description = "Updated description",
            DurationHours = 1,
            DurationMinutes = 30,
            Price = 75.00m,
            Currency = "USD",
            Category = "Updated Category",
            IsMobileService = true
        };

        // Act
        var response = await PutAsJsonAsync<UpdateProviderServiceRequest, ServiceDetailResponse>(
            $"/api/v1/providers/{provider.Id.Value}/services/{service.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be("Updated Service");
        response.Data.DurationMinutes.Should().Be(90); // 1 hour + 30 minutes
        response.Data.Price.Should().Be(75.00m);
    }

    [Fact]
    public async Task UpdateService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "updateowner@test.com");
        var service = await CreateServiceForProviderAsync(provider, "Owner Service", 50.00m, 60);

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "updateother@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateProviderServiceRequest
        {
            ServiceName = "Hacked Service",
            Description = "Should fail",
            DurationHours = 0,
            DurationMinutes = 30,
            Price = 10.00m,
            Currency = "USD",
            Category = "Test",
            IsMobileService = false
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{service.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateService_WithNonExistentServiceId_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "notfound@test.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentServiceId = Guid.NewGuid();
        var request = new UpdateProviderServiceRequest
        {
            ServiceName = "Test",
            Description = "Test",
            DurationHours = 0,
            DurationMinutes = 30,
            Price = 50.00m,
            Currency = "USD",
            Category = "Test",
            IsMobileService = false
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{nonExistentServiceId}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Service Tests

    [Fact]
    public async Task DeleteService_AsProviderOwner_ShouldReturn204NoContent()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Delete Salon", "delete@test.com");
        var service = await CreateServiceForProviderAsync(provider, "Service to Delete", 50.00m, 60);

        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/services/{service.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify service was actually deleted
        var getResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/services");
        var services = await GetResponseAsync<List<ServiceDetailResponse>>(getResponse);
        services.Should().NotContain(s => s.Id == service.Id.Value);
    }

    [Fact]
    public async Task DeleteService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "deleteowner@test.com");
        var service = await CreateServiceForProviderAsync(provider, "Protected Service", 50.00m, 60);

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "deleteother@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/services/{service.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteService_WithNonExistentServiceId_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "deletenotfound@test.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentServiceId = Guid.NewGuid();

        // Act
        var response = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/services/{nonExistentServiceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Complete Service Workflow Test

    [Fact]
    public async Task ServiceCompleteWorkflow_CreateUpdateDelete_ShouldWorkEndToEnd()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Workflow Salon", "workflow@test.com");
        AuthenticateAsProviderOwner(provider);

        // Step 1: Create service
        var createRequest = new AddServiceRequest
        {
            ServiceName = "Workflow Service",
            Description = "Test workflow",
            DurationHours = 1,
            Duration = 0,
            BasePrice = 100.00m,
            Currency = "USD",
            Category = "Test",
            IsMobileService = false
        };

        var createResponse = await PostAsJsonAsync<AddServiceRequest, ServiceDetailResponse>($"/api/v1/providers/{provider.Id.Value}/services", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 2: Update service
        var updateRequest = new UpdateProviderServiceRequest
        {
            ServiceName = "Updated Workflow Service",
            Description = "Updated description",
            DurationHours = 0,
            DurationMinutes = 45,
            Price = 120.00m,
            Currency = "USD",
            Category = "Updated",
            IsMobileService = true
        };

        var updateResponse = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{createResponse.Data!.Id}",
            updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Delete service
        var deleteResponse = await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/services/{createResponse.Data.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 4: Verify deletion
        var getResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/services");
        var services = await GetResponseAsync<List<ServiceDetailResponse>>(getResponse);
        services.Should().NotContain(s => s.Id == createResponse.Data.Id);
    }

    #endregion
}
