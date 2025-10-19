using Booksy.API;
using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.API.Controllers.V1;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.ProviderSettings;

/// <summary>
/// Integration tests for ProviderSettingsController
/// Tests all endpoints: Business Info (Get/Update), Location (Get/Update), Working Hours (Get/Update), Services (Get/Add/Update/Delete)
/// </summary>
public class ProviderSettingsControllerTests : ServiceCatalogIntegrationTestBase
{
    public ProviderSettingsControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    #region Business Info Tests

    [Fact]
    public async Task GetBusinessInfo_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/business-info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<BusinessInfoResponse>(response);
        result.Should().NotBeNull();
        result!.BusinessName.Should().Be("Test Business");
    }

    [Fact]
    public async Task GetBusinessInfo_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        // Authenticate as different provider
        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/business-info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBusinessInfo_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");

        // Clear authentication
        ClearAuthenticationHeader();

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/business-info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateBusinessInfo_WithValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Original Name", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateBusinessInfoRequest
        {
            BusinessName = "Updated Business Name",
            Description = "Updated description",
            OwnerFirstName = "John",
            OwnerLastName = "Doe",
            PhoneNumber = "+989121234567",
            Email = "updated@test.com",
            Website = "https://www.updated.com"
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/business-info", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<BusinessInfoResponse>(response);
        result.Should().NotBeNull();
        result!.BusinessName.Should().Be("Updated Business Name");
        result.Email.Should().Be("updated@test.com");
    }

    [Fact]
    public async Task UpdateBusinessInfo_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateBusinessInfoRequest
        {
            BusinessName = "", // Invalid: empty
            PhoneNumber = "+989121234567"
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/business-info", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBusinessInfo_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateBusinessInfoRequest
        {
            BusinessName = "Hacked Name",
            PhoneNumber = "+989121234567"
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/business-info", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Location Tests

    [Fact]
    public async Task GetLocation_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/location");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<LocationResponse>(response);
        result.Should().NotBeNull();
        result!.AddressLine1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLocation_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/location");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateLocation_WithValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateLocationRequest
        {
            AddressLine1 = "456 New Street",
            AddressLine2 = "Suite 100",
            City = "Tehran",
            State = "Tehran",
            PostalCode = "54321",
            Country = "Iran",
            Latitude = 35.6892,
            Longitude = 51.3890,
            FormattedAddress = "456 New Street, Suite 100, Tehran",
            IsShared = false
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/location", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<LocationResponse>(response);
        result.Should().NotBeNull();
        result!.AddressLine1.Should().Be("456 New Street");
        result.City.Should().Be("Tehran");
    }

    [Fact]
    public async Task UpdateLocation_WithInvalidCoordinates_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateLocationRequest
        {
            AddressLine1 = "123 Main St",
            City = "Tehran",
            PostalCode = "12345",
            Country = "Iran",
            Latitude = 999, // Invalid
            Longitude = 999  // Invalid
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/location", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateLocation_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateLocationRequest
        {
            AddressLine1 = "Hacked Address",
            City = "Tehran",
            PostalCode = "12345",
            Country = "Iran"
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/location", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Working Hours Tests

    [Fact]
    public async Task GetWorkingHours_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/working-hours");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<WorkingHoursResponse>(response);
        result.Should().NotBeNull();
        result!.BusinessHours.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWorkingHours_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/working-hours");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateWorkingHours_WithValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateWorkingHoursRequest
        {
            BusinessHours = new Dictionary<int, DayHoursRequest?>
            {
                { 1, new DayHoursRequest
                    {
                        DayOfWeek = 1,
                        IsOpen = true,
                        OpenTime = new TimeSlotRequest { Hours = 9, Minutes = 0 },
                        CloseTime = new TimeSlotRequest { Hours = 17, Minutes = 0 },
                        Breaks = new List<BreakTimeRequest>()
                    }
                },
                { 2, new DayHoursRequest
                    {
                        DayOfWeek = 2,
                        IsOpen = true,
                        OpenTime = new TimeSlotRequest { Hours = 9, Minutes = 0 },
                        CloseTime = new TimeSlotRequest { Hours = 17, Minutes = 0 },
                        Breaks = new List<BreakTimeRequest>()
                    }
                },
                { 0, null } // Sunday closed
            }
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/working-hours", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateWorkingHours_WithInvalidTimes_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateWorkingHoursRequest
        {
            BusinessHours = new Dictionary<int, DayHoursRequest?>
            {
                { 1, new DayHoursRequest
                    {
                        DayOfWeek = 1,
                        IsOpen = true,
                        OpenTime = new TimeSlotRequest { Hours = 25, Minutes = 0 }, // Invalid hour
                        CloseTime = new TimeSlotRequest { Hours = 17, Minutes = 0 },
                        Breaks = new List<BreakTimeRequest>()
                    }
                }
            }
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/working-hours", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateWorkingHours_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateWorkingHoursRequest
        {
            BusinessHours = new Dictionary<int, DayHoursRequest?>()
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/working-hours", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Services Management Tests

    [Fact]
    public async Task GetServices_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 3);
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<List<ServiceDetailResponse>>(response);
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetServices_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, _) = await CreateProviderWithServicesAsync(serviceCount: 2);
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddService_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new AddServiceRequest
        {
            ServiceName = "New Service",
            Description = "A new service offering",
            DurationHours = 1,
            DurationMinutes = 30,
            Price = 100.00m,
            Currency = "IRR",
            Category = "Beauty",
            IsMobileService = false
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await GetResponseAsync<ServiceDetailResponse>(response);
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Service");
        result.Price.Should().Be(100.00m);
        result.DurationMinutes.Should().Be(90);
    }

    [Fact]
    public async Task AddService_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new AddServiceRequest
        {
            ServiceName = "", // Invalid: empty
            DurationHours = 0,
            DurationMinutes = 0,
            Price = -10.00m // Invalid: negative
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        var request = new AddServiceRequest
        {
            ServiceName = "Hacked Service",
            DurationHours = 1,
            Price = 50.00m
        };

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/services", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateService_WithValidRequest_ShouldReturn200OK()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        AuthenticateAsProviderOwner(provider);

        var serviceToUpdate = services[0];

        var request = new UpdateProviderServiceRequest
        {
            ServiceName = "Updated Service Name",
            Description = "Updated description",
            DurationHours = 2,
            DurationMinutes = 0,
            Price = 150.00m,
            Currency = "IRR",
            Category = "Beauty",
            IsMobileService = true
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{serviceToUpdate.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ServiceDetailResponse>(response);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Service Name");
        result.Price.Should().Be(150.00m);
    }

    [Fact]
    public async Task UpdateService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        var serviceToUpdate = services[0];

        var request = new UpdateProviderServiceRequest
        {
            ServiceName = "Hacked Name",
            DurationHours = 1,
            Price = 50.00m
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{serviceToUpdate.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateService_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentServiceId = Guid.NewGuid();

        var request = new UpdateProviderServiceRequest
        {
            ServiceName = "Updated Name",
            DurationHours = 1,
            Price = 50.00m
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{nonExistentServiceId}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteService_AsOwner_ShouldReturn204NoContent()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 2);
        AuthenticateAsProviderOwner(provider);

        var serviceToDelete = services[0];

        // Act
        var response = await DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{serviceToDelete.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify service count decreased
        await AssertProviderServiceCountAsync(provider.Id.Value, 1);
    }

    [Fact]
    public async Task DeleteService_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 1);
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Business", "other@test.com");

        AuthenticateAsProviderOwner(otherProvider);

        var serviceToDelete = services[0];

        // Act
        var response = await DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{serviceToDelete.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteService_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Business", "owner@test.com");
        AuthenticateAsProviderOwner(provider);

        var nonExistentServiceId = Guid.NewGuid();

        // Act
        var response = await DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/services/{nonExistentServiceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
