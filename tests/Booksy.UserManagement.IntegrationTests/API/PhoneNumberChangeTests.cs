using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.UserManagement.IntegrationTests.API;

/// <summary>
/// refactor-identity-and-membership §1.8, real HTTP boundary. Proves the two live endpoints
/// behind the Vue provider dashboard's "PhoneVerificationModal" actually work end to end against
/// a real database, not just at the handler-unit level (<c>PhoneNumberChangeTests</c> in
/// UserManagement.Application.UnitTests) -- both routes previously threw on every call because
/// their MediatR handlers were entirely commented out.
/// </summary>
[Collection(UserManagementTestCollection.Name)]
public class PhoneNumberChangeTests : UserManagementIntegrationTestBase
{
    public PhoneNumberChangeTests(UserManagementTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    private async Task<User> CreateRealUserAsync(string phone)
    {
        var profile = UserProfile.Create("Test", "Owner", middleName: null, dateOfBirth: null, gender: null);
        var user = User.RegisterWithPhone(
            Email.Create($"{Guid.NewGuid():N}@booksy.test"),
            PhoneNumber.From(phone),
            profile,
            UserType.Provider);

        await CreateEntityAsync(user);
        return user;
    }

    /// <summary>
    /// The plaintext OTP is never persisted (only its hash), so the only way to recover the
    /// code a real send-verification call generated is to read the message the fake SMS
    /// service actually captured, exactly as a human would read it off their phone.
    /// </summary>
    private string ReadSentCode(string phoneE164)
    {
        var fakeSms = (FakeSmsNotificationService)Factory.Services.GetRequiredService<ISmsNotificationService>();
        var message = fakeSms.LastMessageTo(phoneE164);
        message.Should().NotBeNull("the send-verification call should have sent an SMS to this number");
        var match = Regex.Match(message!, @"\d{6}");
        match.Success.Should().BeTrue("the SMS body should contain a 6-digit code");
        return match.Value;
    }

    [Fact]
    public async Task Send_Then_Verify_With_The_Right_Code_Changes_The_Users_Phone_Number()
    {
        var user = await CreateRealUserAsync("09121111111");
        AuthenticateAsCustomerWithId(user.Id.Value);
        const string newPhone = "09129998877";

        var sendResponse = await Client.PostAsJsonAsync(
            $"/api/v1/users/{user.Id.Value}/phone/send-verification",
            new { PhoneNumber = newPhone });
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var code = ReadSentCode(PhoneNumber.From(newPhone).Value);

        var verifyResponse = await Client.PostAsJsonAsync(
            $"/api/v1/users/{user.Id.Value}/phone/verify",
            new { PhoneNumber = newPhone, VerificationCode = code });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await verifyResponse.Content.ReadFromJsonAsync<VerifyPhoneCodeResponseBody>();
        body!.Success.Should().BeTrue();

        // The HTTP request updated the row through its own DbContext instance/scope; this
        // fixture's long-lived DbContext still has the ORIGINAL `user` tracked from
        // CreateEntityAsync, and EF's identity map would hand that stale in-memory instance
        // back on a plain query instead of the row's current values. Clear tracking first so
        // this actually re-reads from the database.
        DbContext.ChangeTracker.Clear();
        var persisted = await FindUserAsync(user.Id.Value);
        persisted!.PhoneNumber!.Value.Should().Be(PhoneNumber.From(newPhone).Value);
        persisted.PhoneNumberVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_With_The_Wrong_Code_Leaves_The_Users_Phone_Number_Unchanged()
    {
        var user = await CreateRealUserAsync("09121111112");
        AuthenticateAsCustomerWithId(user.Id.Value);
        const string newPhone = "09129998878";

        var sendResponse = await Client.PostAsJsonAsync(
            $"/api/v1/users/{user.Id.Value}/phone/send-verification",
            new { PhoneNumber = newPhone });
        sendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResponse = await Client.PostAsJsonAsync(
            $"/api/v1/users/{user.Id.Value}/phone/verify",
            new { PhoneNumber = newPhone, VerificationCode = "000000" });

        // UsersController.VerifyPhoneCode maps a domain-level "not verified" result to 400,
        // not a 200 with Success:false in the body -- unlike the handler-level unit tests,
        // which assert on the VerifyPhoneCodeResult directly.
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        DbContext.ChangeTracker.Clear();
        var persisted = await FindUserAsync(user.Id.Value);
        persisted!.PhoneNumber!.Value.Should().Be(PhoneNumber.From("09121111112").Value);
    }

    [Fact]
    public async Task A_Different_User_Cannot_Request_A_Phone_Change_For_Someone_Elses_Account()
    {
        var owner = await CreateRealUserAsync("09121111113");
        var stranger = await CreateRealUserAsync("09121111114");
        AuthenticateAsCustomerWithId(stranger.Id.Value);

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/users/{owner.Id.Value}/phone/send-verification",
            new { PhoneNumber = "09129998879" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record VerifyPhoneCodeResponseBody(bool Success, string Message, string? PhoneNumber, DateTime? VerifiedAt);
}
