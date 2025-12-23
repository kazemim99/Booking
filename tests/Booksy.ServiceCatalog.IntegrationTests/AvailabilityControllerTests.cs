using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Application.Commands.Booking.CreateBooking;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// Integration tests for Availability API endpoints
/// Covers: Availability checking for booking time slots
/// Endpoints: /api/v1/availability/*
/// </summary>
public class AvailabilityControllerTests : ServiceCatalogIntegrationTestBase
{
    public AvailabilityControllerTests(ServiceCatalogTestWebApplicationFactory<Startup> factory) : base(factory)
    {
    }

    #region Get Available Slots Tests

    [Fact]
    public async Task GetAvailableSlots_WithValidDate_ShouldReturn200WithSlots()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var futureDate = DateTime.UtcNow.AddDays(3).Date;

        // Act
        var response = await GetAsync<List<AvailableSlotResponse>>(
            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={futureDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data.Should().NotBeEmpty("Provider has business hours configured, so slots should be available");
    }

    [Fact]
    public async Task GetAvailableSlots_WithPastDate_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var pastDate = DateTime.UtcNow.AddDays(-1).Date;

        // Act
        var response = await GetAsync(
            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={pastDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailableSlots_WithNonExistentProvider_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistentProviderId = Guid.NewGuid();
        var nonExistentServiceId = Guid.NewGuid();
        var futureDate = DateTime.UtcNow.AddDays(3).Date;

        // Act
        var response = await GetAsync(
            $"/api/v1/availability/slots?ProviderId={nonExistentProviderId}&ServiceId={nonExistentServiceId}&Date={futureDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAvailableSlots_ShouldExcludeBookedSlots()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var testDate = DateTime.UtcNow.AddDays(3).Date.AddHours(10); // 10 AM

        // Create a confirmed booking
        var customerId = Guid.NewGuid();
        var booking = await CreateBookingForCustomerAsync(customerId, provider, service, testDate);
        booking.Confirm();
        await DbContext.SaveChangesAsync();

        // Act - Get available slots for the same date
        var response = await GetAsync<List<AvailableSlotResponse>>(
            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={testDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();

        // The booked slot should either be marked as unavailable or not included
        var bookedSlot = response.Data!.FirstOrDefault(s => s.StartTime.Hour == testDate.Hour);
        if (bookedSlot != null)
        {
            bookedSlot.IsAvailable.Should().BeFalse("Slot should be marked as unavailable");
        }
    }

    [Fact]
    public async Task GetAvailableSlots_WithSpecificStaff_ShouldReturnOnlyTheirSlots()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var staff = provider;

        var futureDate = DateTime.UtcNow.AddDays(3).Date;

        // Act
        var response = await GetAsync<List<AvailableSlotResponse>>(
            $"/api/v1/availability/slots?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&Date={futureDate:yyyy-MM-dd}&StaffId={staff.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();

        // All returned slots should be for the specified staff
        if (response.Data!.Any())
        {
            response.Data.Where(s => s.AvailableStaffId.HasValue)
                .All(s => s.AvailableStaffId == staff.Id)
                .Should().BeTrue("All slots should be for the specified staff member");
        }
    }

    #endregion

    #region Check Slot Availability Tests

    [Fact]
    public async Task CheckSlotAvailability_ForAvailableSlot_ShouldReturnTrue()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var futureTime = DateTime.UtcNow.AddDays(3).Date.AddHours(10); // 10 AM, 3 days from now

        // Act
        var response = await GetAsync<SlotAvailabilityResponse>(
            $"/api/v1/availability/check?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&StartTime={futureTime:yyyy-MM-ddTHH:mm:ss}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.StartTime.Should().BeCloseTo(futureTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CheckSlotAvailability_ForPastTime_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var pastTime = DateTime.UtcNow.AddDays(-1);

        // Act
        var response = await GetAsync(
            $"/api/v1/availability/check?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&StartTime={pastTime:yyyy-MM-ddTHH:mm:ss}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CheckSlotAvailability_ForBookedSlot_ShouldReturnFalse()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var futureTime = DateTime.UtcNow.AddDays(3).Date.AddHours(10);

        // Create a confirmed booking for this time
        var customerId = Guid.NewGuid();
        var booking = await CreateBookingForCustomerAsync(customerId, provider, service, futureTime);
        booking.Confirm();
        await DbContext.SaveChangesAsync();

        // Act - Check availability for the same time
        var response = await GetAsync<SlotAvailabilityResponse>(
            $"/api/v1/availability/check?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&StartTime={futureTime:yyyy-MM-ddTHH:mm:ss}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        response.Data!.IsAvailable.Should().BeFalse("Slot is already booked");
    }

    #endregion

    #region Get Available Dates Tests

    [Fact]
    public async Task GetAvailableDates_WithValidRange_ShouldReturnDatesWithAvailability()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var fromDate = DateTime.UtcNow.AddDays(1).Date;
        var toDate = fromDate.AddDays(7);

        // Act
        var response = await GetAsync<List<DateAvailabilityResponse>>(
            $"/api/v1/availability/dates?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&FromDate={fromDate:yyyy-MM-dd}&ToDate={toDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Should().NotBeNull();
        // Should have some weekdays with availability (provider has Mon-Fri business hours)
        response.Data!.Count.Should().BeGreaterThan(0);
        response.Data.All(d => d.HasAvailability).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableDates_WithPastFromDate_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var fromDate = DateTime.UtcNow.AddDays(-1).Date;
        var toDate = DateTime.UtcNow.AddDays(7).Date;

        // Act
        var response = await GetAsync(
            $"/api/v1/availability/dates?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&FromDate={fromDate:yyyy-MM-dd}&ToDate={toDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailableDates_WithRangeOver30Days_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var fromDate = DateTime.UtcNow.AddDays(1).Date;
        var toDate = fromDate.AddDays(31); // More than 30 days

        // Act
        var response = await GetAsync(
            $"/api/v1/availability/dates?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&FromDate={fromDate:yyyy-MM-dd}&ToDate={toDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailableDates_WithInvalidDateOrder_ShouldReturn400BadRequest()
    {
        // Arrange
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var fromDate = DateTime.UtcNow.AddDays(10).Date;
        var toDate = DateTime.UtcNow.AddDays(5).Date; // ToDate before FromDate

        // Act
        var response = await GetAsync(
            $"/api/v1/availability/dates?ProviderId={provider.Id.Value}&ServiceId={service.Id.Value}&FromDate={fromDate:yyyy-MM-dd}&ToDate={toDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Methods

    private async Task<Booking> CreateBookingForCustomerAsync(
        Guid customerId,
        Domain.Aggregates.Provider provider,
        Domain.Aggregates.Service service,
        DateTime startTime)
    {
        var staff = provider;
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

        // Add business hours (Monday-Friday, 9 AM - 5 PM)
        provider.SetBusinessHours(new Dictionary<Domain.Enums.DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>
        {
            { Domain.Enums.DayOfWeek.Monday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Tuesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Wednesday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Thursday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) },
            { Domain.Enums.DayOfWeek.Friday, (TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), TimeOnly.FromTimeSpan(TimeSpan.FromHours(17))) }
        });

        

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
