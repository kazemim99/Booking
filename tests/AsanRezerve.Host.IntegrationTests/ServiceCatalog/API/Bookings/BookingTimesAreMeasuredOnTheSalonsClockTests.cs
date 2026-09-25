using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// A booking's time is the salon's wall clock, and "now" for every booking rule is the salon's clock too
/// (<see cref="SalonTime"/>). The API measured against the server's UTC clock, 3:30 behind the salon: at 10:33 a
/// salon was refused «cannot be completed before scheduled time» for its 10:00 appointment (QA 2026-09-24,
/// openspec/changes/_inline/qa-walkthrough-2026-09-24), while a customer could still book, and see as free, times
/// up to three and a half hours gone.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class BookingTimesAreMeasuredOnTheSalonsClockTests : ServiceCatalogIntegrationTestBase
{
    public BookingTimesAreMeasuredOnTheSalonsClockTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task The_salon_marks_an_appointment_done_half_an_hour_after_it_started()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(salon.Id.Value)).First();
        var booking = Booking.CreateConfirmedByProvider(
            UserId.From(Guid.NewGuid()), salon.Id, service.Id, salon.Id.Value,
            SalonTime.Now.AddMinutes(-33),
            service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "qa 2026-09-24");
        await CreateEntityAsync(booking);

        AuthenticateAsProviderOwner(salon);
        var response = await Client.PostAsJsonAsync($"/api/v1/bookings/{booking.Id.Value}/complete", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_customer_cannot_book_an_hour_that_has_already_gone_at_the_salon()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(salon.Id.Value);
        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = salon.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = salon.Id.Value,
            startTime = SalonTime.Now.AddHours(-1),
        });

        // 400 is the "in the past" answer; anything else (a 201, or a 409 from the opening-hours check) means the
        // past-time guard let it through.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Todays_free_times_start_no_earlier_than_the_salons_now()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(salon.Id.Value);
        await OpenAllDayAsync(salon.Id.Value);

        var today = SalonTime.Now.Date;
        var response = await Client.GetAsync(
            $"/api/v1/availability/slots?ProviderId={salon.Id.Value}&ServiceId={service.Id.Value}&Date={today:yyyy-MM-dd}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var root = JObject.Parse(body);
        var data = root["data"] as JObject ?? root;
        var starts = ((JArray)data["slots"]!).Select(s => s["startTime"]!.Value<DateTime>()).ToList();

        var salonNow = SalonTime.Now;
        starts.Should().OnlyContain(s => s > salonNow.AddMinutes(-1), "a time already gone at the salon is not free");
    }

    private async Task OpenAllDayAsync(Guid providerId)
    {
        var provider = (await FindProviderAsync(providerId))!;
        var hours = new Dictionary<Domain.Enums.DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();
        foreach (Domain.Enums.DayOfWeek day in Enum.GetValues<Domain.Enums.DayOfWeek>())
            hours[day] = (new TimeOnly(0, 0), new TimeOnly(23, 59));
        provider.SetBusinessHours(hours);
        await UpdateEntityAsync(provider);

        // The API reads the salon through CachedProviderReadRepository, and the fixture's own bookability check
        // (MakeBookableAsync) has just cached it with its default 09:00-17:00 hours. This write goes through the
        // test's DbContext, not the unit of work whose BusinessHoursUpdatedEvent evicts that entry in production,
        // so without this the request saw 09:00-17:00: the list came back empty from 16:00 salon time on and the
        // test went red every evening (2026-09-25).
        var cache = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.RemoveAsync($"Provider:{provider.Id.Value}");
        await cache.RemoveAsync($"Provider:owner:{provider.OwnerId.Value}");
    }
}
