using Booksy.API;
using Booksy.ServiceCatalog.Api.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;
using static Booksy.ServiceCatalog.API.Controllers.V1.ProviderSettingsController;

namespace Booksy.ServiceCatalog.IntegrationTests.API.ProviderSettings;

/// <summary>
/// Integration tests for Provider Settings endpoints
/// Covers: Business Info, Location, Working Hours
/// </summary>
public class ProviderSettingsTests : ServiceCatalogIntegrationTestBase
{
    public ProviderSettingsTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }
    #region Business Info Tests

    [Fact]
    public async Task GetBusinessInfo_AsOwner_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Business Info Salon", "businessinfo@test.com");
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/business-info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<BusinessInfoResponse>(response);
        result.Should().NotBeNull();
        result!.BusinessName.Should().Be("Business Info Salon");
        result.Email.Should().Be("businessinfo@test.com");
    }

    [Fact]
    public async Task GetBusinessInfo_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Private Salon", "private@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "other@test.com");
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "noauth@test.com");
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Update Business Salon", "updatebiz@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateBusinessInfoRequest
        {
            BusinessName = "Updated Business Name",
        };

        // Act
        var response = await PutAsJsonAsync<UpdateBusinessInfoRequest, BusinessInfoResponse>($"/api/v1/providers/{provider.Id.Value}/business-info", request);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Should().NotBeNull();
        response.Data!.BusinessName.Should().Be("Updated Business Name");
        response.Data.Description.Should().Be("Updated description with more details");
        response.Data.PhoneNumber.Should().Be("+19876543210");
        response.Data.Email.Should().Be("updated@test.com");
        response.Data.Website.Should().Be("https://updatedbusiness.com");
    }

    [Fact]
    public async Task UpdateBusinessInfo_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Salon", "validation@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateBusinessInfoRequest
        {
            BusinessName = "", // Invalid: empty business name
            Description = "Test",
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "owner@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "otherbiz@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateBusinessInfoRequest
        {
            BusinessName = "Hacked Business",
            Description = "Should fail",
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Location Salon", "location@test.com");
        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/location");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<LocationResponse>(response);
        result.Should().NotBeNull();
        result!.Street.Should().NotBeNullOrEmpty();
        result.City.Should().NotBeNullOrEmpty();
        result.Country.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLocation_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Private Location", "privateloc@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "otherloc@test.com");
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Update Location Salon", "updateloc@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateLocationRequest
        {
            AddressLine1 = "456 New Street",
            City = "San Francisco",
            PostalCode = "94102",
            Country = "USA",
            Latitude = 37.7749,
            Longitude = -122.4194
        };

        // Act
        var response = await PutAsJsonAsync<UpdateLocationRequest, LocationResponse>($"/api/v1/providers/{provider.Id.Value}/location", request);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Street.Should().Be("456 New Street");
        response.Data.City.Should().Be("San Francisco");
        response.Data.State.Should().Be("CA");
        response.Data.PostalCode.Should().Be("94102");
        response.Data.Country.Should().Be("USA");
    }

    [Fact]
    public async Task UpdateLocation_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "ownerloc@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "hackloc@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateLocationRequest
        {
            City = "Hack City",
            PostalCode = "00000",
            Country = "Hack"
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Hours Salon", "hours@test.com");
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Private Hours", "privatehours@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "otherhours@test.com");
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
        var provider = await CreateAndAuthenticateAsProviderAsync("Update Hours Salon", "updatehours@test.com");
        AuthenticateAsProviderOwner(provider);

        var request = new UpdateWorkingHoursRequest
        {
            BusinessHours = new Dictionary<string, RegistrationDayScheduleRequest?>
            {
                ["Monday"] = new RegistrationDayScheduleRequest
                {
                    IsOpen = true,
                    OpenTime = new TimeComponentsRequest
                    {
                        Hours = 10,
                        Minutes = 0,
                    },
                    CloseTime = new TimeComponentsRequest
                    {
                        Hours = 17,
                        Minutes = 0
                    }
                },
                ["Tuesday"] = new DayHoursRequest
                {
                    IsOpen = true,
                    OpenTime = new TimeComponentsRequest
                    {
                        Hours = 10,
                        Minutes = 0,
                    },
                    CloseTime = new TimeComponentsRequest
                    {
                        Hours = 17,
                        Minutes = 0
                    }
                },
                ["Wednesday"] = new DayHoursRequest
                {
                    IsOpen = true,
                    OpenTime = new TimeComponentsRequest
                    {
                        Hours = 10,
                        Minutes = 0,
                    },
                    CloseTime = new TimeComponentsRequest
                    {
                        Hours = 17,
                        Minutes = 0
                    }
                },
                ["Thursday"] = new DayHoursRequest
                {
                    IsOpen = true,
                    OpenTime = new TimeComponentsRequest
                    {
                        Hours = 10,
                        Minutes = 0,
                    },
                    CloseTime = new TimeComponentsRequest
                    {
                        Hours = 17,
                        Minutes = 0
                    }
                },
                ["Friday"] = new DayHoursRequest
                {
                    OpenTime = new TimeComponentsRequest
                    {
                        Hours = 10,
                        Minutes = 0,
                    },
                    CloseTime = new TimeComponentsRequest
                    {
                        Hours = 17,
                        Minutes = 0
                    }
                },
                ["Saturday"] = new DayHoursRequest
                {
                    IsOpen = false
                },
                ["Sunday"] = new DayHoursRequest
                {
                    IsOpen = false
                }
            }
        };

        // Act
        var response = await PutAsJsonAsync<UpdateWorkingHoursRequest, UpdateWorkingHours>($"/api/v1/providers/{provider.Id.Value}/working-hours", request);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.BusinessHours.Should().NotBeNull();
        response.Data.BusinessHours.Should().ContainKey("Monday");
    }

    [Fact]
    public async Task UpdateWorkingHours_AsNonOwner_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Owner Salon", "ownerhours@test.com");

        // Authenticate as different provider
        var otherProvider = await CreateAndAuthenticateAsProviderAsync("Other Salon", "hackhours@test.com");
        AuthenticateAsProviderOwner(otherProvider);

        var request = new UpdateWorkingHoursRequest
        {
            BusinessHours = new Dictionary<string, RegistrationDayScheduleRequest?>()
        };

        // Act
        var response = await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/working-hours", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Complete Settings Workflow Test

    [Fact]
    public async Task ProviderSettings_CompleteWorkflow_ShouldUpdateAllSettings()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Complete Workflow Salon", "completeworkflow@test.com");
        AuthenticateAsProviderOwner(provider);

        // Step 1: Update Business Info
        var businessInfoRequest = new UpdateBusinessInfoRequest
        {
            
            BusinessName = "Workflow Business Updated",
        };

        var businessResponse = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/business-info",
            businessInfoRequest);

        businessResponse.Error.Should().BeNull();
        businessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Update Location
        var locationRequest = new UpdateLocationRequest
        {
            AddressLine1 = "789 Workflow Ave",
            City = "Workflow City",
            PostalCode = "12345",
            Country = "USA"
        };

        var locationResponse = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/location",
            locationRequest);


        locationResponse.Error.Should().BeNull();
        locationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Update Working Hours
        var hoursRequest = new UpdateWorkingHoursRequest
        {
            BusinessHours = new Dictionary<string, RegistrationDayScheduleRequest?>
            {
                ["Monday"] = new RegistrationDayScheduleRequest
                {
                    IsOpen = true,
                    OpenTime = new TimeComponentsRequest
                    {
                        Hours = 10,
                        Minutes = 0
                    },
                    CloseTime = new TimeComponentsRequest
                    {

                        Hours = 17,
                        Minutes = 0
                    }
                }
            }
        };

        var hoursResponse = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/working-hours",
            hoursRequest);
        hoursResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Verify all updates by getting provider info
        var getBusinessResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/business-info");
        var businessResult = await GetResponseAsync<BusinessInfoResponse>(getBusinessResponse);
        businessResult!.BusinessName.Should().Be("Workflow Business Updated");

        var getLocationResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/location");
        var locationResult = await GetResponseAsync<LocationResponse>(getLocationResponse);
        locationResult!.City.Should().Be("Workflow City");
    }

    #endregion
}
