using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Host.IntegrationTests.Infrastructure.Fakes;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Bookings;

/// <summary>
/// The salon's booking list says who each booking is for (QA 2026-09-24, openspec/changes/_inline/qa-walkthrough-2026-09-25).
/// The list carried no customer name at all, so the request card and the booking sheet read «بدون نام» for
/// «ناصر عابدی» — while the notification, which resolves the name, named him. Same name as the notification uses:
/// the customer's own real name, the salon's own book name for a walk-in, and none (never a phone) when there is
/// no real name.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class SalonsBookingListNamesTheCustomerTests : ServiceCatalogIntegrationTestBase
{
    public SalonsBookingListNamesTheCustomerTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static string NewPhone()
    {
        var digits = new string(Guid.NewGuid().ToString("N").Where(char.IsDigit).ToArray()).PadRight(7, '0');
        return "+98912" + digits[..7];
    }

    private async Task<Guid> SignUpAsync(string phone, string? first, string? last)
    {
        ClearAuthenticationHeader();
        var send = await Client.PostAsJsonAsync("/api/v1/auth/send-verification-code",
            new { phoneNumber = phone, countryCode = "+98" });
        send.StatusCode.Should().Be(HttpStatusCode.OK, await send.Content.ReadAsStringAsync());

        var sms = (FakeSmsNotificationService)Factory.Services.GetRequiredService<ISmsNotificationService>();
        var message = sms.LastMessageTo(PhoneNumber.From(phone).Value) ?? sms.LastMessageTo(phone);
        var code = Regex.Match(message!, @"\d{4,8}").Value;

        var complete = await Client.PostAsJsonAsync("/api/v1/auth/customer/complete-authentication",
            new { phoneNumber = phone, code, firstName = first, lastName = last });
        complete.StatusCode.Should().Be(HttpStatusCode.OK, await complete.Content.ReadAsStringAsync());
        return Guid.Parse(JObject.Parse(await complete.Content.ReadAsStringAsync())["data"]!["userId"]!.Value<string>()!);
    }

    private static DateTime Weekday10() =>
        SalonTime.Now.Date.AddDays(3).AddHours(10);

    private async Task<JToken> BookedThenListedAsync(Func<Domain.Aggregates.Provider, Domain.Aggregates.Service, Task> book,
        Domain.Aggregates.Provider salon, Domain.Aggregates.Service service)
    {
        await book(salon, service);

        AuthenticateAsProviderOwner(salon);
        var response = await Client.GetAsync($"/api/v1/bookings/provider/{salon.Id.Value}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var root = JToken.Parse(body);
        var list = root is JArray ? root : root["data"]!;
        return list.Single();
    }

    [Fact]
    public async Task A_booking_by_a_customer_carries_their_real_name()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(salon.Id.Value);
        var customer = await SignUpAsync(NewPhone(), "ناصر", "عابدی");

        var row = await BookedThenListedAsync(async (s, sv) =>
        {
            AuthenticateAsUser(customer, $"{customer:N}@test.com");
            var r = await Client.PostAsJsonAsync("/api/v1/bookings", new
            {
                providerId = s.Id.Value, serviceId = sv.Id.Value, staffProviderId = s.Id.Value, startTime = Weekday10(),
            });
            r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        }, salon, service);

        row["customerName"]!.Value<string>().Should().Be("ناصر عابدی");
    }

    [Fact]
    public async Task A_walk_in_carries_the_name_in_the_salons_own_book()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(salon.Id.Value);

        var row = await BookedThenListedAsync(async (s, sv) =>
        {
            AuthenticateAsProviderOwner(s);
            var r = await Client.PostAsJsonAsync("/api/v1/bookings", new
            {
                providerId = s.Id.Value, serviceId = sv.Id.Value, staffProviderId = s.Id.Value, startTime = Weekday10(),
                walkInFirstName = "مرتضی", walkInLastName = "کاظمی", walkInPhone = NewPhone(), notifyCustomer = false,
            });
            r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        }, salon, service);

        row["customerName"]!.Value<string>().Should().Be("مرتضی کاظمی");
    }

    [Fact]
    public async Task A_customer_without_a_real_name_carries_none_and_never_their_phone()
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(salon.Id.Value);
        var customer = await SignUpAsync(NewPhone(), null, null);

        var row = await BookedThenListedAsync(async (s, sv) =>
        {
            AuthenticateAsUser(customer, $"{customer:N}@test.com");
            var r = await Client.PostAsJsonAsync("/api/v1/bookings", new
            {
                providerId = s.Id.Value, serviceId = sv.Id.Value, staffProviderId = s.Id.Value, startTime = Weekday10(),
            });
            r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        }, salon, service);

        var name = row["customerName"];
        (name == null || name.Type == JTokenType.Null).Should().BeTrue("a placeholder is not a name");
    }
}
