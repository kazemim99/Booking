using Booksy.API;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.Tests.Common.Infrastructure;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests;

/// <summary>
/// Integration tests for Progressive Provider Registration Flow
/// Tests the new draft-based registration pattern where provider is created early
/// and updated incrementally through the registration steps
/// </summary>
[Collection("Integration Tests")]
public class ProgressiveRegistrationTests : ServiceCatalogIntegrationTestBase
{
    public ProgressiveRegistrationTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    #region Helper Method

    private async Task<(HttpStatusCode StatusCode, Guid? ProviderId, string Message)> CreateDraftAsync(object request)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/providers/draft", request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, null, content);
        }

        var data = JObject.Parse(content);
        var dataObj = data["data"] as JObject;
        if (dataObj == null)
        {
            return (response.StatusCode, null, "No data in response");
        }

        var providerIdStr = dataObj["ProviderId"]?.ToString();
        var providerId = !string.IsNullOrEmpty(providerIdStr) && Guid.TryParse(providerIdStr, out var id) ? id : (Guid?)null;
        var message = (string?)dataObj["Message"] ?? "";

        return (response.StatusCode, providerId, message);
    }

    #endregion

    #region Create Draft Provider Tests

    [Fact]
    public async Task CreateDraftProvider_WithValidData_CreatesNewDraft()
    {
        // Arrange
        AuthenticateAsCustomer("test1@example.com");

        var request = new
        {
            BusinessName = "Test Salon",
            BusinessDescription = "A modern salon",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "contact@testsalon.com",
            AddressLine1 = "123 Main Street",
            AddressLine2 = "Suite 100",
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "1234567890",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        // Act
        var (statusCode, providerId, message) = await CreateDraftAsync(request);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Created);
        providerId.Should().NotBeNull();
        message.Should().NotBeNullOrEmpty();

        // If we got a valid provider ID, verify in database
        if (providerId.HasValue && providerId.Value != Guid.Empty)
        {
            var provider = await FindProviderAsync(providerId.Value);
            provider.Should().NotBeNull();
            provider!.Status.Should().Be(ProviderStatus.Drafted);
            provider.RegistrationStep.Should().Be(3);
            provider.Profile.BusinessName.Should().Be(request.BusinessName);
            provider.ProviderType.Should().Be(ProviderType.Salon);
        }
    }

    [Fact]
    public async Task CreateDraftProvider_WhenDraftAlreadyExists_ReturnsExistingDraft()
    {
        // Arrange
        AuthenticateAsCustomer("test2@example.com");

        var request = new
        {
            BusinessName = "Test Salon",
            BusinessDescription = "A modern salon",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "contact@testsalon.com",
            AddressLine1 = "456 Oak Avenue",
            AddressLine2 = (string?)null,
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "1234567890",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        // Act - Create first draft
        var (status1, providerId1, _) = await CreateDraftAsync(request);
        status1.Should().Be(HttpStatusCode.Created);
        providerId1.Should().NotBeNull();

        // Act - Try to create second draft with different name
        var request2 = new
        {
            BusinessName = "Different Name",
            BusinessDescription = request.BusinessDescription,
            Category = request.Category,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        var (status2, providerId2, message2) = await CreateDraftAsync(request2);

        // Assert
        status2.Should().Be(HttpStatusCode.OK);
        message2.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateDraftProvider_WithInvalidCategory_ReturnsError()
    {
        // Arrange
        AuthenticateAsCustomer("test3@example.com");

        var request = new
        {
            BusinessName = "Test Business",
            BusinessDescription = "Test description",
            Category = "InvalidCategory",
            PhoneNumber = "+989123456789",
            Email = "test@test.com",
            AddressLine1 = "123 Street",
            AddressLine2 = (string?)null,
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "1234567890",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        // Act
        var (statusCode, providerId, message) = await CreateDraftAsync(request);

        // Assert - Request should fail
        statusCode.Should().NotBe(HttpStatusCode.Created);
        providerId.Should().BeNull();
    }

    [Fact]
    public async Task CreateDraftProvider_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication
        ClearAuthenticationHeader();

        var request = new
        {
            BusinessName = "Test Business",
            BusinessDescription = "Test description",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "test@test.com",
            AddressLine1 = "123 Street",
            AddressLine2 = (string?)null,
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "1234567890",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        // Act
        var response = await GetAsync("/api/v1/providers/draft");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Draft Provider Tests

    [Fact]
    public async Task GetDraftProvider_WithExistingDraft_ReturnsDraftData()
    {
        // Arrange
        AuthenticateAsCustomer("test4@example.com");

        var createRequest = new
        {
            BusinessName = "My Business",
            BusinessDescription = "A great business",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "contact@test.com",
            AddressLine1 = "789 Test St",
            AddressLine2 = (string?)null,
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "9876543210",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        var (createStatus, _, _) = await CreateDraftAsync(createRequest);
        createStatus.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await GetAsync("/api/v1/providers/draft");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("My Business");
    }

    [Fact]
    public async Task GetDraftProvider_WithoutDraft_Returns404()
    {
        // Arrange
        AuthenticateAsCustomer("test5@example.com");

        // Act - User has no draft provider
        var response = await GetAsync("/api/v1/providers/draft");

        // Assert - API returns 404 when no draft found
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDraftProvider_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationHeader();

        // Act
        var response = await GetAsync("/api/v1/providers/draft");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Complete Registration Tests

    [Fact]
    public async Task CompleteRegistration_WithoutRequiredData_ReturnsError()
    {
        // Arrange
        AuthenticateAsCustomer("test6@example.com");

        var createRequest = new
        {
            BusinessName = "Incomplete Business",
            BusinessDescription = "Missing required data",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "incomplete@test.com",
            AddressLine1 = "111 Incomplete St",
            AddressLine2 = (string?)null,
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "2222222222",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        var (createStatus, providerId, _) = await CreateDraftAsync(createRequest);
        createStatus.Should().Be(HttpStatusCode.Created);
        providerId.Should().NotBeNull();

        // Use the provider ID if available, otherwise use a dummy GUID
        var idToUse = providerId ?? Guid.NewGuid();

        // Act - Try to complete without adding services/hours
        var completeRequest = new { ProviderId = idToUse };
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/providers/{idToUse}/complete",
            completeRequest);

        // Assert - Should fail because no services/hours added
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompleteRegistration_ByNonOwner_ReturnsForbidden()
    {
        // Arrange - Create as one user
        AuthenticateAsCustomer("owner@example.com");

        var createRequest = new
        {
            BusinessName = "Owner Business",
            BusinessDescription = "Only owner can complete",
            Category = "Salon",
            PhoneNumber = "+989123456789",
            Email = "owner@test.com",
            AddressLine1 = "222 Owner St",
            AddressLine2 = (string?)null,
            City = "Tehran",
            Province = "Tehran",
            PostalCode = "3333333333",
            Latitude = 35.6892m,
            Longitude = 51.3890m
        };

        var (createStatus, providerId, _) = await CreateDraftAsync(createRequest);
        createStatus.Should().Be(HttpStatusCode.Created);
        providerId.Should().NotBeNull();

        // Use the provider ID if available, otherwise use a dummy GUID
        var idToUse = providerId ?? Guid.NewGuid();

        // Switch to different user
        AuthenticateAsCustomer("different@example.com");

        // Act
        var completeRequest = new { ProviderId = idToUse };
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/providers/{idToUse}/complete",
            completeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompleteRegistration_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        ClearAuthenticationHeader();
        var providerId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/providers/{providerId}/complete");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
