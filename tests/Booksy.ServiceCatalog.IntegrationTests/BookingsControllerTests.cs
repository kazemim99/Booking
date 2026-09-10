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

        // Business hours come from the fixture (09:00-17:00, every day). This test used to
        // set them again here, which meant calling SetBusinessHours TWICE on the same tracked
        // Provider in one DbContext: the second call removes seven already-persisted owned
        // BusinessHours rows and adds seven new ones, and EF then reports
        // "expected to affect 1 row(s), but actually affected 0" (the owned child-collection
        // remove-then-add fragility recorded in FOLLOW-UPS). Nothing in production calls it
        // twice in one unit of work, and the booking below targets a Monday either way, so
        // the second call bought nothing and only broke the arrange.

        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var staff = provider;
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
        var staff = provider;
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
        var staff = provider;

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
        var staff = provider;
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
        // The action answers with its own MessageResponse payload; deserialising into the REQUEST
        // type discarded it. `response.Message` is the envelope's generic "Request completed
        // successfully" (ApiResponseMiddleware), never the action's text — so this assertion was
        // reading the wrapper and could not have passed whatever the endpoint said.
        var response = await PostAsJsonAsync<CancelBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/cancel", request);

        // Assert
        response.Errors.Should().BeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Message.Should().Contain("cancelled successfully");

        // Verify booking is cancelled in database. The tracker is cleared first: the cancellation
        // happened in the request's own DbContext, so EF's identity map would otherwise hand back
        // the instance this test arranged, still Requested.
        DbContext.ChangeTracker.Clear();
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

        // 2 PM on the next weekday at least 5 days out. Two traps here: `UtcNow.AddDays(5)`
        // keeps the current time-of-day, so `.AddHours(14)` lands in the early hours of the
        // FOLLOWING day (4 AM when run mid-afternoon) rather than 2 PM; and the test provider
        // opens Mon-Fri only, so a fixed offset can fall on a weekend. Both put the new time
        // outside business hours, which now surfaces as a 409 — previously invisible because
        // the endpoint 404'd before it ever reached validation.
        var newStartTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(5), 14);

        var request = new RescheduleBookingRequest
        {
            NewStartTime = newStartTime,
            Reason = "Need different time"
        };

        // Act
        // See CancelBooking_AsCustomer: the action's message lives in the payload, not the envelope.
        var response = await PostAsJsonAsync<RescheduleBookingRequest, BookingMessagePayload>(
            $"/api/v1/bookings/{booking.Id.Value}/reschedule", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.Message.Should().Contain("rescheduled successfully");

        // Verify old booking is marked as rescheduled
        // See CancelBooking_AsCustomer: read the row, not this test's tracked instance.
        DbContext.ChangeTracker.Clear();
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
        // The salon's service-providing MEMBER, not the salon itself — see
        // GetBookableMemberIdAsync. A booking held against provider.Id is one that no
        // bookable resource can see, so the slot it should occupy still reads as free.
        var staffId = await GetBookableMemberIdAsync(provider);
        var bookingPolicy = service.BookingPolicy ?? BookingPolicy.Default;

        var booking = Booking.CreateBookingRequest(
            Core.Domain.ValueObjects.UserId.From(customerId),
            provider.Id,
            service.Id,
            staffId,
            startTime,
            service.Duration,
            service.BasePrice,
            bookingPolicy,
            "Test booking");

        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();

        return booking;
    }

    /// <summary>
    /// The first Monday-Friday date on or after <paramref name="from"/>, at <paramref name="hour"/>.
    /// Keeps time-dependent assertions off the calendar: the test provider opens Mon-Fri only.
    /// </summary>
    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);

        return day.AddHours(hour);
    }

    // The private CreateTestProviderWithServicesAsync() that used to sit here SHADOWED the
    // one on ServiceCatalogIntegrationTestBase. It opened Monday-Friday only and created a
    // Provider + Service and nothing else, so every fix to the shared fixture silently missed
    // this class: the salon had no service-providing member, its service stayed in Draft, and
    // a test picking "three days from now" landed on a Saturday the salon was closed. Deleted
    // in favour of the base helper, which builds a genuinely bookable salon.

    private async Task<Domain.Aggregates.Service> GetFirstServiceForProviderAsync(Guid providerId)
    {
        var services = await GetProviderServicesAsync(providerId);
        return services.First();
    }

    #endregion
}

/// <summary>The payload shape of the cancel and reschedule actions (BookingsController's MessageResponse).</summary>
public sealed record BookingMessagePayload
{
    public string Message { get; init; } = string.Empty;
}
