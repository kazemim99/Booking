using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.ServiceCatalog.Api.Models.Responses;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using System.Net;

namespace Booksy.ServiceCatalog.IntegrationTests.StepDefinitions.Bookings;

/// <summary>
/// Step definitions for booking-related scenarios
/// </summary>
[Binding]
public class BookingSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ServiceCatalogReqnrollTestBase _testBase;
    private readonly ScenarioContextHelper _helper;

    public BookingSteps(
        ScenarioContext scenarioContext,
        ServiceCatalogReqnrollTestBase testBase)
    {
        _scenarioContext = scenarioContext;
        _testBase = testBase;
        _helper = _scenarioContext.Get<ScenarioContextHelper>("Helper");
    }

    [When(@"I send a POST request to create a booking with:")]
    public async Task WhenISendAPostRequestToCreateABookingWith(Table table)
    {
        var provider = _scenarioContext.Get<Domain.Aggregates.Provider>("Provider:Current");

        // Build request from table
        var requestData = _helper.BuildDictionaryFromTable(table);

        // Multi-service visits: "ServiceIds" is a comma-separated list of
        // resolved ids; single-service requests keep using "ServiceId".
        string[]? serviceIds = requestData.ContainsKey("ServiceIds")
            ? requestData["ServiceIds"]!
                .ToString()!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null;

        // Staff is mandatory on the wire; scenarios seed it via
        // "the provider has at least one staff member".
        var staffProviderId = _scenarioContext.ContainsKey("CurrentStaffId")
            ? _scenarioContext.Get<Guid>("CurrentStaffId")
            : Guid.Empty;

        // Create request object
        var request = new
        {
            ProviderId = provider.Id.Value,
            ServiceId = serviceIds is { Length: > 0 }
                ? serviceIds[0]
                : requestData["ServiceId"],
            ServiceIds = serviceIds,
            StaffProviderId = staffProviderId,
            StartTime = requestData["StartTime"],
            CustomerNotes = requestData.ContainsKey("Notes") ? requestData["Notes"] : null
        };

        // Send request
        var response = await _testBase.PostAsJsonAsync<object, BookingResponse>(
            "/api/v1/bookings", request);

        // Store response
        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");

        // Surface the API's own reason in the scenario log — a bare status
        // assertion hides WHY creation failed.
        if (response.Error != null)
            Console.WriteLine($"Create booking error: {response.Error.Code} — {response.Error.Message}");

        if (response.Data != null)
        {
            _scenarioContext.Set(response.Data, "LastBookingResponse");
            _scenarioContext.Set(response.Data.Id, "LastBookingId");
        }
    }

    [When(@"I send a POST request to cancel the booking with:")]
    public async Task WhenISendAPostRequestToCancelTheBookingWith(Table table)
    {
        var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");
        var customerId = _scenarioContext.Get<Guid>("CurrentUserId");

        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            Reason = requestData["Reason"],
            CancelledBy = customerId
        };

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/cancel", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I send a POST request to cancel the other customer's booking with:")]
    public async Task WhenISendAPostRequestToCancelTheOtherCustomersBookingWith(Table table)
    {
        var otherBooking = _scenarioContext.Get<Booking>("OtherCustomerBooking");
        var customerId = _scenarioContext.Get<Guid>("CurrentUserId");

        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            Reason = requestData["Reason"],
            CancelledBy = customerId
        };

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/bookings/{otherBooking.Id.Value}/cancel", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");
    }

    [When(@"I send a POST request to reschedule the booking with:")]
    public async Task WhenISendAPostRequestToRescheduleTheBookingWith(Table table)
    {
        var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");

        var requestData = _helper.BuildDictionaryFromTable(table);

        var request = new
        {
            NewStartTime = _helper.ParseRelativeTime(requestData["NewStartTime"]!.ToString()!),
            Reason = requestData.ContainsKey("Reason") ? requestData["Reason"] : null
        };

        var response = await _testBase.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/reschedule", request);

        _scenarioContext.Set(response, "LastResponse");
        _scenarioContext.Set(response.StatusCode, "LastStatusCode");

        // Surface the API's own reason — a bare status assertion hides WHY it failed.
        if (response.Error != null)
            Console.WriteLine($"Reschedule error: {response.Error.Code} — {response.Error.Message}");
    }

    [Then(@"the old booking should have status ""(.*)"" in the database")]
    public async Task ThenTheOldBookingShouldHaveStatusInTheDatabase(string expectedStatus)
    {
        var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");

        var booking = await _testBase.DbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == BookingId.From(bookingId));

        booking.Should().NotBeNull($"Booking with ID {bookingId} should exist in database");
        booking!.Status.ToString().Should().Be(expectedStatus,
            "the rescheduled-away booking is retained and marked, not deleted");
    }

    [Then(@"a new booking should exist for the new time slot")]
    public async Task ThenANewBookingShouldExistForTheNewTimeSlot()
    {
        var oldBookingId = _scenarioContext.Get<Guid>("CurrentBookingId");

        var newBooking = await _testBase.DbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.PreviousBookingId == BookingId.From(oldBookingId));

        newBooking.Should().NotBeNull(
            "rescheduling creates a successor booking linked back to the original");

        _scenarioContext.Set(newBooking!, "NewBooking:Current");
    }

    /// <summary>
    /// Rescheduling moves a booking in TIME; it must not silently move it to a
    /// different staff member, which is what a resource-resolution bug would do.
    /// </summary>
    [Then(@"the new booking should be held for the same resource")]
    public void ThenTheNewBookingShouldBeHeldForTheSameResource()
    {
        var newBooking = _scenarioContext.Get<Booking>("NewBooking:Current");
        var originalResourceId = _scenarioContext.Get<Guid>("CurrentBookingResourceId");

        newBooking.StaffId.Should().Be(originalResourceId,
            "a reschedule that does not name a new staff member keeps the original resource");
    }

    [Then(@"the booking should still be scheduled for its original time")]
    public async Task ThenTheBookingShouldStillBeScheduledForItsOriginalTime()
    {
        var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");
        var originalStart = _scenarioContext.Get<DateTime>("CurrentBookingStartTime");

        var booking = await _testBase.DbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == BookingId.From(bookingId));

        booking.Should().NotBeNull();
        booking!.TimeSlot.StartTime.Should().BeCloseTo(originalStart, TimeSpan.FromSeconds(1),
            "a rejected reschedule must not move the booking it failed to reschedule");
    }

    /// <summary>
    /// The successor booking must own its policy and payment state rather than borrow the
    /// original's instances — sharing them makes EF treat the save as re-parenting an owned
    /// entity and reject it. Asserting the ORIGINAL still has its own is how we detect the
    /// aliasing having been reintroduced.
    /// </summary>
    [Then(@"the original booking still has its own policy and payment information")]
    public async Task ThenTheOriginalBookingStillHasItsOwnPolicyAndPaymentInformation()
    {
        var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");

        var original = await _testBase.DbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == BookingId.From(bookingId));

        original.Should().NotBeNull();
        original!.Policy.Should().NotBeNull("the rescheduled-away booking keeps its own policy");
        original.PaymentInfo.Should().NotBeNull("and its own payment state");
        original.TotalPrice.Should().NotBeNull("and its own price");

        var successor = await _testBase.DbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.PreviousBookingId == BookingId.From(bookingId));

        successor.Should().NotBeNull();
        successor!.Policy.RescheduleWindowHours.Should().Be(original.Policy.RescheduleWindowHours,
            "the successor carries the same policy TERMS");
        successor.TotalPrice.Amount.Should().Be(original.TotalPrice.Amount,
            "and the same price");
        successor.PaymentInfo.Status.Should().Be(original.PaymentInfo.Status,
            "and the same payment state");
    }

    [Then(@"the response should contain a booking with:")]
    public void ThenTheResponseShouldContainABookingWith(Table table)
    {
        var response = _scenarioContext.Get<ApiResponse<BookingResponse>>("LastResponse");

        response.Data.Should().NotBeNull("Response should contain booking data");

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = _helper.ReplaceContextPlaceholders(row["Value"]);

            var actualValue = GetFieldValue(response.Data, field);

            actualValue?.ToString().Should().Be(expectedValue,
                $"Field '{field}' should have value '{expectedValue}'");
        }
    }

    [Then(@"the response message should contain ""(.*)""")]
    public void ThenTheResponseMessageShouldContain(string expectedText)
    {
        var response = _scenarioContext.Get<ApiResponse>("LastResponse");

        response.Message.Should().Contain(expectedText,
            $"Response message should contain '{expectedText}'");
    }

    [Then(@"the booking should exist in the database with status ""(.*)""")]
    public async Task ThenTheBookingShouldExistInTheDatabaseWithStatus(string expectedStatus)
    {
        var bookingId = _scenarioContext.Get<Guid>("LastBookingId");

        var booking = await _testBase.DbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == BookingId.From(bookingId));

        booking.Should().NotBeNull($"Booking with ID {bookingId} should exist in database");
        booking!.Status.ToString().Should().Be(expectedStatus,
            $"Booking status should be {expectedStatus}");
    }

    [Then(@"the stored booking should have (\d+) service lines, (\d+) minutes and total price (.*)")]
    public async Task ThenTheStoredBookingShouldHaveServiceLines(
        int expectedLines, int expectedMinutes, decimal expectedTotal)
    {
        var bookingId = _scenarioContext.Get<Guid>("LastBookingId");

        var booking = await _testBase.DbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == BookingId.From(bookingId));

        booking.Should().NotBeNull();
        booking!.Services.Should().HaveCount(expectedLines,
            "every bundled service becomes one persisted line item");
        booking.Duration.Value.Should().Be(expectedMinutes,
            "the visit occupies the combined duration of its services");
        booking.TotalPrice.Amount.Should().Be(expectedTotal,
            "the bill is the sum over the service lines");
    }

    [Then(@"the booking should have status ""(.*)"" in the database")]
    public async Task ThenTheBookingShouldHaveStatusInTheDatabase(string expectedStatus)
    {
        var bookingId = _scenarioContext.Get<Guid>("CurrentBookingId");

        var booking = await _testBase.DbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == BookingId.From(bookingId));

        booking.Should().NotBeNull($"Booking with ID {bookingId} should exist in database");
        booking!.Status.ToString().Should().Be(expectedStatus,
            $"Booking status should be {expectedStatus}");
    }

    private object? GetFieldValue(object obj, string fieldName)
    {
        if (obj == null)
            return null;

        var property = obj.GetType().GetProperty(fieldName);
        if (property == null)
            return null;

        return property.GetValue(obj);
    }
}
