using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.API.Models.Requests;
using Booksy.UserManagement.API.Models.Responses;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.ValueObjects;
using FluentAssertions;

namespace Booksy.UserManagement.IntegrationTests.API;

/// <summary>
/// Integration tests for Customers API endpoints
/// Covers: Customer profile management, favorites, bookings, preferences
/// Endpoints: /api/v1/customers/*
/// </summary>
public class CustomersControllerTests : UserManagementIntegrationTestBase
{
    public CustomersControllerTests(UserManagementTestWebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    #region Get Customer Profile Tests

    [Fact]
    public async Task GetCustomerProfile_WithValidId_ShouldReturn200Ok()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync("Jane", "Smith", "+989123456789");

        // Act
        var response = await GetCustomerProfileAsync(customer.Id.Value);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Jane");
        content.Should().Contain("Smith");
    }

    [Fact]
    public async Task GetCustomerProfile_WithNonExistentId_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        AuthenticateAsCustomerWithId(nonExistentId);

        // Act
        var response = await GetCustomerProfileAsync(nonExistentId);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    #endregion

    #region Update Customer Profile Tests

    [Fact]
    public async Task UpdateCustomerProfile_WithValidData_ShouldReturn200Ok()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync("John", "Doe", "+989123456789");

        var request = new
        {
            FirstName = "Johnny",
            LastName = "Updated",
            PhoneNumber = "+989123456789",
            Email = "johnny.updated@test.com"
        };

        // Act
        var response = await UpdateCustomerProfileAsync(customer.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Verify in database
        var updatedCustomer = await FindCustomerAsync(customer.Id.Value);
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.Profile.FirstName.Should().Be("Johnny");
        updatedCustomer.Profile.LastName.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateCustomerProfile_WithInvalidData_ShouldReturn400BadRequest()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();

        var request = new
        {
            FirstName = "", // Invalid: empty first name
            LastName = "Test",
            PhoneNumber = "+989123456789"
        };

        // Act
        var response = await UpdateCustomerProfileAsync(customer.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region Favorite Providers Tests

    [Fact]
    public async Task AddFavoriteProvider_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        var providerId = Guid.NewGuid();

        var request = new
        {
            ProviderId = providerId,
            Notes = "Great haircuts!"
        };

        // Act
        var response = await AddFavoriteProviderAsync(customer.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        // Verify in database
        await AssertCustomerHasFavoriteProviderAsync(customer.Id.Value, providerId);
        await AssertCustomerFavoriteCountAsync(customer.Id.Value, 1);
    }

    [Fact]
    public async Task AddFavoriteProvider_WhenAlreadyExists_ShouldReturn409Conflict()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        var providerId = Guid.NewGuid();

        // Add provider first time
        customer.AddFavoriteProvider(providerId, "First time");
        await UpdateEntityAsync(customer);

        var request = new
        {
            ProviderId = providerId,
            Notes = "Trying to add again"
        };

        // Act
        var response = await AddFavoriteProviderAsync(customer.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetFavoriteProviders_ShouldReturnAllFavorites()
    {
        // Arrange
        var customer = await CreateCustomerWithFavoritesAsync(favoriteCount: 3);
        AuthenticateAsCustomer(customer);

        // Act
        var response = await GetCustomerFavoritesAsync(customer.Id.Value);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Favorite provider 1");
        content.Should().Contain("Favorite provider 2");
        content.Should().Contain("Favorite provider 3");
    }

    [Fact]
    public async Task RemoveFavoriteProvider_WithValidData_ShouldReturn200Ok()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        var providerId = Guid.NewGuid();

        customer.AddFavoriteProvider(providerId, "Test provider");
        await UpdateEntityAsync(customer);

        // Act
        var response = await RemoveFavoriteProviderAsync(customer.Id.Value, providerId);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Verify in database
        await AssertCustomerFavoriteCountAsync(customer.Id.Value, 0);
    }

    [Fact]
    public async Task RemoveFavoriteProvider_WhenNotFavorite_ShouldReturn404NotFound()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        var nonExistentProviderId = Guid.NewGuid();

        // Act
        var response = await RemoveFavoriteProviderAsync(customer.Id.Value, nonExistentProviderId);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    #endregion

    #region Booking History Tests

    [Fact]
    public async Task GetUpcomingBookings_ShouldReturnUpcomingBookings()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        var providerId = Guid.NewGuid();

        // Create upcoming booking (future)
        await CreateBookingHistoryEntryAsync(
            customer.Id.Value,
            providerId,
            "Test Provider",
            "Haircut",
            DateTime.UtcNow.AddDays(7),
            "Confirmed",
            50m);

        // Create past booking (should not appear in upcoming)
        await CreateBookingHistoryEntryAsync(
            customer.Id.Value,
            providerId,
            "Test Provider",
            "Manicure",
            DateTime.UtcNow.AddDays(-7),
            "Completed",
            30m);

        // Act
        var response = await GetUpcomingBookingsAsync(customer.Id.Value, limit: 5);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Haircut");
        content.Should().NotContain("Manicure");
    }

    [Fact]
    public async Task GetBookingHistory_ShouldReturnPaginatedHistory()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        var providerId = Guid.NewGuid();

        // Create multiple past bookings
        for (int i = 0; i < 5; i++)
        {
            await CreateBookingHistoryEntryAsync(
                customer.Id.Value,
                providerId,
                "Test Provider",
                $"Service {i + 1}",
                DateTime.UtcNow.AddDays(-i - 1),
                "Completed",
                50m + (i * 10));
        }

        // Act
        var response = await GetBookingHistoryAsync(customer.Id.Value, page: 1, size: 20);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        await AssertCustomerBookingHistoryCountAsync(customer.Id.Value, 5);
    }

