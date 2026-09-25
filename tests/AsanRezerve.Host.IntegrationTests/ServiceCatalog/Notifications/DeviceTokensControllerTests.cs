using System.Net;
using System.Net.Http.Json;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The device endpoints, and the fact that they only ever act on the caller's own devices.
/// </summary>
/// <remarks>
/// The registry tests prove the storage rules. What is proved here is that the identity comes from the
/// token and not from the request — a device registered against somebody else's account, or somebody else's
/// phone silenced, are both things the API must make impossible rather than merely discourage.
/// </remarks>
[Collection(AsanRezerveHostTestCollection.Name)]
public class DeviceTokensControllerTests : ServiceCatalogIntegrationTestBase
{
    public DeviceTokensControllerTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Registering_attaches_the_device_to_the_caller()
    {
        var me = Guid.NewGuid();
        var token = NewToken();

        AuthenticateAsUser(me, "me@test.com");
        var response = await Client.PostAsJsonAsync("/api/v1/DeviceTokens", Body(token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ActiveTokensAsync(me)).Should().ContainSingle().Which.Should().Be(token);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_register_a_device()
    {
        ClearAuthentication();

        var response = await Client.PostAsJsonAsync("/api/v1/DeviceTokens", Body(NewToken()));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_blank_token_is_rejected()
    {
        AuthenticateAsUser(Guid.NewGuid(), "me@test.com");

        var response = await Client.PostAsJsonAsync("/api/v1/DeviceTokens", Body("   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signing_out_revokes_only_my_device()
    {
        var me = Guid.NewGuid();
        var token = NewToken();
        AuthenticateAsUser(me, "me@test.com");
        await Client.PostAsJsonAsync("/api/v1/DeviceTokens", Body(token));

        var response = await Client.DeleteAsync($"/api/v1/DeviceTokens?token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ActiveTokensAsync(me)).Should().BeEmpty();
    }

    [Fact]
    public async Task I_cannot_silence_somebody_elses_phone()
    {
        // The endpoint takes a token, so without caller scoping anyone holding one could stop another
        // person's notifications.
        var owner = Guid.NewGuid();
        var token = NewToken();
        AuthenticateAsUser(owner, "owner@test.com");
        await Client.PostAsJsonAsync("/api/v1/DeviceTokens", Body(token));

        AuthenticateAsUser(Guid.NewGuid(), "attacker@test.com");
        var response = await Client.DeleteAsync($"/api/v1/DeviceTokens?token={token}");

        // 204 either way, because saying "that token is not yours" would confirm it exists.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ActiveTokensAsync(owner)).Should().ContainSingle("the owner's device is untouched");
    }

    [Fact]
    public async Task Registering_the_same_device_under_a_new_account_moves_it()
    {
        var previousOwner = Guid.NewGuid();
        var newOwner = Guid.NewGuid();
        var token = NewToken();

        AuthenticateAsUser(previousOwner, "previous@test.com");
        await Client.PostAsJsonAsync("/api/v1/DeviceTokens", Body(token));

        AuthenticateAsUser(newOwner, "new@test.com");
        await Client.PostAsJsonAsync("/api/v1/DeviceTokens", Body(token));

        (await ActiveTokensAsync(previousOwner)).Should().BeEmpty();
        (await ActiveTokensAsync(newOwner)).Should().ContainSingle();
    }

    // ── helpers ──

    private static string NewToken() => $"fcm-{Guid.NewGuid():N}";

    private static object Body(string token) => new { token, platform = DevicePlatform.Android.ToString() };

    private async Task<List<string>> ActiveTokensAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IDeviceTokenRegistry>();
        return (await registry.GetActiveForUserAsync(userId)).Select(d => d.Token).ToList();
    }
}
