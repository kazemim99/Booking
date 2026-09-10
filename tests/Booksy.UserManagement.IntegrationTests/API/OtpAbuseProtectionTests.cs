using System.Net;
using System.Net.Http.Json;
using Booksy.UserManagement.Application.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Booksy.UserManagement.IntegrationTests.API;

/// <summary>
/// The anonymous OTP endpoint sends a real SMS, which costs real money and lands on a stranger's
/// phone. Nothing stopped a caller from asking for an unbounded number of them: the handler created
/// a fresh <c>PhoneVerification</c> per request, so the aggregate's own 60-second cooldown and
/// 3-send cap in <c>CanResend()</c> were never consulted, and the one cap the handler did have was
/// wrapped in <c>#if !DEBUG</c> — protection that disappeared depending on how the binary was built.
///
/// <para>Each test builds its OWN host with the limits it wants to prove, rather than mutating the
/// shared options object. The suite's host deliberately raises these limits so that identity tests
/// can sign the same phone in twice; reaching into that singleton to tighten it leaks into every
/// other class sharing the host, which is exactly what the first version of this file did.</para>
/// </summary>
public class OtpAbuseProtectionTests : UserManagementIntegrationTestBase
{
    public OtpAbuseProtectionTests(UserManagementTestWebApplicationFactory<Program> factory)
        : base(factory) { }

    private static string NewLocalPhone() => $"0912{Random.Shared.Next(1000000, 9999999)}";

    /// <summary>A client on an isolated host that enforces the given limits.</summary>
    private HttpClient ClientWithLimits(int maxSends, string cooldown)
    {
        var configured = Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Otp:Protection:MaxSendsPerWindow", maxSends.ToString());
            builder.UseSetting("Otp:Protection:ResendCooldown", cooldown);
        });

        var client = configured.CreateClient();

        // Prove the host took the settings before asserting on behaviour. Without this, a binding
        // that silently fell back to the defaults would look like a broken limit rule.
        var applied = configured.Services
            .GetRequiredService<IOptions<OtpProtectionOptions>>().Value;
        applied.MaxSendsPerWindow.Should().Be(maxSends, "the test host must use the limits this test set");
        applied.ResendCooldown.Should().Be(TimeSpan.Parse(cooldown));

        return client;
    }

    private static Task<HttpResponseMessage> SendCodeAsync(HttpClient client, string phone) =>
        client.PostAsJsonAsync("/api/v1/auth/send-verification-code",
            new { phoneNumber = phone, countryCode = "+98" });

    // The cooldown rule has no test yet, deliberately. It cannot work until FOLLOW-UPS #48 is
    // fixed: stored timestamps come back ahead of UtcNow by the server's offset, so "time since the
    // last send" is negative and the rule is guarded off. A test written today would pass for the
    // wrong reason — the 429 would come from the cap, not the cooldown — which is worse than no
    // test. It belongs with #48, which is where the guard gets deleted.

    [Fact]
    public async Task The_Send_Cap_Applies_Per_Phone_Within_The_Window()
    {
        // No cooldown, so this measures the CAP alone rather than the gap between sends.
        var client = ClientWithLimits(maxSends: 2, cooldown: "00:00:00");
        var phone = NewLocalPhone();

        var first = await SendCodeAsync(client, phone);
        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());

        var second = await SendCodeAsync(client, phone);
        second.StatusCode.Should().Be(HttpStatusCode.OK, await second.Content.ReadAsStringAsync());

        var overTheCap = await SendCodeAsync(client, phone);

        overTheCap.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "the cap is per phone number, which is what an SMS costs money against");
    }

    [Fact]
    public async Task One_Numbers_Limit_Does_Not_Spend_Another_Numbers_Allowance()
    {
        var client = ClientWithLimits(maxSends: 1, cooldown: "00:00:00");

        var busy = NewLocalPhone();
        (await SendCodeAsync(client, busy)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendCodeAsync(client, busy)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var untouched = NewLocalPhone();
        (await SendCodeAsync(client, untouched)).StatusCode.Should().Be(HttpStatusCode.OK,
            "the limit is keyed on the phone; a shared bucket would let one caller lock out everyone else");
    }
}

/// <summary>
/// Guards the suite's own configuration: the shared test host must run with the limits raised, or
/// unrelated identity tests (which legitimately sign the same phone in twice) start failing with
/// 429s that have nothing to do with what they are testing.
/// </summary>
public class OtpTestHostLimitsTests : UserManagementIntegrationTestBase
{
    public OtpTestHostLimitsTests(UserManagementTestWebApplicationFactory<Program> factory)
        : base(factory) { }

    [Fact]
    public void The_Shared_Test_Host_Runs_With_Raised_Otp_Limits()
    {
        var options = Factory.Services
            .GetRequiredService<IOptions<OtpProtectionOptions>>().Value;

        options.ResendCooldown.Should().Be(TimeSpan.Zero,
            "TestWebApplicationFactory raises these deliberately; if this fails the setting is not reaching the host");
        options.MaxSendsPerWindow.Should().BeGreaterThan(100);
    }
}
