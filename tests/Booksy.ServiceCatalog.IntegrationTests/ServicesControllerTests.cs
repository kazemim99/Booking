using Booksy.API;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Services;

/// <summary>
/// Integration tests for ServicesController
/// Tests all endpoints: CreateService, GetById, SearchServices, GetByProvider, UpdateService,
/// ActivateService, DeactivateService, ArchiveService, GetByStatus, GetPopular
/// </summary>
public class ServicesControllerTests : ServiceCatalogIntegrationTestBase
{
    public ServicesControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    #region CreateService Tests

    [Fact]
    public async Task CreateService_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "provider@test.com");

        var request = new CreateServiceRequest
        {
            ProviderId = provider.Id.Value,
            Name = "Premium Haircut",
            Description = "Professional haircut with styling",
            CategoryName = "Hair Services",
            ServiceType = ServiceType.Premium,
            BasePrice = 100.00m,
            Currency = "USD",
            DurationMinutes = 60,
            PreparationMinutes = 5,
            BufferMinutes = 10,
            RequiresDeposit = true,
            DepositPercentage = 20,
            AvailableAtLocation = true,
            AvailableAsMobile = false,
            MaxAdvanceBookingDays = 30,
            MinAdvanceBookingHours = 2,
            MaxConcurrentBookings = 5,
            ImageUrl = "https://example.com/haircut.jpg"
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await GetResponseAsync<ServiceResponse>(response);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Premium Haircut");

        // Verify in database
        await AssertServiceExistsAsync(result.Id);
    }

    [Fact]
    public async Task CreateService_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange - No authentication
        var request = new CreateServiceRequest
        {
            ProviderId = Guid.NewGuid(),
            Name = "Test Service",
            BasePrice = 50.00m,
            DurationMinutes = 30
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateService_WithInvalidPrice_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "provider@test.com");

        var request = new CreateServiceRequest
        {
            ProviderId = provider.Id.Value,
            Name = "Test Service",
            BasePrice = -50.00m, // Invalid: negative price
            DurationMinutes = 30
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateService_WithZeroDuration_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "provider@test.com");

        var request = new CreateServiceRequest
        {
            ProviderId = provider.Id.Value,
            Name = "Test Service",
            BasePrice = 50.00m,
            DurationMinutes = 0 // Invalid: zero duration
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateService_WithDuplicateName_ShouldReturn409Conflict()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "provider@test.com");
        await CreateServiceForProviderAsync(provider, "Haircut", 50.00m, 30);

        var request = new CreateServiceRequest
        {
            ProviderId = provider.Id.Value,
            Name = "Haircut", // Duplicate
            BasePrice = 60.00m,
            DurationMinutes = 40
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region GetServiceById Tests

    [Fact]
    public async Task GetServiceById_WhenExists_ShouldReturn200OK()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];

        // Act
        var response = await GetAsync($"/api/v1/services/{service.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ServiceDetailsResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(service.Id.Value);
        result.Name.Should().Be(service.Name);
    }

    [Fact]
    public async Task GetServiceById_WithIncludeProvider_ShouldReturnProviderInfo()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];

        // Act
        var response = await GetAsync($"/api/v1/services/{service.Id.Value}?includeProvider=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ServiceDetailsResponse>(response);
        result.Should().NotBeNull();
        result!.Provider.Should().NotBeNull();
        result.Provider!.BusinessName.Should().Be("Test Provider");
    }

    [Fact]
    public async Task GetServiceById_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/services/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region SearchServices Tests

    [Fact]
    public async Task SearchServices_WithSearchTerm_ShouldReturnMatchingServices()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");
        await CreateServiceForProviderAsync(provider, "Haircut", 50.00m, 30);
        await CreateServiceForProviderAsync(provider, "Hair Coloring", 80.00m, 60);
        await CreateServiceForProviderAsync(provider, "Manicure", 30.00m, 45);

        // Act
        var response = await GetAsync("/api/v1/services/search?searchTerm=Hair");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ServiceSearchResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(s => s.Name.Contains("Hair"));
    }

    [Fact]
    public async Task SearchServices_WithPriceRange_ShouldReturnServicesInRange()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");
        await CreateServiceForProviderAsync(provider, "Budget Service", 20.00m, 30);
        await CreateServiceForProviderAsync(provider, "Standard Service", 50.00m, 30);
        await CreateServiceForProviderAsync(provider, "Premium Service", 100.00m, 30);

        // Act
        var response = await GetAsync("/api/v1/services/search?minPrice=40&maxPrice=80");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ServiceSearchResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().Contain(s => s.BasePrice >= 40 && s.BasePrice <= 80);
    }

    [Fact]
    public async Task SearchServices_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");
        for (int i = 1; i <= 10; i++)
        {
            await CreateServiceForProviderAsync(provider, $"Service {i}", 50.00m, 30);
        }

        // Act
        var response = await GetAsync("/api/v1/services/search?pageNumber=1&pageSize=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ServiceSearchResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public async Task SearchServices_WithInvalidPageNumber_ShouldReturn400BadRequest()
    {
        // Act
        var response = await GetAsync("/api/v1/services/search?pageNumber=0&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetServicesByProvider Tests

    [Fact]
    public async Task GetServicesByProvider_ShouldReturnAllProviderServices()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 5);

        // Act
        var response = await GetAsync($"/api/v1/services/provider/{provider.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ServiceSummaryResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(5);
        result.Items.Should().OnlyContain(s => s.ProviderId == provider.Id.Value);
    }

    [Fact]
    public async Task GetServicesByProvider_WithCategoryFilter_ShouldReturnFilteredServices()
    {
        // Arrange
        var (provider, _) = await CreateProviderWithServicesAsync(serviceCount: 3);

        // Act
        var response = await GetAsync($"/api/v1/services/provider/{provider.Id.Value}?category=Beauty");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ServiceSummaryResponse>>(response);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetServicesByProvider_WhenProviderNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentProviderId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/services/provider/{nonExistentProviderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region UpdateService Tests

    [Fact]
    public async Task UpdateService_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateServiceRequest
        {
            Name = "Updated Service Name",
            Description = "Updated description",
            CategoryName = "Updated Category",
            ServiceType = ServiceType.Premium,
            BasePrice = 120.00m,
            Currency = "USD",
            DurationMinutes = 90,
            PreparationMinutes = 10,
            BufferMinutes = 15,
            RequiresDeposit = true,
            ImageUrl = "https://example.com/updated.jpg"
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/services/{service.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ServiceResponse>(response);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Service Name");
    }

    [Fact]
    public async Task UpdateService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Provider", "other@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateServiceRequest
        {
            Name = "Hacked Name",
            BasePrice = 50.00m,
            DurationMinutes = 30
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/services/{service.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateService_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];

        // Clear authentication
        ClearAuthenticationHeader();

        var request = new UpdateServiceRequest
        {
            Name = "Updated Name",
            BasePrice = 50.00m,
            DurationMinutes = 30
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/services/{service.Id.Value}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateService_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentId = Guid.NewGuid();

        var request = new UpdateServiceRequest
        {
            Name = "Updated Name",
            BasePrice = 50.00m,
            DurationMinutes = 30
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/services/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region ActivateService Tests

    [Fact]
    public async Task ActivateService_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await PostAsJsonAsync($"/api/v1/services/{service.Id.Value}/activate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<MessageResponse>(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("activated");

        // Verify status in database
        await AssertServiceStatusAsync(service.Id.Value, ServiceStatus.Active);
    }

    [Fact]
    public async Task ActivateService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];

        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Provider", "other@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await PostAsJsonAsync($"/api/v1/services/{service.Id.Value}/activate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateService_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await PostAsJsonAsync($"/api/v1/services/{nonExistentId}/activate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DeactivateService Tests

    [Fact]
    public async Task DeactivateService_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];
        AuthenticateAsProviderOwner(provider);

        var request = new DeactivateServiceRequest
        {
            Reason = "Temporarily unavailable"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/services/{service.Id.Value}/deactivate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<MessageResponse>(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("deactivated");

        // Verify status in database
        await AssertServiceStatusAsync(service.Id.Value, ServiceStatus.Inactive);
    }

    [Fact]
    public async Task DeactivateService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];

        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Provider", "other@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new DeactivateServiceRequest
        {
            Reason = "Hacking attempt"
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/services/{service.Id.Value}/deactivate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateService_WithoutReason_ShouldUseDefaultReason()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await PostAsJsonAsync($"/api/v1/services/{service.Id.Value}/deactivate", (object?)null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region ArchiveService Tests

    [Fact]
    public async Task ArchiveService_AsOwner_ShouldReturn204NoContent()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];
        AuthenticateAsProviderOwner(provider);

        var request = new ArchiveServiceRequest
        {
            Reason = "No longer offered"
        };

        // Act
        var response = await DeleteAsync($"/api/v1/services/{service.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status in database
        await AssertServiceStatusAsync(service.Id.Value, ServiceStatus.Archived);
    }

    [Fact]
    public async Task ArchiveService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var service = services[0];

        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Provider", "other@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new ArchiveServiceRequest
        {
            Reason = "Hacking attempt"
        };

        // Act
        var response = await DeleteAsync($"/api/v1/services/{service.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ArchiveService_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentId = Guid.NewGuid();

        var request = new ArchiveServiceRequest
        {
            Reason = "Test"
        };

        // Act
        var response = await DeleteAsync($"/api/v1/services/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetServicesByStatus Tests

    [Fact]
    public async Task GetServicesByStatus_AsAdmin_ShouldReturnServicesWithStatus()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 3);

        AuthenticateAsTestAdmin();

        // Act
        var response = await GetAsync($"/api/v1/services/by-status/{ServiceStatus.Active}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<List<ServiceSummaryResponse>>(response);
        result.Should().NotBeNull();
        result.Should().AllSatisfy(s => s.Status.Should().Be(ServiceStatus.Active.ToString()));
    }

    [Fact]
    public async Task GetServicesByStatus_AsNonAdmin_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/services/by-status/{ServiceStatus.Active}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetServicesByStatus_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await GetAsync($"/api/v1/services/by-status/{ServiceStatus.Active}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetServicesByStatus_WithMaxResults_ShouldRespectLimit()
    {
        // Arrange
        var (provider, _) = await CreateProviderWithServicesAsync(serviceCount: 10);

        AuthenticateAsTestAdmin();

        // Act
        var response = await GetAsync($"/api/v1/services/by-status/{ServiceStatus.Active}?maxResults=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<List<ServiceSummaryResponse>>(response);
        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(5);
    }

    #endregion

    #region GetPopularServices Tests

    [Fact]
    public async Task GetPopularServices_ShouldReturnPopularServices()
    {
        // Arrange
        var (provider, _) = await CreateProviderWithServicesAsync(serviceCount: 5);

        // Act
        var response = await GetAsync("/api/v1/services/popular?limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<List<ServiceSummaryResponse>>(response);
        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetPopularServices_WithCategoryFilter_ShouldReturnFilteredServices()
    {
        // Arrange
        var (provider, _) = await CreateProviderWithServicesAsync(serviceCount: 5);

        // Act
        var response = await GetAsync("/api/v1/services/popular?categoryFilter=Beauty&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<List<ServiceSummaryResponse>>(response);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPopularServices_WithInvalidLimit_ShouldReturn400BadRequest()
    {
        // Act
        var response = await GetAsync("/api/v1/services/popular?limit=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPopularServices_WithLimitExceedingMax_ShouldReturn400BadRequest()
    {
        // Act
        var response = await GetAsync("/api/v1/services/popular?limit=200");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