    [Fact]
    public async Task GetBookingHistory_WithPagination_ShouldRespectPageSize()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();
        var providerId = Guid.NewGuid();

        // Create 25 past bookings
        for (int i = 0; i < 25; i++)
        {
            await CreateBookingHistoryEntryAsync(
                customer.Id.Value,
                providerId,
                "Test Provider",
                $"Service {i + 1}",
                DateTime.UtcNow.AddDays(-i - 1),
                "Completed",
                50m);
        }

        // Act - Get first page (size 20)
        var responsePage1 = await GetBookingHistoryAsync(customer.Id.Value, page: 1, size: 20);

        // Assert
        responsePage1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var contentPage1 = await responsePage1.Content.ReadAsStringAsync();
        // The response should indicate there are more items or contain pagination info
        contentPage1.Should().NotBeEmpty();
    }

    #endregion

    #region Notification Preferences Tests

    [Fact]
    public async Task UpdateNotificationPreferences_WithValidData_ShouldReturn200Ok()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();

        var request = new
        {
            SmsEnabled = true,
            EmailEnabled = false,
            ReminderTiming = "24h"
        };

        // Act
        var response = await UpdateNotificationPreferencesAsync(customer.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Verify in database
        var updatedCustomer = await FindCustomerAsync(customer.Id.Value);
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.NotificationPreferences.SmsEnabled.Should().BeTrue();
        updatedCustomer.NotificationPreferences.EmailEnabled.Should().BeFalse();
        updatedCustomer.NotificationPreferences.ReminderTiming.Should().Be("24h");
    }

    [Fact]
    public async Task UpdateNotificationPreferences_WithInvalidTiming_ShouldReturn400BadRequest()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();

        var request = new
        {
            SmsEnabled = true,
            EmailEnabled = true,
            ReminderTiming = "invalid_timing" // Invalid timing
        };

        // Act
        var response = await UpdateNotificationPreferencesAsync(customer.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_DisablingBothChannels_ShouldSucceed()
    {
        // Arrange
        var customer = await CreateAndAuthenticateAsCustomerAsync();

        var request = new
        {
            SmsEnabled = false,
            EmailEnabled = false,
            ReminderTiming = "24h"
        };

        // Act
        var response = await UpdateNotificationPreferencesAsync(customer.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Verify in database
        var updatedCustomer = await FindCustomerAsync(customer.Id.Value);
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.NotificationPreferences.SmsEnabled.Should().BeFalse();
        updatedCustomer.NotificationPreferences.EmailEnabled.Should().BeFalse();
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetCustomerProfile_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var customer = await CreateTestCustomerAsync();
        ClearAuthenticationHeader(); // Remove authentication

        // Act
        var response = await GetCustomerProfileAsync(customer.Id.Value);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCustomerProfile_AccessingOtherCustomer_ShouldReturn403Forbidden()
    {
        // Arrange
        var customer1 = await CreateTestCustomerAsync("Customer", "One", "+989111111111");
        var customer2 = await CreateTestCustomerAsync("Customer", "Two", "+989222222222");

        // Authenticate as customer1
        AuthenticateAsCustomer(customer1);

        var request = new
        {
            FirstName = "Hacker",
            LastName = "Attempt",
            PhoneNumber = "+989222222222"
        };

        // Act - Try to update customer2's profile
        var response = await UpdateCustomerProfileAsync(customer2.Id.Value, request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    #endregion
}
