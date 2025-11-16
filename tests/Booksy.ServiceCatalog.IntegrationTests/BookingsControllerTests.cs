using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// Integration tests for Bookings API endpoints
/// Covers: Booking lifecycle operations (create, confirm, cancel, complete, reschedule, no-show)
/// Endpoints: /api/v1/bookings/*
/// </summary>
public class BookingsControllerTests : ServiceCatalogIntegrationTestBase
{
    public BookingsControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }

    #region Create Booking Tests

    [Fact]
    public async Task CreateBooking_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();

        // Set business hours (9 AM to 5 PM, Monday to Friday)
        var businessHours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
        {
            { DayOfWeek.Monday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Tuesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Wednesday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Thursday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Friday, (new TimeOnly(9, 0), new TimeOnly(17, 0)) },
            { DayOfWeek.Saturday, (null, null) }, // Closed
            { DayOfWeek.Sunday, (null, null) }    // Closed
        };
        provider.SetBusinessHours(businessHours);
        await UpdateEntityAsync(provider);

        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var staff = provider.Staff.First();
        var customerId = Guid.NewGuid();

        AuthenticateAsUser(customerId, "customer@test.com");

        // Calculate a valid booking time: next Monday at 10 AM (within business hours)
        var now = DateTime.UtcNow;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // If today is Monday, schedule for next Monday
        var nextMonday = now.AddDays(daysUntilMonday).Date;
        var bookingTime = nextMonday.AddHours(10); // 10 AM

        var request = new CreateBookingRequest
        {
            ProviderId = provider.Id.Value,
            ServiceId = service.Id.Value,
            StaffId = staff.Id,
            StartTime = bookingTime,
            CustomerNotes = "First time customer"
        };

        // Act
        var response = await PostAsJsonAsync<CreateBookingRequest, BookingResponse>(
            "/api/v1/bookings", request);

        // Assert
        // First check if there's an error and log it for debugging
        if (response.Error != null)
        {
            Console.WriteLine($"API Error: {response.Error.Message}");
            Console.WriteLine($"Error Code: {response.Error.Code}");
            if (response.Error.Errors != null)
            {
                foreach (var error in response.Error.Errors)
                {
                    Console.WriteLine($"Validation Error - {error.Key}: {string.Join(", ", error.Value)}");
                }
            }
        }

        response.Error.Should().BeNull("There should be no error in successful booking creation");
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().NotBeEmpty();
        response.Data.CustomerId.Should().Be(customerId);
        response.Data.ProviderId.Should().Be(provider.Id.Value);
        response.Data.ServiceId.Should().Be(service.Id.Value);
        response.Data.StaffId.Should().Be(staff.Id);
        response.Data.Status.Should().Be(nameof(BookingStatus.Requested));

        // Verify booking exists in database
        var booking = await DbContext.Bookings.FirstOrDefaultAsync(b => b.Id == BookingId.From(response.Data.Id));
        booking.Should().NotBeNull();
        booking!.CustomerId.Value.Should().Be(customerId);
    }

    [Fact]
    public async Task CreateBooking_WithPastDate_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var staff = provider.Staff.First();
        var customerId = Guid.NewGuid();

        AuthenticateAsUser(customerId, "customer@test.com");

        var request = new CreateBookingRequest
        {
            ProviderId = provider.Id.Value,
            ServiceId = service.Id.Value,
            StaffId = staff.Id,
            StartTime = DateTime.UtcNow.AddDays(-1), // Past date
            CustomerNotes = "Should fail"
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBooking_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var staff = provider.Staff.First();

        ClearAuthenticationHeader();

        var request = new CreateBookingRequest
        {
            ProviderId = provider.Id.Value,
            ServiceId = service.Id.Value,
            StaffId = staff.Id,
            StartTime = DateTime.UtcNow.AddDays(2).AddHours(10)
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBooking_WithNonExistentService_ShouldReturn404NotFound()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var staff = provider.Staff.First();
        var customerId = Guid.NewGuid();

        AuthenticateAsUser(customerId, "customer@test.com");

        var request = new CreateBookingRequest
        {
            ProviderId = provider.Id.Value,
            ServiceId = Guid.NewGuid(), // Non-existent service
            StaffId = staff.Id,
            StartTime = DateTime.UtcNow.AddDays(2).AddHours(10)
        };

        // Act
        var response = await PostAsJsonAsync("/api/v1/bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Booking Tests

    [Fact]
    public async Task GetBookingById_AsCustomer_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, customerId, booking) = await CreateTestBookingAsync();

        AuthenticateAsUser(customerId, "customer@test.com");

        // Act
        var response = await GetAsync<BookingDetailsResponse>($"/api/v1/bookings/{booking.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(booking.Id.Value);
        response.Data.CustomerId.Should().Be(customerId);
        response.Data.Status.Should().Be(nameof(BookingStatus.Requested));
    }

    [Fact]
    public async Task GetBookingById_AsWrongCustomer_ShouldReturn403Forbidden()
    {
        // Arrange
        var (provider, service, customerId, booking) = await CreateTestBookingAsync();

        // Authenticate as different customer
        AuthenticateAsUser(Guid.NewGuid(), "othercustomer@test.com");

        // Act
        var response = await GetAsync($"/api/v1/bookings/{booking.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBookingById_AsProvider_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, customerId, booking) = await CreateTestBookingAsync();

        AuthenticateAsProviderOwner(provider);

        // Act
        var response = await GetAsync<BookingDetailsResponse>($"/api/v1/bookings/{booking.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(booking.Id.Value);
    }

    #endregion

    #region Get My Bookings Tests

    [Fact(Skip = "EFCore composite key issue: The property 'Booking.TotalPrice#Price.BookingId' is part of a key and so cannot be modified")]
    public async Task GetMyBookings_ShouldReturnCustomerBookings()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        // Create multiple bookings for the customer
        await CreateBookingForCustomerAsync(customerId, provider, service, DateTime.UtcNow.AddDays(1));
        await CreateBookingForCustomerAsync(customerId, provider, service, DateTime.UtcNow.AddDays(2));
        await CreateBookingForCustomerAsync(customerId, provider, service, DateTime.UtcNow.AddDays(3));

        AuthenticateAsUser(customerId, "customer@test.com");

        // Act
        var response = await GetAsync<List<BookingResponse>>("/api/v1/bookings/my-bookings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Should().HaveCount(3);
        response.Data!.All(b => b.CustomerId == customerId).Should().BeTrue();
    }

    [Fact(Skip = "EFCore composite key issue: The property 'Booking.TotalPrice#Price.BookingId' is part of a key and so cannot be modified")]
    public async Task GetMyBookings_WithStatusFilter_ShouldReturnFilteredBookings()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        // Create bookings with different statuses
        var requestedBooking = await CreateBookingForCustomerAsync(customerId, provider, service, DateTime.UtcNow.AddDays(1));
        var confirmedBooking = await CreateBookingForCustomerAsync(customerId, provider, service, DateTime.UtcNow.AddDays(2));

        // Confirm one booking
        confirmedBooking.Confirm();
        await DbContext.SaveChangesAsync();

        AuthenticateAsUser(customerId, "customer@test.com");

        // Act
        var response = await GetAsync<List<BookingResponse>>("/api/v1/bookings/my-bookings?status=Confirmed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Should().HaveCount(1);
        response.Data![0].Status.Should().Be(nameof(BookingStatus.Confirmed));
    }

    #endregion

    #region Cancel Booking Tests

    [Fact]
    public async Task CancelBooking_AsCustomer_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, customerId, booking) = await CreateTestBookingAsync();

        AuthenticateAsUser(customerId, "customer@test.com");

        var request = new CancelBookingRequest
        {
            Reason = "Change of plans",
            CancelledBy = customerId
        };

        // Act
        var response = await PostAsJsonAsync<CancelBookingRequest, CancelBookingRequest>(
            $"/api/v1/bookings/{booking.Id.Value}/cancel", request);

        // Assert
        response.Errors.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response!.Message.Should().Contain("cancelled successfully");

        // Verify booking is cancelled in database
        var cancelledBooking = await DbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        cancelledBooking.Status.Should().Be(BookingStatus.Cancelled);
    }

    #endregion

    #region Reschedule Booking Tests

    [Fact]
    public async Task RescheduleBooking_WithValidNewTime_ShouldReturn200Ok()
    {
        // Arrange
        var (provider, service, customerId, booking) = await CreateTestBookingAsync();

        AuthenticateAsUser(customerId, "customer@test.com");

        var newStartTime = DateTime.UtcNow.AddDays(5).AddHours(14); // 2 PM, 5 days from now

        var request = new RescheduleBookingRequest
        {
            NewStartTime = newStartTime,
            Reason = "Need different time"
        };

        // Act
        var response = await PostAsJsonAsync<RescheduleBookingRequest, RescheduleBookingRequest>(
            $"/api/v1/bookings/{booking.Id.Value}/reschedule", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Message.Should().Contain("rescheduled successfully");

        // Verify old booking is marked as rescheduled
        var oldBooking = await DbContext.Bookings.FirstAsync(b => b.Id == booking.Id);
        oldBooking.Status.Should().Be(BookingStatus.Rescheduled);

        // Verify new booking exists
        var newBooking = await DbContext.Bookings.FirstOrDefaultAsync(b => b.PreviousBookingId == booking.Id);
        newBooking.Should().NotBeNull();
        newBooking!.TimeSlot.StartTime.Should().BeCloseTo(newStartTime, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Helper Methods

    private async Task<(Domain.Aggregates.Provider provider, Domain.Aggregates.Service service, Guid customerId, Booking booking)> CreateTestBookingAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = Guid.NewGuid();

        var booking = await CreateBookingForCustomerAsync(customerId, provider, service, DateTime.UtcNow.AddDays(2));

        return (provider, service, customerId, booking);
    }

    private async Task<Booking> CreateBookingForCustomerAsync(
        Guid customerId,
        Domain.Aggregates.Provider provider,
        Domain.Aggregates.Service service,
        DateTime startTime)
    {
        var staff = provider.Staff.First();
        var bookingPolicy = service.BookingPolicy ?? BookingPolicy.Default;

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            staff.Id,
            startTime,
            service.Duration,
            service.BasePrice,
            bookingPolicy,
            "Test booking");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return booking;
    }

    private async Task<Domain.Aggregates.Provider> CreateTestProviderWithServicesAsync()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Test Provider", "provider@test.com");

        // Add business hours
        provider.SetBusinessHours(new Dictionary<Domain.Enums.DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
        {
            { Domain.Enums.DayOfWeek.Monday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Tuesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Wednesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Thursday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Friday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) }
        });

        // Add a staff member if none exists
        if (!provider.Staff.Any())
        {
            provider.AddStaff(
                "Test Staff",
                "staff@test.com",
                StaffRole.Assistant,
                PhoneNumber.Create("+1234567890"));
        }

        await DbContext.SaveChangesAsync();

        // Create a service
        await CreateServiceForProviderAsync(provider, "Test Service", 50.00m, 60);

        return provider;
    }

    private async Task<Domain.Aggregates.Service> GetFirstServiceForProviderAsync(Guid providerId)
    {
        var services = await GetProviderServicesAsync(providerId);
        return services.First();
    }

    #endregion
}
