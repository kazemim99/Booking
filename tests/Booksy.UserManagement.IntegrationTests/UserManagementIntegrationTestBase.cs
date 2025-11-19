// ========================================
// Booksy.UserManagement.IntegrationTests/UserManagementIntegrationTestBase.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.Tests.Common.Infrastructure;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.ReadModels;
using Booksy.UserManagement.Domain.ValueObjects;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net.Http.Json;

namespace Booksy.UserManagement.IntegrationTests;

/// <summary>
/// Base class for User Management integration tests
/// Inherits from generic IntegrationTestBase and provides UserManagement-specific helpers
/// </summary>
public abstract class UserManagementIntegrationTestBase
    : IntegrationTestBase<
        UserManagementTestWebApplicationFactory<Program>, // Use Program as entry point
        UserManagementDbContext,
        Program>
{
    public UserManagementIntegrationTestBase(UserManagementTestWebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    // ================================================
    // DATABASE CLEANUP (UserManagement Specific)
    // ================================================

    public override async Task CleanDatabaseAsync()
    {
        // Clean tables in correct order (respecting foreign keys)
        // Override if specific cleanup is needed
        await base.CleanDatabaseAsync();
    }

    // ================================================
    // USERMANAGEMENT SPECIFIC ENTITY HELPERS
    // ================================================

    /// <summary>
    /// Find a Customer by ID
    /// </summary>
    public async Task<Customer?> FindCustomerAsync(Guid customerId)
    {
        return await DbContext.Customers
            .Include(c => c.FavoriteProviders)
            .FirstOrDefaultAsync(c => c.Id == CustomerId.From(customerId));
    }

    /// <summary>
    /// Find a Customer by predicate
    /// </summary>
    public async Task<Customer?> FindCustomerAsync(Expression<Func<Customer, bool>> predicate)
    {
        return await DbContext.Customers
            .Include(c => c.FavoriteProviders)
            .FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Find a User by ID
    /// </summary>
    public async Task<User?> FindUserAsync(Guid userId)
    {
        return await DbContext.Users
            .FirstOrDefaultAsync(u => u.Id == UserId.From(userId));
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await DbContext.Customers
            .Include(c => c.FavoriteProviders)
            .ToListAsync();
    }

    /// <summary>
    /// Get customer booking history entries
    /// </summary>
    public async Task<List<CustomerBookingHistoryEntry>> GetCustomerBookingHistoryAsync(Guid customerId)
    {
        return await DbContext.CustomerBookingHistory
            .Where(cbh => cbh.CustomerId == CustomerId.From(customerId))
            .OrderByDescending(cbh => cbh.StartTime)
            .ToListAsync();
    }

    // ================================================
    // AUTHENTICATION HELPERS (UserManagement Specific)
    // ================================================

    /// <summary>
    /// Authenticate as a customer
    /// </summary>
    public void AuthenticateAsCustomer(Customer customer)
    {
        var testUser = new TestUser
        {
            UserId = customer.UserId.Value.ToString(),
            Email = customer.Profile.PhoneNumber?.Value ?? "customer@test.com", // Use phone as identifier
            Name = customer.Profile.GetFullName(),
            Role = "Customer",
            AdditionalClaims = new Dictionary<string, string>
            {
                { "customerId", customer.Id.Value.ToString() },
                { "user_type", "Customer" }
            }
        };

        _userContext.SetUser(testUser);
    }

    /// <summary>
    /// Authenticate as the customer who owns a specific ID
    /// </summary>
    public void AuthenticateAsCustomerWithId(Guid customerId, string email = "customer@test.com")
    {
        base.AuthenticateAsCustomer(customerId, email);
    }

    /// <summary>
    /// Authenticate as an admin user
    /// </summary>
    public void AuthenticateAsTestAdmin()
    {
        base.AuthenticateAsAdmin("admin@booksy.com");
    }

    /// <summary>
    /// Clear authentication
    /// </summary>
    public new void ClearAuthenticationHeader()
    {
        base.ClearAuthentication();
    }

    // ================================================
    // ASSERTION HELPERS (UserManagement Specific)
    // ================================================

    /// <summary>
    /// Assert that a customer exists in the database
    /// </summary>
    public async Task AssertCustomerExistsAsync(Guid customerId)
    {
        var customer = await FindCustomerAsync(customerId);
        customer.Should().NotBeNull($"Customer with ID {customerId} should exist");
    }

    /// <summary>
    /// Assert that a customer does not exist
    /// </summary>
    public async Task AssertCustomerNotExistsAsync(Guid customerId)
    {
        var customer = await FindCustomerAsync(customerId);
        customer.Should().BeNull($"Customer with ID {customerId} should not exist");
    }

    /// <summary>
    /// Assert that a customer has a specific number of favorites
    /// </summary>
    public async Task AssertCustomerFavoriteCountAsync(Guid customerId, int expectedCount)
    {
        var customer = await FindCustomerAsync(customerId);
        customer.Should().NotBeNull();
        customer!.FavoriteProviders.Should().HaveCount(expectedCount,
            $"Customer {customerId} should have {expectedCount} favorite providers");
    }

    /// <summary>
    /// Assert that a customer has a specific provider as favorite
    /// </summary>
    public async Task AssertCustomerHasFavoriteProviderAsync(Guid customerId, Guid providerId)
    {
        var customer = await FindCustomerAsync(customerId);
        customer.Should().NotBeNull();
        customer!.FavoriteProviders.Should().Contain(f => f.ProviderId == providerId,
            $"Customer {customerId} should have provider {providerId} as favorite");
    }

    /// <summary>
    /// Assert that a customer has booking history entries
    /// </summary>
    public async Task AssertCustomerBookingHistoryCountAsync(Guid customerId, int expectedCount)
    {
        var bookings = await GetCustomerBookingHistoryAsync(customerId);
        bookings.Should().HaveCount(expectedCount,
            $"Customer {customerId} should have {expectedCount} booking history entries");
    }

    // ================================================
    // SETUP HELPERS (UserManagement Specific)
    // ================================================

    /// <summary>
    /// Create a test customer
    /// </summary>
    public async Task<Customer> CreateTestCustomerAsync(
        string firstName = "John",
        string lastName = "Doe",
        string phoneNumber = "+989123456789")
    {
        var userId = UserId.From(Guid.NewGuid());
        var profile = UserProfile.Create(firstName, lastName, null, null, null);

        // Update profile with phone number
        var phone = PhoneNumber.Create(phoneNumber);
        profile.UpdateContactInfo(phone, null, null);

        var customer = Customer.Create(userId, profile);

        await CreateEntityAsync(customer);
        return customer;
    }

    /// <summary>
    /// Create a customer and authenticate as them in one call
    /// </summary>
    public async Task<Customer> CreateAndAuthenticateAsCustomerAsync(
        string firstName = "Test",
        string lastName = "Customer",
        string phoneNumber = "+989123456789")
    {
        var customer = await CreateTestCustomerAsync(firstName, lastName, phoneNumber);
        AuthenticateAsCustomer(customer);
        return customer;
    }

    /// <summary>
    /// Create a customer with favorite providers
    /// </summary>
    public async Task<Customer> CreateCustomerWithFavoritesAsync(
        int favoriteCount = 3)
    {
        var customer = await CreateTestCustomerAsync();

        for (int i = 0; i < favoriteCount; i++)
        {
            var providerId = Guid.NewGuid();
            customer.AddFavoriteProvider(providerId, $"Favorite provider {i + 1}");
        }

        await UpdateEntityAsync(customer);
        return customer;
    }

    /// <summary>
    /// Create a customer booking history entry
    /// </summary>
    public async Task<CustomerBookingHistoryEntry> CreateBookingHistoryEntryAsync(
        Guid customerId,
        Guid providerId,
        string providerName = "Test Provider",
        string serviceName = "Test Service",
        DateTime? startTime = null,
        string status = "Completed",
        decimal totalPrice = 100m)
    {
        var entry = CustomerBookingHistoryEntry.Create(
            Guid.NewGuid(), // bookingId
            CustomerId.From(customerId),
            providerId,
            providerName,
            serviceName,
            startTime ?? DateTime.UtcNow.AddDays(-7),
            status,
            totalPrice);

        await CreateEntityAsync(entry);
        return entry;
    }

    // ================================================
    // VALUE OBJECT HELPERS
    // ================================================

    /// <summary>
    /// Create a test CustomerId
    /// </summary>
    public CustomerId CreateCustomerId(Guid? id = null)
    {
        return CustomerId.From(id ?? Guid.NewGuid());
    }

    /// <summary>
    /// Create a test PhoneNumber
    /// </summary>
    public PhoneNumber CreatePhoneNumber(string number = "+989123456789")
    {
        return PhoneNumber.Create(number);
    }

    /// <summary>
    /// Create a test UserProfile
    /// </summary>
    public UserProfile CreateUserProfile(
        string firstName = "Test",
        string lastName = "User",
        string? middleName = null,
        DateTime? dateOfBirth = null,
        string? gender = null)
    {
        return UserProfile.Create(firstName, lastName, middleName, dateOfBirth, gender);
    }

    // ================================================
    // API ENDPOINT HELPERS
    // ================================================

    /// <summary>
    /// Get customer profile endpoint
    /// </summary>
    public async Task<HttpResponseMessage> GetCustomerProfileAsync(Guid customerId)
    {
        return await GetAsync($"/api/v1/customers/{customerId}/profile");
    }

    /// <summary>
    /// Update customer profile endpoint
    /// </summary>
    public async Task<HttpResponseMessage> UpdateCustomerProfileAsync(Guid customerId, object request)
    {
        return await Client.PutAsJsonAsync($"/api/v1/customers/{customerId}", request);
    }

    /// <summary>
    /// Get customer favorites endpoint
    /// </summary>
    public async Task<HttpResponseMessage> GetCustomerFavoritesAsync(Guid customerId)
    {
        return await GetAsync($"/api/v1/customers/{customerId}/favorites");
    }

    /// <summary>
    /// Add favorite provider endpoint
    /// </summary>
    public async Task<HttpResponseMessage> AddFavoriteProviderAsync(Guid customerId, object request)
    {
        return await Client.PostAsJsonAsync($"/api/v1/customers/{customerId}/favorites", request);
    }

    /// <summary>
    /// Remove favorite provider endpoint
    /// </summary>
    public async Task<HttpResponseMessage> RemoveFavoriteProviderAsync(Guid customerId, Guid providerId)
    {
        return await Client.DeleteAsync($"/api/v1/customers/{customerId}/favorites/{providerId}");
    }

    /// <summary>
    /// Get upcoming bookings endpoint
    /// </summary>
    public async Task<HttpResponseMessage> GetUpcomingBookingsAsync(Guid customerId, int limit = 5)
    {
        return await GetAsync($"/api/v1/customers/{customerId}/bookings/upcoming?limit={limit}");
    }

    /// <summary>
    /// Get booking history endpoint
    /// </summary>
    public async Task<HttpResponseMessage> GetBookingHistoryAsync(Guid customerId, int page = 1, int size = 20)
    {
        return await GetAsync($"/api/v1/customers/{customerId}/bookings/history?page={page}&size={size}");
    }

    /// <summary>
    /// Update notification preferences endpoint
    /// </summary>
    public async Task<HttpResponseMessage> UpdateNotificationPreferencesAsync(Guid customerId, object request)
    {
        return await Client.PatchAsync($"/api/v1/customers/{customerId}/preferences",
            JsonContent.Create(request));
    }
}
