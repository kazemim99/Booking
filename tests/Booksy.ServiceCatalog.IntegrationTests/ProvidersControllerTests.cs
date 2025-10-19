using Booksy.API;
using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Providers;

/// <summary>
/// Integration tests for ProvidersController
/// Tests all endpoints: RegisterProvider, RegisterProviderFull, GetById, SearchProviders, GetByLocation, ActivateProvider, GetByStatus
/// </summary>
public class ProvidersControllerTests : ServiceCatalogIntegrationTestBase
{
    public ProvidersControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    #region RegisterProvider Tests

    [Fact]
    public async Task RegisterProvider_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new RegisterProviderRequest
        {
            OwnerId = Guid.NewGuid(),
            BusinessName = "Test Beauty Salon",
            Description = "A professional beauty salon",
            Type = ProviderType.Salon,
            WebsiteUrl = "https://www.testbeautysalon.com",
            ContactInfo = new ContactInfoRequest
            {
                PrimaryPhone = "+989121234567",
                SecondaryPhone = null
            },
            Address = new AddressRequest
            {
                Street = "123 Main St",
                City = "Tehran",
                State = "Tehran",
                PostalCode = "12345",
                Country = "Iran",
                Latitude = 35.6892,
                Longitude = 51.3890
            }
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/providers/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await GetResponseAsync<ProviderResponse>(response);
        result.Should().NotBeNull();
        result!.BusinessName.Should().Be(request.BusinessName);
        result.Type.Should().Be(request.Type.ToString());
        result.Status.Should().Be(ProviderStatus.PendingVerification.ToString());

        // Verify in database
        await AssertProviderExistsAsync(result.Id);
    }

