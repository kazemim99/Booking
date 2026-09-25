using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// How far ahead a customer may book (QA walkthrough 2026-09-22: "nobody should be able to book more than a week
/// out"). The salon's own book is not bound by it — it fills its diary as far ahead as it likes.
/// </summary>
/// <remarks>
/// Before this, the only advance limit enforced when booking was the service's own (usually 90 days); the salon's
/// policy was checked at <c>Booking.Confirm()</c>, i.e. after the customer had already booked.
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class BookingHorizonTests : ServiceCatalogIntegrationTestBase
{
    public BookingHorizonTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static DateTime WeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    [Fact]
    public async Task A_customer_cannot_book_further_out_than_the_window()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = WeekdayAtHour(DateTime.UtcNow.Date.AddDays(14), 10),
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        // Parsed, not string-matched: the response escapes Persian into \u sequences.
        var message = JObject.Parse(body)["message"]!.Value<string>()!;
        message.Should().Contain("7").And.Contain("روز آینده امکان‌پذیر نیست");
    }

    [Fact]
    public async Task The_last_day_of_the_window_is_still_bookable()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            // Inside seven days, and on a weekday the test provider is open.
            startTime = WeekdayAtHour(DateTime.UtcNow.Date.AddDays(5), 10),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_salon_can_still_write_its_own_book_months_ahead()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = WeekdayAtHour(DateTime.UtcNow.Date.AddDays(45), 11),
            walkInFirstName = "مرتضی",
            walkInLastName = "کاظمی",
            walkInPhone = $"+98912{Random.Shared.Next(1000000, 9999999)}",
            notifyCustomer = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_window_is_published_so_a_client_can_stop_offering_those_days()
    {
        var provider = await CreateTestProviderWithServicesAsync();

        var response = await Client.GetAsync($"/api/v1/providers/{provider.Id.Value}");
        var data = JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;

        data["maxAdvanceBookingDays"]!.Value<int>().Should().Be(7);
    }
}
