using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Host.IntegrationTests.Infrastructure.Fakes;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// A customer is told, in their own inbox, what the salon decided about their appointment.
/// </summary>
/// <remarks>
/// <para>Found in the production QA recording of 2026-09-23: a customer booked at سالن نهال, the owner accepted
/// the request in the provider app, the appointment turned «تایید شده» — and the customer's inbox still held only
/// the «درخواست نوبت ثبت شد» notices. Accepting a request raised nothing at all: the confirm handler scheduled
/// reminders and stopped. The outbox-level tests could not see it, because none of them confirms a request.</para>
///
/// <para>So these run the whole journey the way the two apps do it — a named customer signs up and books through
/// the API, the owner acts through the API, the sweep runs, and the CUSTOMER reads their inbox through the API —
/// and assert on what the customer actually reads: which salon, which service, which day and which wall-clock time,
/// with a tap that opens the booking.</para>
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class SalonDecisionInboxTests : ServiceCatalogIntegrationTestBase
{
    public SalonDecisionInboxTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task When_the_salon_accepts_a_request_the_customer_is_told_in_their_inbox()
    {
        var b = await BookAsNamedCustomerAsync(daysAhead: 3, hour: 10);

        AuthenticateAsProviderOwner(b.Provider);
        await ConfirmAsync(b.BookingId);

        await SweepAsync();
        var notice = await CustomersNoticeAsync(b, NotificationEventCode.BookingConfirmed);

        notice["subject"]!.Value<string>().Should().Be("نوبت شما تأیید شد");
        AssertNamesTheAppointment(notice, b, b.StartTime);
        AssertOpensTheBooking(notice, b.BookingId);
    }

    [Fact]
    public async Task Accepting_a_request_tells_the_customer_once_however_often_the_sweep_runs()
    {
        var b = await BookAsNamedCustomerAsync(daysAhead: 3, hour: 11);

        AuthenticateAsProviderOwner(b.Provider);
        await ConfirmAsync(b.BookingId);

        await SweepAsync();
        await SweepAsync();

        var items = await CustomersInboxAsync(b);
        items.Where(i => IsCode(i, NotificationEventCode.BookingConfirmed)).Should().ContainSingle();
    }

    [Fact]
    public async Task When_the_salon_declines_a_request_the_customer_is_told_in_their_inbox()
    {
        var b = await BookAsNamedCustomerAsync(daysAhead: 3, hour: 12);

        AuthenticateAsProviderOwner(b.Provider);
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{b.BookingId}/cancel", new { reason = "آن ساعت پر است" });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        await SweepAsync();
        var notice = await CustomersNoticeAsync(b, NotificationEventCode.BookingRejected);

        notice["subject"]!.Value<string>().Should().Be("درخواست نوبت تأیید نشد");
        AssertNamesTheAppointment(notice, b, b.StartTime);
        notice["body"]!.Value<string>().Should().Contain("آن ساعت پر است", "the salon's reason is passed on");
        notice["destinationId"]!.Value<string>().Should().Be(b.BookingId.ToString());
    }

    [Fact]
    public async Task When_the_salon_cancels_an_accepted_appointment_the_customer_is_told_in_their_inbox()
    {
        var b = await BookAsNamedCustomerAsync(daysAhead: 4, hour: 10);

        AuthenticateAsProviderOwner(b.Provider);
        await ConfirmAsync(b.BookingId);
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{b.BookingId}/cancel", new { reason = "استاد بیمار است" });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        await SweepAsync();
        var notice = await CustomersNoticeAsync(b, NotificationEventCode.BookingCancelledByProvider);

        notice["subject"]!.Value<string>().Should().Be("نوبت شما لغو شد");
        AssertNamesTheAppointment(notice, b, b.StartTime);
        notice["body"]!.Value<string>().Should().Contain("استاد بیمار است");
        notice["destinationId"]!.Value<string>().Should().Be(b.BookingId.ToString());
    }

    [Fact]
    public async Task When_the_salon_moves_an_accepted_appointment_the_customer_is_told_the_new_time_in_their_inbox()
    {
        var b = await BookAsNamedCustomerAsync(daysAhead: 4, hour: 11);
        var newStart = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(6), 14);

        AuthenticateAsProviderOwner(b.Provider);
        await ConfirmAsync(b.BookingId);
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/bookings/{b.BookingId}/reschedule", new { newStartTime = newStart, reason = "جابه‌جایی" });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var newBookingId = Guid.Parse(Regex.Match(body, @"New booking ID: ([0-9a-fA-F-]{36})").Groups[1].Value);

        await SweepAsync();
        var notice = await CustomersNoticeAsync(b, NotificationEventCode.BookingRescheduled);

        notice["subject"]!.Value<string>().Should().Be("زمان نوبت تغییر کرد");
        AssertNamesTheAppointment(notice, b, newStart);
        AssertOpensTheBooking(notice, newBookingId);
    }

    // ── assert ──

    private static void AssertNamesTheAppointment(JToken notice, Arranged b, DateTime wallClock)
    {
        var body = notice["body"]!.Value<string>();

        body.Should().StartWith("مصطفی کاظمی عزیز،", "the customer is addressed by name");
        body.Should().Contain(b.BusinessName, "which salon");
        body.Should().Contain(b.ServiceName, "which service");
        // Salon wall-clock digits, written the way every other booking notice writes them.
        body.Should().Contain(BookingSmsText.PersianDateTime(wallClock), "which day and time");
    }

    private static void AssertOpensTheBooking(JToken notice, Guid bookingId)
    {
        notice["destinationKind"]!.Value<string>().Should().Be("Booking");
        notice["destinationId"]!.Value<string>().Should().Be(bookingId.ToString());
        notice["isActionable"]!.Value<bool>().Should().BeTrue("the booking exists and is the customer's own");
    }

    private static bool IsCode(JToken item, NotificationEventCode code) =>
        string.Equals(item["eventCode"]?.Value<string>(), code.ToString(), StringComparison.OrdinalIgnoreCase);

    // ── arrange ──

    private sealed record Arranged(
        Guid BookingId,
        Guid CustomerId,
        DateTime StartTime,
        string BusinessName,
        string ServiceName,
        Domain.Aggregates.Provider Provider);

    /// <summary>A real, named customer (a UserManagement user with a phone) books online: a request, not an appointment.</summary>
    private async Task<Arranged> BookAsNamedCustomerAsync(int daysAhead, int hour)
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var customerId = await SignUpAsCustomerAsync(UniqueMobile(), "مصطفی", "کاظمی");
        var start = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(daysAhead), hour);

        AuthenticateAsUser(customerId, $"{Guid.NewGuid():N}@test.com");
        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = start,
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var root = JObject.Parse(body);
        var data = root["data"] as JObject ?? root;
        var bookingId = Guid.Parse((data["bookingId"] ?? data["id"])!.Value<string>()!);
        (data["status"]?.Value<string>()).Should().BeEquivalentTo("Requested", "an online booking waits for the salon");

        return new Arranged(bookingId, customerId, start, provider.Profile.BusinessName, service.Name, provider);
    }

    /// <summary>Exactly what the provider app sends: an empty body.</summary>
    private async Task ConfirmAsync(Guid bookingId)
    {
        var response = await Client.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/confirm", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<Guid> SignUpAsCustomerAsync(string phone, string firstName, string lastName)
    {
        ClearAuthenticationHeader();
        var send = await Client.PostAsJsonAsync("/api/v1/auth/send-verification-code",
            new { phoneNumber = phone, countryCode = "+98" });
        send.StatusCode.Should().Be(HttpStatusCode.OK, await send.Content.ReadAsStringAsync());

        var sms = (FakeSmsNotificationService)Factory.Services.GetRequiredService<ISmsNotificationService>();
        var message = sms.LastMessageTo(PhoneNumber.From(phone).Value) ?? sms.LastMessageTo(phone);
        var code = Regex.Match(message!, @"\d{4,8}").Value;

        var complete = await Client.PostAsJsonAsync("/api/v1/auth/customer/complete-authentication",
            new { phoneNumber = phone, code, firstName, lastName });
        complete.StatusCode.Should().Be(HttpStatusCode.OK, await complete.Content.ReadAsStringAsync());
        return Guid.Parse(JObject.Parse(await complete.Content.ReadAsStringAsync())["data"]!["userId"]!.Value<string>()!);
    }

    private async Task SweepAsync()
    {
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>().ExecuteAsync();
    }

    private async Task<List<JToken>> CustomersInboxAsync(Arranged b)
    {
        AuthenticateAsUser(b.CustomerId, $"{Guid.NewGuid():N}@test.com");
        var response = await Client.GetAsync("/api/v1/Notifications/inbox?pageSize=50");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var parsed = JObject.Parse(body);
        var page = parsed["data"] as JObject ?? parsed;
        return page["items"]!.Children().ToList();
    }

    private async Task<JToken> CustomersNoticeAsync(Arranged b, NotificationEventCode code)
    {
        var items = await CustomersInboxAsync(b);
        var codes = items.Select(i => i["eventCode"]?.Value<string>()).ToList();

        var notice = items.SingleOrDefault(i => IsCode(i, code));
        notice.Should().NotBeNull(
            $"the customer should find a {code} notice in their inbox; it holds [{string.Join(", ", codes)}]");
        return notice!;
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    /// <summary>A number no other test uses: a per-run counter under a prefix of this class's own (0936).</summary>
    private static string UniqueMobile() => $"+98936{Interlocked.Increment(ref _mobiles):D7}";

    private static int _mobiles = 4_200_000;
}