    [Fact]
    public async Task RegisterProvider_WithMissingBusinessName_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new RegisterProviderRequest
        {
            OwnerId = Guid.NewGuid(),
            BusinessName = "", // Invalid: empty
            Description = "A professional beauty salon",
            Type = ProviderType.Salon,
            ContactInfo = new ContactInfoRequest
            {
                PrimaryPhone = "+989121234567"
            },
            Address = new AddressRequest
            {
                Street = "123 Main St",
                City = "Tehran",
                State = "Tehran",
                PostalCode = "12345",
                Country = "Iran"
            }
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/providers/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterProvider_WithInvalidPhoneNumber_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new RegisterProviderRequest
        {
            OwnerId = Guid.NewGuid(),
            BusinessName = "Test Salon",
            Description = "A professional salon",
            Type = ProviderType.Individual,
            ContactInfo = new ContactInfoRequest
            {
                PrimaryPhone = "invalid-phone" // Invalid format
            },
            Address = new AddressRequest
            {
                Street = "123 Main St",
                City = "Tehran",
                State = "Tehran",
                PostalCode = "12345",
                Country = "Iran"
            }
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/providers/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region RegisterProviderFull Tests

    [Fact]
    public async Task RegisterProviderFull_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId.ToString(), "owner@test.com");

        var request = new RegisterProviderFullRequest
        {
            OwnerId = userId,
            CategoryId = "beauty-salon",
            BusinessInfo = new BusinessInfoRequest
            {
                BusinessName = "Full Service Salon",
                OwnerFirstName = "John",
                OwnerLastName = "Doe",
                PhoneNumber = "+989121234567"
            },
            Address = new AddressRequest
            {
                Street = "123 Main St",
                City = "Tehran",
                State = "Tehran",
                PostalCode = "12345",
                Country = "Iran"
            },
            Location = new BusinessLocationRequest
            {
                Latitude = 35.6892,
                Longitude = 51.3890,
                FormattedAddress = "123 Main St, Tehran"
            },
            BusinessHours = new Dictionary<int, DayHoursRequest?>
            {
                { 1, new DayHoursRequest { DayOfWeek = 1, IsOpen = true, OpenTime = new TimeSlotRequest { Hours = 9, Minutes = 0 }, CloseTime = new TimeSlotRequest { Hours = 17, Minutes = 0 }, Breaks = new List<BreakTimeRequest>() } },
                { 2, new DayHoursRequest { DayOfWeek = 2, IsOpen = true, OpenTime = new TimeSlotRequest { Hours = 9, Minutes = 0 }, CloseTime = new TimeSlotRequest { Hours = 17, Minutes = 0 }, Breaks = new List<BreakTimeRequest>() } }
            },
            Services = new List<ServiceRequest>
            {
                new ServiceRequest { Name = "Haircut", DurationHours = 0, DurationMinutes = 30, Price = 50000, PriceType = "fixed" }
            },
            AssistanceOptions = new List<string> { "Online Booking" },
            TeamMembers = new List<TeamMemberRequest>
            {
                new TeamMemberRequest { Name = "John Doe", Email = "john@test.com", PhoneNumber = "1234567890", CountryCode = "+98", Position = "Owner", IsOwner = true }
            }
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/providers/register-full", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await GetResponseAsync<ProviderFullRegistrationResponse>(response);
        result.Should().NotBeNull();
        result!.BusinessName.Should().Be("Full Service Salon");
        result.Status.Should().Be(ProviderStatus.PendingVerification.ToString());
        result.ServicesCount.Should().Be(1);
        result.StaffCount.Should().Be(1);

        // Verify in database
        await AssertProviderExistsAsync(result.ProviderId);
    }

    [Fact]
    public async Task RegisterProviderFull_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange - No authentication
        var request = new RegisterProviderFullRequest
        {
            OwnerId = Guid.NewGuid(),
            CategoryId = "beauty-salon",
            BusinessInfo = new BusinessInfoRequest { BusinessName = "Salon", OwnerFirstName = "John", OwnerLastName = "Doe", PhoneNumber = "+989121234567" },
            Address = new AddressRequest { Street = "123 Main St", City = "Tehran", PostalCode = "12345", Country = "Iran" },
            BusinessHours = new Dictionary<int, DayHoursRequest?>(),
            Services = new List<ServiceRequest>(),
            AssistanceOptions = new List<string>(),
            TeamMembers = new List<TeamMemberRequest>()
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/providers/register-full", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterProviderFull_WithMismatchedOwnerId_ShouldReturn403Forbidden()
    {
        // Arrange
        var authenticatedUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        AuthenticateAsUser(authenticatedUserId.ToString(), "user@test.com");

        var request = new RegisterProviderFullRequest
        {
            OwnerId = differentUserId, // Different from authenticated user
            CategoryId = "beauty-salon",
            BusinessInfo = new BusinessInfoRequest { BusinessName = "Salon", OwnerFirstName = "John", OwnerLastName = "Doe", PhoneNumber = "+989121234567" },
            Address = new AddressRequest { Street = "123 Main St", City = "Tehran", PostalCode = "12345", Country = "Iran" },
            BusinessHours = new Dictionary<int, DayHoursRequest?>(),
            Services = new List<ServiceRequest>(),
            AssistanceOptions = new List<string>(),
            TeamMembers = new List<TeamMemberRequest>()
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/providers/register-full", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetProviderById Tests

    [Fact]
    public async Task GetProviderById_WhenExists_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderDetailsResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(provider.Id.Value);
        result.BusinessName.Should().Be("Test Provider");
    }

    [Fact]
    public async Task GetProviderById_WithIncludeServices_ShouldReturnProviderWithServices()
    {
        // Arrange
        var (provider, services) = await CreateProviderWithServicesAsync(serviceCount: 3);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}?includeServices=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderDetailsResponse>(response);
        result.Should().NotBeNull();
        result!.Services.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetProviderById_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/providers/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region SearchProviders Tests

    [Fact]
    public async Task SearchProviders_WithSearchTerm_ShouldReturnMatchingProviders()
    {
        // Arrange
        await CreateAndAuthenticateAsProviderAsync("Beauty Salon Alpha", "alpha@test.com");
        await CreateAndAuthenticateAsProviderAsync("Beauty Spa Beta", "beta@test.com");
        await CreateAndAuthenticateAsProviderAsync("Hair Studio Gamma", "gamma@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/search?searchTerm=Beauty");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ProviderSearchResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().OnlyContain(p => p.BusinessName.Contains("Beauty"));
    }

    [Fact]
    public async Task SearchProviders_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange - Create 5 providers
        for (int i = 0; i < 5; i++)
        {
            await CreateAndAuthenticateAsProviderAsync($"Provider {i}", $"provider{i}@test.com");
        }

        // Act
        var response = await GetAsync("/api/v1/providers/search?pageNumber=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ProviderSearchResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task SearchProviders_WithoutSearchTerm_ShouldReturnAllProviders()
    {
        // Arrange
        await CreateAndAuthenticateAsProviderAsync("Provider A", "a@test.com");
        await CreateAndAuthenticateAsProviderAsync("Provider B", "b@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ProviderSearchResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region GetProvidersByLocation Tests

    [Fact]
    public async Task GetProvidersByLocation_WithValidCoordinates_ShouldReturnNearbyProviders()
    {
        // Arrange - This test assumes providers have location data
        await CreateAndAuthenticateAsProviderAsync("Local Salon", "local@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/by-location?latitude=35.6892&longitude=51.3890&radiusKm=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<PagedResult<ProviderLocationResponse>>(response);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProvidersByLocation_WithInvalidCoordinates_ShouldReturn400BadRequest()
    {
        // Act
        var response = await GetAsync("/api/v1/providers/by-location?latitude=999&longitude=999&radiusKm=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ActivateProvider Tests

    [Fact]
    public async Task ActivateProvider_AsAdmin_ShouldReturn200OK()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");

        // Change to admin authentication
        AuthenticateAsTestAdmin();

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/activate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<MessageResponse>(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("activated");
    }

    [Fact]
    public async Task ActivateProvider_AsNonAdmin_ShouldReturn403Forbidden()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");

        // Act - Still authenticated as provider (not admin)
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/activate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateProvider_WhenNotExists_ShouldReturn404NotFound()
    {
        // Arrange
        AuthenticateAsTestAdmin();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{nonExistentId}/activate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetProvidersByStatus Tests

    [Fact]
    public async Task GetProvidersByStatus_AsAdmin_ShouldReturnProvidersWithStatus()
    {
        // Arrange
        await CreateAndAuthenticateAsProviderAsync("Active Provider", "active@test.com");

        AuthenticateAsTestAdmin();

        // Act
        var response = await GetAsync($"/api/v1/providers/by-status/{ProviderStatus.Active}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<List<ProviderSummaryResponse>>(response);
        result.Should().NotBeNull();
        result.Should().AllSatisfy(p => p.Status.Should().Be(ProviderStatus.Active.ToString()));
    }

    [Fact]
    public async Task GetProvidersByStatus_AsNonAdmin_ShouldReturn403Forbidden()
    {
        // Arrange
        await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");

        // Act - Authenticated as provider, not admin
        var response = await GetAsync($"/api/v1/providers/by-status/{ProviderStatus.Active}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProvidersByStatus_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange - No authentication

        // Act
        var response = await GetAsync($"/api/v1/providers/by-status/{ProviderStatus.Active}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProvidersByStatus_WithMaxResults_ShouldRespectLimit()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await CreateAndAuthenticateAsProviderAsync($"Provider {i}", $"provider{i}@test.com");
        }

        AuthenticateAsTestAdmin();

        // Act
        var response = await GetAsync($"/api/v1/providers/by-status/{ProviderStatus.Active}?maxResults=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<List<ProviderSummaryResponse>>(response);
        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(5);
    }

    #endregion
}
