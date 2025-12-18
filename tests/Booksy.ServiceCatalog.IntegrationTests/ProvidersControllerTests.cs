using Booksy.Core.Application.DTOs;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using System.Net.Http.Json;

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
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId);
        // Arrange
        var request = new RegisterProviderRequest
        {
            OwnerId = userId,
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
        var response = await PostAsJsonAsync<RegisterProviderRequest,ProviderDetailsResponse>("/api/v1/providers/register", request);

        // Assert
        response.Error.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        response.Data.Should().NotBeNull();
        response.Data!.BusinessName.Should().Be(request.BusinessName);
        response.Data.Type.Should().Be(request.Type.ToString());
        response.Data.Status.Should().Be(ProviderStatus.PendingVerification.ToString());

        // Verify in database
        await AssertProviderExistsAsync(response.Data.Id);
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
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId);
        // Arrange
        var request = new RegisterProviderRequest
        {
            OwnerId = userId,
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
        AuthenticateAsUser(userId, "owner@test.com");

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
                new TeamMemberRequest { Name = "John Doe", Email = "john@test.com", PhoneNumber = "1234567890", CountryCode = "+98", Position = "Owner", IsOwner = false }
            }
        };

        // Act
        var result = await PostAsJsonAsync<RegisterProviderFullRequest, ProviderFullRegistrationResponse>("/api/v1/providers/register-full", request);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        result.Data.Should().NotBeNull();
        result.Data!.BusinessName.Should().Be("Full Service Salon");
        result.Data.Status.Should().Be(ProviderStatus.PendingVerification.ToString());
        result.Data.ServicesCount.Should().Be(1);
        result.Data.StaffCount.Should().Be(1);

        // Verify in database
        await AssertProviderExistsAsync(result.Data.ProviderId);
    }

    [Fact]
    public async Task RegisterProviderFull_WithoutAuthentication_ShouldReturn401Unauthorized()
    {

        // Arrange - No authentication
        LogOut();
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

        AuthenticateAsUser(authenticatedUserId, "user@test.com");

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
        result.Items.Should().Contain(p => p.BusinessName.Contains("Beauty"));
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
 

    #endregion

    #region ActivateProvider Tests

    [Fact]
    public async Task ActivateProvider_AsAdmin_ShouldReturn200OK()
    {
        // Arrange - Create an inactive provider
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "test@provider.com");

        // Deactivate the provider first (since CreateAndAuthenticateAsProviderAsync sets it to Active)
        provider.Deactivate("Test deactivation for activation test");
        DbContext.Set<Provider>().Update(provider);
        await DbContext.SaveChangesAsync();

        // Change to admin authentication
        AuthenticateAsTestAdmin();

        // Act
        var response = await PostAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/activate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Data.Should().NotBeNull();
        response.Message.Should().NotBeNullOrEmpty();
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
        LogOut();
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

    #region GetCurrentProviderStatus Tests

    [Fact]
    public async Task GetCurrentProviderStatus_WithAuthenticatedProviderUser_ShouldReturn200OK()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "provider@test.com", userId);

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderStatusResponse>(response);
        result.Should().NotBeNull();
        result!.ProviderId.Should().Be(provider.Id.Value);
        result.UserId.Should().Be(userId);
        result.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrentProviderStatus_WithDraftedStatus_ShouldReturnDraftedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Drafted Provider", ProviderStatus.Drafted);
        AuthenticateAsUser(userId, "drafted@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderStatusResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProviderStatus.Drafted.ToString());
        result.ProviderId.Should().Be(provider.Id.Value);
    }

    [Fact]
    public async Task GetCurrentProviderStatus_WithPendingVerificationStatus_ShouldReturnPendingVerificationStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Pending Provider", ProviderStatus.PendingVerification);
        AuthenticateAsUser(userId, "pending@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderStatusResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProviderStatus.PendingVerification.ToString());
    }

    [Fact]
    public async Task GetCurrentProviderStatus_WithActiveStatus_ShouldReturnActiveStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Active Provider", ProviderStatus.Active);
        AuthenticateAsUser(userId, "active@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderStatusResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProviderStatus.Active.ToString());
    }

    [Fact]
    public async Task GetCurrentProviderStatus_WithInactiveStatus_ShouldReturnInactiveStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Inactive Provider", ProviderStatus.Inactive);
        AuthenticateAsUser(userId, "inactive@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderStatusResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProviderStatus.Inactive.ToString());
    }

    [Fact]
    public async Task GetCurrentProviderStatus_WithSuspendedStatus_ShouldReturnSuspendedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Suspended Provider", ProviderStatus.Suspended);
        AuthenticateAsUser(userId, "suspended@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderStatusResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProviderStatus.Suspended.ToString());
    }

    [Fact]
    public async Task GetCurrentProviderStatus_WithoutProviderRecord_ShouldReturn404NotFound()
    {
        // Arrange - Authenticate as user without a provider record
        var userId = Guid.NewGuid();
        AuthenticateAsUser(userId, "noprovider@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Provider record not found");
    }

    [Fact]
    public async Task GetCurrentProviderStatus_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange - No authentication
        LogOut();

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentProviderStatus_MultipleProviders_ShouldReturnOnlyCurrentUserProvider()
    {


        // Arrange - Create multiple providers
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var provider1 = await CreateProviderWithStatusAsync(user1Id, "Provider 1", ProviderStatus.Active);
        var provider2 = await CreateProviderWithStatusAsync(user2Id, "Provider 2", ProviderStatus.Drafted);

        // Authenticate as user 1
        AuthenticateAsUser(user1Id, "user1@test.com");

        // Act
        var response = await GetAsync("/api/v1/providers/current/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetResponseAsync<ProviderStatusResponse>(response);
        result.Should().NotBeNull();
        result!.ProviderId.Should().Be(provider1.Id.Value);
        result.ProviderId.Should().NotBe(provider2.Id.Value);
        result.UserId.Should().Be(user1Id);
        result.Status.Should().Be(ProviderStatus.Active.ToString());
    }

    #endregion

    #region Image Upload Tests

    [Fact]
    public async Task UploadProfileImage_WithValidImage_ShouldReturn200OkWithImageUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Test Provider", ProviderStatus.Active);
        AuthenticateAsUser(userId, "provider@test.com");

        // Create a test image file (1x1 PNG)
        var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "test.png");

        // Act
        var response = await Client.PostAsync("/api/v1/providers/profile/image", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>();
        result.Should().NotBeNull();
        result!.ImageUrl.Should().NotBeNullOrEmpty();
        result.ImageUrl.Should().Contain("/uploads/providers/");
        result.ImageUrl.Should().Contain(provider.Id.Value.ToString());
    }

    [Fact]
    public async Task UploadProfileImage_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "test.png");

        // Act
        var response = await Client.PostAsync("/api/v1/providers/profile/image", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadProfileImage_WithEmptyFile_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Test Provider", ProviderStatus.Active);
        AuthenticateAsUser(userId, "provider@test.com");

        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(Array.Empty<byte>());
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "empty.png");

        // Act
        var response = await Client.PostAsync("/api/v1/providers/profile/image", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("No image file provided");
    }

    [Fact]
    public async Task UploadBusinessLogo_WithValidImage_ShouldReturn200OkWithImageUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Test Provider", ProviderStatus.Active);
        AuthenticateAsUser(userId, "provider@test.com");

        // Create a test image file (1x1 PNG)
        var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "image", "logo.png");

        // Act
        var response = await Client.PostAsync("/api/v1/providers/business/logo", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>();
        result.Should().NotBeNull();
        result!.ImageUrl.Should().NotBeNullOrEmpty();
        result.ImageUrl.Should().Contain("/uploads/providers/");
        result.ImageUrl.Should().Contain(provider.Id.Value.ToString());
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ShouldReturn204NoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Test Provider", ProviderStatus.Active);
        AuthenticateAsUser(userId, "provider@test.com");

        var request = new
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            ProfileImageUrl = "/uploads/providers/test/profile.png"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/v1/providers/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            ProfileImageUrl = "/uploads/providers/test/profile.png"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/v1/providers/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateBusinessInfo_WithValidData_ShouldReturn204NoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Test Provider", ProviderStatus.Active);
        AuthenticateAsUser(userId, "provider@test.com");

        var request = new
        {
            BusinessName = "Updated Business Name",
            Description = "Updated description",
            OwnerFirstName = "John",
            OwnerLastName = "Doe",
            PhoneNumber = "+989121234567",
            Email = "business@example.com",
            Website = "https://www.updated.com",
            LogoUrl = "/uploads/providers/test/logo.png"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/v1/providers/business", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateBusinessInfo_WithInvalidPhoneNumber_ShouldReturn400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var provider = await CreateProviderWithStatusAsync(userId, "Test Provider", ProviderStatus.Active);
        AuthenticateAsUser(userId, "provider@test.com");

        var request = new
        {
            BusinessName = "Updated Business Name",
            Description = "Updated description",
            OwnerFirstName = "John",
            OwnerLastName = "Doe",
            PhoneNumber = "invalid",
            Email = "business@example.com"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/v1/providers/business", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Response Models

    private class UploadImageResponse
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
    }

    #endregion
}
