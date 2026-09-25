using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Host.IntegrationTests.Infrastructure.Fakes;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.People;

/// <summary>
/// No API names a person by the placeholder phone sign-in gave them (production QA 2026-09-23: the customer app's
/// confirm step read «ارائه‌دهنده 9123135143»; "the number must never be written anywhere").
/// </summary>
/// <remarks>
/// The production shape, reproduced end to end: the salon's owner signed up through the salon app's OTP with no
/// name, so their person is «ارائه‌دهنده» + national number, and they are the salon's only bookable member (the
/// owner membership carries no display name of its own). The 2026-09-22 fix covered the slot's staff name, but the
/// salon page's staff list rebuilt the name from the raw first/last parts, and the customer app auto-selects that
/// single member — so the confirm step printed it anyway.
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class NoPhoneNumberAsANameTests : ServiceCatalogIntegrationTestBase
{
    private const string SalonName = "سالن نهال";
    private const string PlaceholderWord = "ارائه";

    public NoPhoneNumberAsANameTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private static string RandomMobile() => $"+98912{Random.Shared.Next(1000000, 9999999)}";

    /// <summary>The digits the placeholder carries: the phone's national number («9123135143»).</summary>
    private static string NationalOf(string phone) => PhoneNumber.From(phone).NationalNumber;

    private static JToken Data(string body)
    {
        var root = JToken.Parse(body);
        return root is JObject o && o["data"] is { } data ? data : root;
    }

    /// <summary>Signs up the way a real person does — an OTP read from the SMS — with whatever name they gave.</summary>
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

    /// <summary>A bookable salon, open every day, whose owner (and only member) is <paramref name="ownerId"/>.</summary>
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

    /// <summary>The production owner: signed up on the salon app with nothing but a phone.</summary>
    private async Task<(Provider Salon, Service Service, Guid MemberId, string Phone)> SalonWithPlaceholderOwnerAsync()
    {
        var phone = RandomMobile();
        var ownerId = await SignUpByOtpAsync("provider", phone);
        var (salon, service, memberId) = await SalonOwnedByAsync(ownerId);
        return (salon, service, memberId, phone);
    }

    private static void ShouldNotNameAnyoneByTheirNumber(JToken token, string phone, string because)
    {
        var text = token.ToString(Newtonsoft.Json.Formatting.None);
        text.Should().NotContain(NationalOf(phone), because);
        text.Should().NotContain(PlaceholderWord, because);
    }

    private static DateTime DayAfterTomorrowAt(int hour) => DateTime.UtcNow.Date.AddDays(2).AddHours(hour);

    [Fact]
    public async Task A_salon_whose_owner_has_only_the_OTP_placeholder_is_named_by_the_salon_on_its_page()
    {
        var (salon, _, memberId, phone) = await SalonWithPlaceholderOwnerAsync();

        // Exactly what the customer app asks for when it opens the salon and starts a booking.
        var response = await Client.GetAsync(
            $"/api/v1/providers/{salon.Id.Value}?includeServices=true&includeStaff=true");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var staff = (JArray)Data(body)["staff"]!;
        staff.Should().ContainSingle();
        var member = staff[0];
        member["id"]!.Value<string>().Should().Be(memberId.ToString());
        member["fullName"]!.Value<string>().Should().Be(SalonName,
            "a member with no real name is named by the salon, never by «ارائه‌دهنده <digits>»");
        (member["firstName"]?.Value<string>() ?? string.Empty).Should().BeEmpty(
            "a client joining the parts must not be able to rebuild the placeholder");
        (member["lastName"]?.Value<string>() ?? string.Empty).Should().BeEmpty(
            "the placeholder's surname is the phone number");
        ShouldNotNameAnyoneByTheirNumber(staff, phone, "the salon page must not carry the owner's number as a name");
    }

    [Fact]
    public async Task The_slots_and_the_qualified_staff_name_the_salon_too()
    {
        var (salon, service, _, phone) = await SalonWithPlaceholderOwnerAsync();
        var day = DateTime.UtcNow.Date.AddDays(2);
        AuthenticateAsUser(Guid.NewGuid(), $"{Guid.NewGuid():N}@test.com");

        var slots = await Client.GetAsync(
            $"/api/v1/bookings/available-slots?providerId={salon.Id.Value}&serviceId={service.Id.Value}&date={day:yyyy-MM-dd}");
        var slotsBody = await slots.Content.ReadAsStringAsync();
        slots.StatusCode.Should().Be(HttpStatusCode.OK, slotsBody);
        var offered = (JArray)Data(slotsBody)["availableSlots"]!;
        offered.Should().NotBeEmpty();
        offered.Select(s => s["availableStaffName"]!.Value<string>()).Should().OnlyContain(n => n == SalonName);
        ShouldNotNameAnyoneByTheirNumber(offered, phone, "a slot names who does it");

        var qualified = await Client.GetAsync(
            $"/api/v1/services/provider/{salon.Id.Value}/{service.Id.Value}/qualified-staff");
        var qualifiedBody = await qualified.Content.ReadAsStringAsync();
        qualified.StatusCode.Should().Be(HttpStatusCode.OK, qualifiedBody);
        var people = (JArray)Data(qualifiedBody)["qualifiedStaff"]!;
        people.Should().ContainSingle();
        people[0]["name"]!.Value<string>().Should().Be(SalonName);
        ShouldNotNameAnyoneByTheirNumber(people, phone, "the Vue booking flow's staff picker reads this");
    }

    [Fact]
    public async Task The_salons_own_roster_never_uses_the_number_as_the_owners_name()
    {
        var (salon, _, memberId, phone) = await SalonWithPlaceholderOwnerAsync();
        AuthenticateAsProviderOwner(salon);

        var members = await Client.GetAsync($"/api/v1/providers/{salon.Id.Value}/hierarchy/members");
        var membersBody = await members.Content.ReadAsStringAsync();
        members.StatusCode.Should().Be(HttpStatusCode.OK, membersBody);
        var owner = ((JArray)Data(membersBody)["members"]!)
            .Single(m => m["membershipId"]!.Value<string>() == memberId.ToString());
        owner["name"]!.Value<string>().Should().BeEmpty(
            "the salon app labels a nameless member itself; the placeholder is not a name");
        owner["phoneNumber"]!.Value<string>().Should().Be(PhoneNumber.From(phone).Value,
            "the number is still there — as the member's phone");

        var roster = await Client.GetAsync($"/api/v1/providers/{salon.Id.Value}/staff");
        var rosterBody = await roster.Content.ReadAsStringAsync();
        roster.StatusCode.Should().Be(HttpStatusCode.OK, rosterBody);
        var row = ((JArray)Data(rosterBody)).Single(s => s["id"]!.Value<string>() == memberId.ToString());
        row["fullName"]!.Value<string>().Should().Be(SalonName);
        (row["firstName"]?.Value<string>() ?? string.Empty).Should().BeEmpty();
        (row["lastName"]?.Value<string>() ?? string.Empty).Should().BeEmpty();
    }

    [Fact]
    public async Task A_customer_who_gave_no_name_is_nameless_in_the_client_book_not_a_number()
    {
        var owner = await CreateAndAuthenticateAsRealUserAsync($"{Guid.NewGuid():N}@test.com");
        var (salon, service, memberId) = await SalonOwnedByAsync(owner);

        // A customer signed up by OTP and skipped the name: stored as «مشتری <digits>».
        var customerPhone = RandomMobile();
        var customer = await SignUpByOtpAsync("customer", customerPhone);
        AuthenticateAsUser(customer, $"{Guid.NewGuid():N}@test.com");
        var booked = await Client.PostAsJsonAsync("/api/v1/bookings", new
        {
            providerId = salon.Id.Value,
            serviceId = service.Id.Value,
            staffProviderId = memberId,
            startTime = DayAfterTomorrowAt(10),
        });
        booked.StatusCode.Should().Be(HttpStatusCode.Created, await booked.Content.ReadAsStringAsync());

        AuthenticateAsProviderOwner(salon);
        var clients = await Client.GetAsync($"/api/v1/providers/{salon.Id.Value}/clients");
        var body = await clients.Content.ReadAsStringAsync();
        clients.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var row = ((JArray)Data(body)["clients"]!).Single(c => c["customerId"]!.Value<string>() == customer.ToString());
        row["name"]!.Value<string>().Should().BeEmpty("«مشتری 912…» is not a name; the salon app shows its own label");
        row["phone"]!.Value<string>().Should().Be(PhoneNumber.From(customerPhone).Value, "the phone stays a phone");
    }

    [Fact]
    public async Task A_notification_to_a_customer_without_a_name_does_not_store_their_number_as_one()
    {
        var customer = await SignUpByOtpAsync("customer", RandomMobile());

        using (var scope = Factory.Services.CreateScope())
        {
            var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();
            await raiser.RaiseAsync(NotificationEventCode.BookingConfirmed, customer, Guid.NewGuid(),
                new Dictionary<string, string> { ["businessName"] = SalonName });
            await scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>().SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>().ExecuteAsync();

        DbContext.ChangeTracker.Clear();
        var sent = await GetUserNotificationsAsync(customer);
        sent.Should().NotBeEmpty();
        sent.Select(n => n.RecipientName).Should().OnlyContain(n => n == null,
            "«مشتری <digits>» is the phone number, not the recipient's name");
    }
}
