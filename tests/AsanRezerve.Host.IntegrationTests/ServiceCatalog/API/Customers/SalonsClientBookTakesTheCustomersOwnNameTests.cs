using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Host.IntegrationTests.Infrastructure.Fakes;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Customers;

/// <summary>
/// A salon's client book holds whatever it first called someone — often the label from its phone's contacts. When
/// that person books the salon themselves with their real name, the salon's entry for their number takes that name
/// (QA 2026-09-24: the salon still listed «Mostafa Cell» for a customer who had booked as «مصطفی کاظمی»). Booking is
/// the moment the customer shares their name with THAT salon, so no other salon's book changes.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class SalonsClientBookTakesTheCustomersOwnNameTests : ServiceCatalogIntegrationTestBase
{
    public SalonsClientBookTakesTheCustomersOwnNameTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Booking_the_salon_renames_its_entry_for_their_number()
    {
        var phone = NewPhone();
        var salon = await CreateTestProviderWithServicesAsync();
        var entry = await SalonCallsThemAsync(salon, phone, "Mostafa", "Cell");

        var customerId = await SignUpAsCustomerAsync(phone, "مصطفی", "کاظمی");
        await BookAsync(salon, customerId);

        var renamed = await EntryAsync(entry.Id);
        renamed.FirstName.Should().Be("مصطفی");
        renamed.LastName.Should().Be("کاظمی");
    }

    [Fact]
    public async Task Another_salons_entry_for_the_same_number_is_left_alone()
    {
        var phone = NewPhone();
        var booked = await CreateTestProviderWithServicesAsync();
        var other = await CreateTestProviderWithServicesAsync();
        await SalonCallsThemAsync(booked, phone, "Mostafa", "Cell");
        var elsewhere = await SalonCallsThemAsync(other, phone, "Mostafa", "Cell");

        var customerId = await SignUpAsCustomerAsync(phone, "مصطفی", "کاظمی");
        await BookAsync(booked, customerId);

        (await EntryAsync(elsewhere.Id)).FullName.Should().Be("Mostafa Cell");
    }

    [Fact]
    public async Task A_customer_without_a_real_name_does_not_blank_the_salons_label()
    {
        var phone = NewPhone();
        var salon = await CreateTestProviderWithServicesAsync();
        var entry = await SalonCallsThemAsync(salon, phone, "Mostafa", "Cell");

        // Signed up by OTP alone: the account holds the placeholder «مشتری 912…», which is not a name.
        var customerId = await SignUpAsCustomerAsync(phone, null, null);
        await BookAsync(salon, customerId);

        (await EntryAsync(entry.Id)).FullName.Should().Be("Mostafa Cell");
    }

    // ── helpers ──

    /// <summary>A mobile number no other test uses (digits from a fresh Guid, not an unseeded Random).</summary>
    private static string NewPhone()
    {
        var digits = new string(Guid.NewGuid().ToString("N").Where(char.IsDigit).ToArray()).PadRight(7, '0');
        return "+98912" + digits[..7];
    }

    private async Task<ProviderCustomer> SalonCallsThemAsync(Domain.Aggregates.Provider salon, string phone, string first, string last)
    {
        var entry = ProviderCustomer.Create(salon.Id, first, last, PhoneNumber.From(phone), null, CustomerSource.Contacts);
        await CreateEntityAsync(entry);
        return entry;
    }

    private async Task<ProviderCustomer> EntryAsync(Guid id)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await db.ProviderCustomers.AsNoTracking().SingleAsync(c => c.Id == id);
    }

    private async Task BookAsync(Domain.Aggregates.Provider salon, Guid customerId)
    {
        var service = await GetFirstServiceForProviderAsync(salon.Id.Value);
        AuthenticateAsUser(customerId, $"{customerId:N}@test.com");

        var day = SalonTime.Now.Date.AddDays(2);
        while (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            day = day.AddDays(1);

        var response = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = salon.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = salon.Id.Value,
            startTime = day.AddHours(10),
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    /// Signs the person up the way they really do: an OTP to their mobile, read from the captured SMS.
    private async Task<Guid> SignUpAsCustomerAsync(string phone, string? firstName, string? lastName)
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
}
