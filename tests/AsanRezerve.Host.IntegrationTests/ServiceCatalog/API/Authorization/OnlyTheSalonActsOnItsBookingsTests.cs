using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Authorization;

/// <summary>
/// Confirming, completing or marking a no-show is the salon's call on its OWN bookings. The endpoints only asked
/// "is the caller a provider?" (policy ProviderOrAdmin), so the owner of ANY salon could accept another salon's
/// request — found while fixing the customer's «نوبت شما تأیید شد» notice (QA 2026-09-23,
/// openspec/changes/_inline/qa-walkthrough-2026-09-23b), which would then have told the customer so. They now use the
/// rule the salon's booking list already uses: the salon's owner, a member allowed to manage its bookings, or an admin.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class OnlyTheSalonActsOnItsBookingsTests : ServiceCatalogIntegrationTestBase
{
    public OnlyTheSalonActsOnItsBookingsTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    private async Task<(Guid BookingId, Domain.Aggregates.Provider Salon)> ARequestAtASalonAsync()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = (await GetProviderServicesAsync(salon.Id.Value)).First();
        var booking = Booking.CreateBookingRequest(
            UserId.From(Guid.NewGuid()), salon.Id, service.Id, salon.Id.Value,
            NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(4), 14),
            service.Duration, service.BasePrice, service.BookingPolicy ?? BookingPolicy.Default, "authz");
        await CreateEntityAsync(booking);
        return (booking.Id.Value, salon);
    }

    [Theory]
    [InlineData("confirm")]
    [InlineData("complete")]
    [InlineData("no-show")]
    [InlineData("assign-staff")]
    public async Task Another_salons_owner_cannot_act_on_the_booking(string action)
    {
        var (bookingId, _) = await ARequestAtASalonAsync();
        var otherSalon = await CreateTestProviderWithServicesAsync();

        AuthenticateAsProviderOwner(otherSalon);
        var response = action == "assign-staff"
            ? await Client.PutAsJsonAsync($"/api/v1/bookings/{bookingId}/assign-staff/{otherSalon.Id.Value}", new { })
            : await Client.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/{action}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_salons_own_owner_still_accepts_the_request()
    {
        var (bookingId, salon) = await ARequestAtASalonAsync();

        AuthenticateAsProviderOwner(salon);
        var response = await Client.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/confirm", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}
