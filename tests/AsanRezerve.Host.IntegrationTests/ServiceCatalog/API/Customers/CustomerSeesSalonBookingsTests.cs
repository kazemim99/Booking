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

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// A person sees the appointments a salon made for them
/// (openspec/changes/_inline/customer-sees-salon-bookings). The salon books them by mobile number
/// — before or after they ever open the app — and the booking's "customer" in the aggregate is the
/// salon's own owner, so nothing of theirs showed in "my bookings" until now. Sign-in proves the
/// number, so what a salon recorded against it is theirs to see.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class CustomerSeesSalonBookingsTests : ServiceCatalogIntegrationTestBase
{
    public CustomerSeesSalonBookingsTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static DateTime NextWeekdayAtHour(DateTime from, int hour)
    {
        var day = from.Date;
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);
        return day.AddHours(hour);
    }

    /// Signs the person up the way they really do: an OTP to their mobile, read from the SMS the
    /// test host captures. Returns their user id.
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
        var data = JObject.Parse(await complete.Content.ReadAsStringAsync())["data"]!;
        return Guid.Parse(data["userId"]!.Value<string>()!);
    }

    private async Task<JArray> MyBookings()
    {
        var response = await Client.GetAsync("/api/v1/bookings/my-bookings");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var data = JObject.Parse(await response.Content.ReadAsStringAsync())["data"]!;
        return (JArray)(data["items"] ?? data)!;
    }

    [Fact]
    public async Task What_the_salon_booked_for_their_number_is_in_their_own_bookings()
    {
        var phone = "+989123135143";
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);

        var booking = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(2), 10),
            walkInFirstName = "مرتضی",
            walkInLastName = "کاظمی",
            walkInPhone = phone,
            notifyCustomer = false,
        });
        booking.StatusCode.Should().Be(HttpStatusCode.Created, await booking.Content.ReadAsStringAsync());

        // The person signs up afterwards, with the number the salon booked.
        var customer = await SignUpAsCustomerAsync(phone, "مرتضی", "کاظمی");
        AuthenticateAsUser(customer, "customer@test.com");

        var mine = await MyBookings();
        mine.Should().ContainSingle("the salon's booking belongs to the person it was made for");
        mine[0]["providerId"]!.Value<string>().Should().Be(provider.Id.Value.ToString());
    }

    [Fact]
    public async Task Somebody_elses_number_is_not_their_business()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var service = await GetFirstServiceForProviderAsync(provider.Id.Value);
        AuthenticateAsProviderOwner(provider);
        await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = provider.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = provider.Id.Value,
            startTime = NextWeekdayAtHour(DateTime.UtcNow.Date.AddDays(3), 11),
            walkInFirstName = "مرتضی",
            walkInLastName = "کاظمی",
            walkInPhone = "+989123135143",
            notifyCustomer = false,
        });

        var stranger = await SignUpAsCustomerAsync("+989351112233", "سارا", "احمدی");
        AuthenticateAsUser(stranger, "stranger@test.com");

        (await MyBookings()).Should().BeEmpty();
    }
}
