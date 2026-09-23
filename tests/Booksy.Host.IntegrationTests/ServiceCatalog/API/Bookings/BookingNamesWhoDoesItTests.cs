using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Host.IntegrationTests.Infrastructure.Fakes;
using Booksy.ServiceCatalog.Domain.Aggregates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// A customer's booking names who does it: <c>staffName</c> on GET /Bookings/my-bookings items and on
/// GET /Bookings/{id} — the assigned member's display name by the same rule as the booking flow (real name, else
/// the salon's name for them, else the salon's own name; never a placeholder or a phone), and null when the
/// booking is held by the salon itself (production QA 2026-09-23). Additive: nothing else in either shape moves.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class BookingNamesWhoDoesItTests : ServiceCatalogIntegrationTestBase
{
    private const string SalonName = "سالن نهال";

    public BookingNamesWhoDoesItTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static string RandomMobile() => $"+98912{Random.Shared.Next(1000000, 9999999)}";

    private static JToken Data(string body)
    {
        var root = JToken.Parse(body);
        return root is JObject o && o["data"] is { } data ? data : root;
    }

    private async Task<Guid> SignUpByOtpAsync(string audience, string phone, string? firstName = null, string? lastName = null)
    {
        ClearAuthenticationHeader();
        var send = await Client.PostAsJsonAsync("/api/v1/auth/send-verification-code",
            new { phoneNumber = phone, countryCode = "+98" });
        send.StatusCode.Should().Be(HttpStatusCode.OK, await send.Content.ReadAsStringAsync());

        var sms = (FakeSmsNotificationService)Factory.Services.GetRequiredService<ISmsNotificationService>();
        var message = sms.LastMessageTo(PhoneNumber.From(phone).Value) ?? sms.LastMessageTo(phone);
        var code = Regex.Match(message!, @"\d{4,8}").Value;

        var complete = await Client.PostAsJsonAsync($"/api/v1/auth/{audience}/complete-authentication",
            new { phoneNumber = phone, code, firstName, lastName });
        var body = await complete.Content.ReadAsStringAsync();
        complete.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return Guid.Parse(Data(body)["userId"]!.Value<string>()!);
    }

    private async Task<(Provider Salon, Service Service, Guid MemberId)> SalonOwnedByAsync(Guid ownerId)
    {
        var salon = await CreateAndAuthenticateAsProviderAsync(SalonName, $"{Guid.NewGuid():N}@test.com", ownerId);
        var hours = new Dictionary<DayOfWeek, (TimeOnly? Open, TimeOnly? Close)>();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            hours[day] = (new TimeOnly(9, 0), new TimeOnly(17, 0));
        salon.SetBusinessHours(hours);
        await DbContext.SaveChangesAsync();

        var service = await CreateServiceForProviderAsync(salon, "کوتاهی مو", 50.00m, 60);
        var member = await MakeBookableAsync(salon);
        ClearAuthenticationHeader();
        return (salon, service, member.Id);
    }

    /// <summary>A customer books <paramref name="resourceId"/> and reads the booking back both ways.</summary>
    private async Task<(JToken ListItem, JToken Details)> BookAndReadBackAsync(
        Provider salon, Service service, Guid resourceId, int hour)
    {
        var customer = await SignUpByOtpAsync("customer", RandomMobile(), "سارا", "احمدی");
        AuthenticateAsUser(customer, $"{Guid.NewGuid():N}@test.com");

        var booked = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = salon.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = resourceId,
            startTime = DateTime.UtcNow.Date.AddDays(2).AddHours(hour),
        });
        var bookedBody = await booked.Content.ReadAsStringAsync();
        booked.StatusCode.Should().Be(HttpStatusCode.Created, bookedBody);
        var created = Data(bookedBody);
        var bookingId = (created["bookingId"] ?? created["id"])!.Value<string>()!;

        var mine = await Client.GetAsync("/api/v1/bookings/my-bookings");
        var mineBody = await mine.Content.ReadAsStringAsync();
        mine.StatusCode.Should().Be(HttpStatusCode.OK, mineBody);
        var items = Data(mineBody) is JObject page && page["items"] is JArray paged ? paged : (JArray)Data(mineBody);
        var item = items.Single(i => i["bookingId"]!.Value<string>() == bookingId);

        var details = await Client.GetAsync($"/api/v1/bookings/{bookingId}");
        var detailsBody = await details.Content.ReadAsStringAsync();
        details.StatusCode.Should().Be(HttpStatusCode.OK, detailsBody);

        return (item, Data(detailsBody));
    }

    /// <summary>The booking's staffName; the API leaves nulls out of its JSON (WhenWritingNull), so absent is null.</summary>
    private static string? StaffNameOf(JToken booking) => booking["staffName"]?.Value<string>();

    [Fact]
    public async Task A_booking_with_a_named_member_carries_their_name()
    {
        // CreateAndAuthenticateAsRealUserAsync registers the person as «Test Owner».
        var owner = await CreateAndAuthenticateAsRealUserAsync($"{Guid.NewGuid():N}@test.com");
        var (salon, service, memberId) = await SalonOwnedByAsync(owner);

        var (item, details) = await BookAndReadBackAsync(salon, service, memberId, hour: 10);

        StaffNameOf(item).Should().Be("Test Owner");
        StaffNameOf(details).Should().Be("Test Owner");
    }

    [Fact]
    public async Task A_member_with_only_the_OTP_placeholder_is_named_by_the_salon()
    {
        var phone = RandomMobile();
        var owner = await SignUpByOtpAsync("provider", phone);
        var (salon, service, memberId) = await SalonOwnedByAsync(owner);

        var (item, details) = await BookAndReadBackAsync(salon, service, memberId, hour: 11);

        StaffNameOf(item).Should().Be(SalonName);
        StaffNameOf(details).Should().Be(SalonName);
        item.ToString().Should().NotContain(PhoneNumber.From(phone).NationalNumber);
        details.ToString().Should().NotContain(PhoneNumber.From(phone).NationalNumber);
    }

    [Fact]
    public async Task A_booking_held_by_the_salon_itself_names_nobody()
    {
        var owner = await CreateAndAuthenticateAsRealUserAsync($"{Guid.NewGuid():N}@test.com");
        var (salon, service, _) = await SalonOwnedByAsync(owner);

        var (item, details) = await BookAndReadBackAsync(salon, service, salon.Id.Value, hour: 12);

        StaffNameOf(item).Should().BeNull("no team member is assigned");
        StaffNameOf(details).Should().BeNull("no team member is assigned");
    }
}
