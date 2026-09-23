using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Host.IntegrationTests.Infrastructure.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// A booking's time is the salon's wall clock (FOLLOW-UPS #63): «۱۰:۳۰» means half past ten at the salon, in no
/// time zone. It is stored as those digits, and it must come back as those digits.
///
/// <para>QA 2026-09-23 (openspec/changes/_inline/qa-walkthrough-2026-09-23b, task 1): a customer booked 10:30; the
/// notifications said 10:30, but the customer's appointment and the salon's calendar both said 14:00. Since
/// UtcDateTimeConverter (2026-09-11) every DateTime is read back as <c>Kind=Utc</c>, so the API wrote
/// "…T10:30:00Z"; every client (Flutter <c>toLocal()</c>, the browser's <c>new Date(…)</c>) then moved it to Tehran
/// time, +3:30. A wall-clock value must be sent without a zone, so every client reads it as the clock it is.</para>
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class BookingTimesReadBackAsWallClockTests : ServiceCatalogIntegrationTestBase
{
    public BookingTimesReadBackAsWallClockTests(BooksyHostFactory factory) : base(factory)
    {
    }

    private static DateTime NextWeekday(DateTime from)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day;
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
        using var doc = JsonDocument.Parse(await complete.Content.ReadAsStringAsync());
        return Guid.Parse(doc.RootElement.GetProperty("data").GetProperty("userId").GetString()!);
    }

    /// Every "startTime" / "endTime" string anywhere in the response, exactly as it was written on the wire.
    private static List<(string Name, string Value)> TimeFields(string json)
    {
        var found = new List<(string, string)>();
        void Walk(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var p in e.EnumerateObject())
                    {
                        if (p.Value.ValueKind == JsonValueKind.String &&
                            (p.Name.Equals("startTime", StringComparison.OrdinalIgnoreCase) ||
                             p.Name.Equals("endTime", StringComparison.OrdinalIgnoreCase)))
                        {
                            found.Add((p.Name, p.Value.GetString()!));
                        }
                        Walk(p.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in e.EnumerateArray()) Walk(item);
                    break;
            }
        }
        using var doc = JsonDocument.Parse(json);
        Walk(doc.RootElement);
        return found;
    }

    private async Task<string> GetOk(string url)
    {
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return body;
    }

    [Fact]
    public async Task A_booking_made_for_half_past_ten_reads_back_as_half_past_ten_everywhere()
    {
        var phone = "+989121112233";
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        var day = NextWeekday(DateTime.UtcNow.Date.AddDays(2));

        // Sent the way the customer app sends a slot: the salon's digits, no zone (wallClockIso).
        var start = $"{day:yyyy-MM-dd}T10:30:00";
        AuthenticateAsProviderOwner(provider);
        var create = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = start,
            walkInFirstName = "مصطفی",
            walkInLastName = "کاظمی",
            walkInPhone = phone,
            notifyCustomer = false,
        });
        var created = await create.Content.ReadAsStringAsync();
        create.StatusCode.Should().Be(HttpStatusCode.Created, created);

        // The salon's calendar.
        var salonView = await GetOk($"/api/v1/bookings/provider/{provider.Id.Value}");

        // The customer's appointments.
        var customer = await SignUpAsCustomerAsync(phone, "مصطفی", "کاظمی");
        AuthenticateAsUser(customer, "customer@test.com");
        var customerView = await GetOk("/api/v1/bookings/my-bookings");

        foreach (var (where, body) in new[] { ("create response", created), ("salon calendar", salonView), ("my bookings", customerView) })
        {
            var times = TimeFields(body);
            times.Should().NotBeEmpty($"the {where} carries the booking's time");
            foreach (var (name, value) in times)
            {
                value.Should().NotEndWith("Z", $"{where}.{name} is a wall-clock time, not a UTC instant");
                value.Should().NotMatchRegex(@"[+-]\d{2}:\d{2}$", $"{where}.{name} carries no offset");
            }
            times.Should().Contain(t => t.Name.Equals("startTime", StringComparison.OrdinalIgnoreCase) && t.Value.StartsWith(start),
                $"the {where} says {start}, the time that was booked");
        }
    }
}
