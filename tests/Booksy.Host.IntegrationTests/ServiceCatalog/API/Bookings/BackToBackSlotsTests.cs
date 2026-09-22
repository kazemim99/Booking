using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// Times after a booking follow from when it ends (QA walkthrough 2026-09-22: "a 45-minute booking at 14:00 must
/// make the next slot 14:45"). A fixed half-hour grid offered the next mark clear of the booking — 15:00 — and
/// left the quarter hour in between unsellable.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class BackToBackSlotsTests : ServiceCatalogIntegrationTestBase
{
    public BackToBackSlotsTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private async Task<List<DateTime>> SlotsForAsync(Guid providerId, Guid serviceId, DateTime day)
    {
        var response = await Client.GetAsync(
            $"/api/v1/availability/slots?ProviderId={providerId}&ServiceId={serviceId}&Date={day:yyyy-MM-dd}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var root = JObject.Parse(body);
        var data = root["data"] as JObject ?? root;
        return ((JArray)data["slots"]!)
            .Where(s => s["isAvailable"] == null || s["isAvailable"]!.Value<bool>())
            .Select(s => s["startTime"]!.Value<DateTime>())
            .ToList();
    }

    [Fact]
    public async Task The_slot_after_a_booking_starts_when_that_booking_ends()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var duration = service.Duration.Value;

        // A weekday inside the booking window, at 11:00 — the test salon is open 09:00–17:00.
        var day = DateTime.UtcNow.Date.AddDays(2);
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday) day = day.AddDays(1);
        var taken = day.AddHours(11);

        AuthenticateAsProviderOwner(provider);
        var booked = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = taken,
            walkInFirstName = "مرتضی",
            walkInLastName = "کاظمی",
            walkInPhone = $"+98912{Random.Shared.Next(1000000, 9999999)}",
            notifyCustomer = false,
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created, await booked.Content.ReadAsStringAsync());
        ClearAuthenticationHeader();

        var slots = await SlotsForAsync(provider.Id.Value, service.Id.Value, day);

        var endOfBooking = taken.AddMinutes(duration);
        slots.Should().Contain(endOfBooking, "the next visit can start the moment this one ends");
        slots.Should().NotContain(taken, "that time is taken");
        slots.Where(s => s > taken && s < endOfBooking).Should().BeEmpty("nothing may start inside a booking");
    }

    [Fact]
    public async Task A_slot_the_customer_was_offered_can_actually_be_booked()
    {
        // The offered grid and the conflict check used to disagree: the check added a hidden fifteen minutes to
        // the new booking only, so a start that was offered could still come back as a conflict.
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);

        var day = DateTime.UtcNow.Date.AddDays(3);
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday) day = day.AddDays(1);
        var taken = day.AddHours(10);

        AuthenticateAsProviderOwner(provider);
        await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = taken,
            walkInFirstName = "سارا",
            walkInLastName = "احمدی",
            walkInPhone = $"+98912{Random.Shared.Next(1000000, 9999999)}",
            notifyCustomer = false,
        });
        ClearAuthenticationHeader();

        var next = (await SlotsForAsync(provider.Id.Value, service.Id.Value, day))
            .Where(s => s > taken)
            .OrderBy(s => s)
            .First();

        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = next,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }
}
