using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AsanRezerve.Core.Application.Services.Notifications;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.Host.IntegrationTests.Infrastructure.Fakes;
using AsanRezerve.UserManagement.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.UserManagement.IntegrationTests.API;

/// <summary>
/// The phone-first OTP sign-in is the production login for both apps, and until now nothing
/// proved it end to end through the UserManagement host: that the code is issued, that
/// completing it creates exactly one person, that the refresh token it hands out is persisted
/// and rotates, and that the same phone signing in as a customer reuses that same person.
/// The code is read from the SMS the test host's fake gateway captured, exactly as a user would
/// read it; the aggregate persists only the code's hash.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class OtpAuthenticationLifecycleTests : UserManagementIntegrationTestBase
{
    public OtpAuthenticationLifecycleTests(AsanRezerveHostFactory factory)
        : base(factory) { }

    private static string NewLocalPhone() => $"0912{Random.Shared.Next(1000000, 9999999)}";

    private static JsonElement Payload(JsonElement root) =>
        root.TryGetProperty("data", out var data) ? data : root;

    private async Task<string> SendCodeAndReadItAsync(string phone)
    {
        ClearAuthenticationHeader();
        var send = await Client.PostAsJsonAsync("/api/v1/auth/send-verification-code",
            new { phoneNumber = phone, countryCode = "+98" });
        send.StatusCode.Should().Be(HttpStatusCode.OK, await send.Content.ReadAsStringAsync());

        // PhoneVerification persists only the OTP hash, so the code is read the way a real
        // user reads it: from the SMS, which the test host's fake gateway captures.
        var canonical = PhoneNumber.From(phone).Value;
        var sms = (FakeSmsNotificationService)Factory.Services.GetRequiredService<ISmsNotificationService>();
        var message = sms.LastMessageTo(canonical) ?? sms.LastMessageTo(phone);
        message.Should().NotBeNull("the send-code handler must hand the SMS gateway a message for this phone");
        var code = System.Text.RegularExpressions.Regex.Match(message!, @"\d{4,8}").Value;
        code.Should().NotBeEmpty("the SMS must carry the code");
        return code;
    }

    private Task<HttpResponseMessage> CompleteAsync(string audience, string phone, string code) =>
        Client.PostAsJsonAsync($"/api/v1/auth/{audience}/complete-authentication",
            new { phoneNumber = phone, code, firstName = "Otp", lastName = "Person" });

    [Fact]
    public async Task Provider_Signin_Creates_One_Person_And_A_Working_Refresh_Token()
    {
        var phone = NewLocalPhone();
        var code = await SendCodeAndReadItAsync(phone);

        var complete = await CompleteAsync("provider", phone, code);
        complete.StatusCode.Should().Be(HttpStatusCode.OK, await complete.Content.ReadAsStringAsync());
        var body = Payload(JsonDocument.Parse(await complete.Content.ReadAsStringAsync()).RootElement);
        var userId = body.GetProperty("userId").GetGuid();
        var refreshToken = body.GetProperty("refreshToken").GetString();
        refreshToken.Should().NotBeNullOrWhiteSpace();

        // Exactly one person for the phone, carrying the canonical number.
        var canonical = PhoneNumber.From(phone).Value;
        DbContext.ChangeTracker.Clear();
        var people = await DbContext.Users
            .Include(u => u.RefreshTokens)
            .Where(u => u.PhoneNumber != null && u.PhoneNumber.Value == canonical)
            .ToListAsync();
        people.Should().ContainSingle();
        people[0].Id.Value.Should().Be(userId);
        people[0].RefreshTokens.Should().Contain(t => t.Token == refreshToken,
            "the sign-in handler must commit the refresh token it hands out");

        // The handed-out token refreshes, and the refresh rotates it durably.
        var refresh = await Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK, await refresh.Content.ReadAsStringAsync());
        var rotated = Payload(JsonDocument.Parse(await refresh.Content.ReadAsStringAsync()).RootElement)
            .GetProperty("refreshToken").GetString();
        rotated.Should().NotBe(refreshToken);
        (await Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the rotated-away token is revoked");
        (await Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = rotated }))
            .StatusCode.Should().Be(HttpStatusCode.OK, "the rotated token was stored");
    }

    [Fact]
    public async Task The_Same_Phone_Signing_In_As_Customer_Reuses_The_Person()
    {
        var phone = NewLocalPhone();

        var providerCode = await SendCodeAndReadItAsync(phone);
        var asProvider = await CompleteAsync("provider", phone, providerCode);
        asProvider.StatusCode.Should().Be(HttpStatusCode.OK, await asProvider.Content.ReadAsStringAsync());
        var providerUserId = Payload(JsonDocument.Parse(await asProvider.Content.ReadAsStringAsync()).RootElement)
            .GetProperty("userId").GetGuid();

        var customerCode = await SendCodeAndReadItAsync(phone);
        var asCustomer = await CompleteAsync("customer", phone, customerCode);
        asCustomer.StatusCode.Should().Be(HttpStatusCode.OK, await asCustomer.Content.ReadAsStringAsync());
        var customerUserId = Payload(JsonDocument.Parse(await asCustomer.Content.ReadAsStringAsync()).RootElement)
            .GetProperty("userId").GetGuid();

        customerUserId.Should().Be(providerUserId, "one person per phone, whichever side of the marketplace they use");

        var canonical = PhoneNumber.From(phone).Value;
        DbContext.ChangeTracker.Clear();
        var person = await DbContext.Users.SingleAsync(u => u.PhoneNumber != null && u.PhoneNumber.Value == canonical);
        person.Type.Should().Be(UserType.Both, "the second capacity is granted to the same account, not a second account");
    }

    [Fact]
    public async Task Malformed_Input_Is_A_Client_Error_Not_A_500()
    {
        ClearAuthenticationHeader();

        var badPhone = await Client.PostAsJsonAsync("/api/v1/auth/send-verification-code",
            new { phoneNumber = "not-a-phone", countryCode = "+98" });
        badPhone.StatusCode.Should().Be(HttpStatusCode.BadRequest, await badPhone.Content.ReadAsStringAsync());

        // The completion actions used to catch Exception and answer 500 for anything that was
        // not an InvalidOperationException, which hid validation and domain errors from clients.
        var badComplete = await CompleteAsync("provider", "not-a-phone", "123456");
        ((int)badComplete.StatusCode).Should().BeLessThan(500, await badComplete.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_Wrong_Code_Is_Rejected_And_Creates_Nobody()
    {
        var phone = NewLocalPhone();
        await SendCodeAndReadItAsync(phone);

        var complete = await CompleteAsync("provider", phone, "000000");
        complete.IsSuccessStatusCode.Should().BeFalse();

        var canonical = PhoneNumber.From(phone).Value;
        DbContext.ChangeTracker.Clear();
        (await DbContext.Users.AnyAsync(u => u.PhoneNumber != null && u.PhoneNumber.Value == canonical))
            .Should().BeFalse("no account may be provisioned before the phone is proven");
    }
}
