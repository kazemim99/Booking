using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The push channel's address book, against a real database.
/// </summary>
/// <remarks>
/// <para>The push fan-out tests fake this registry, which proves nothing about it. What lives here is the
/// behaviour the database actually enforces: one row per device, a handset that changes hands moving with
/// its new owner, and revocation that cannot reach somebody else's phone.</para>
///
/// <para>The reassignment rule is the one that matters. A registration token identifies an app
/// installation, not an account — so if it were duplicated instead of moved, the previous owner's
/// notifications would keep arriving on a handset that is no longer theirs.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class DeviceTokenRegistryTests : ServiceCatalogIntegrationTestBase
{
    public DeviceTokenRegistryTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task A_device_is_registered_and_becomes_reachable()
    {
        var user = Guid.NewGuid();
        var token = NewToken();

        await RegisterAsync(user, token, DevicePlatform.Android);

        var active = await ActiveTokensAsync(user);
        active.Should().ContainSingle().Which.Should().Be(token);
    }

    [Fact]
    public async Task Re_registering_the_same_device_refreshes_it_instead_of_adding_a_second_row()
    {
        // The app re-registers on every start, and the OS can rotate a token at any time.
        var user = Guid.NewGuid();
        var token = NewToken();

        await RegisterAsync(user, token, DevicePlatform.Android);
        await RegisterAsync(user, token, DevicePlatform.Android);
        await RegisterAsync(user, token, DevicePlatform.Android);

        (await RowsForTokenAsync(token)).Should().HaveCount(1);
        (await ActiveTokensAsync(user)).Should().ContainSingle();
    }

    [Fact]
    public async Task Re_registering_updates_the_platform_and_last_seen()
    {
        var user = Guid.NewGuid();
        var token = NewToken();

        await RegisterAsync(user, token, DevicePlatform.Android);
        var first = (await RowsForTokenAsync(token)).Single();

        await RegisterAsync(user, token, DevicePlatform.Ios);
        var second = (await RowsForTokenAsync(token)).Single();

        second.Id.Should().Be(first.Id, "it is the same device, not a new one");
        second.Platform.Should().Be(DevicePlatform.Ios);
        second.LastSeenAt.Should().BeOnOrAfter(first.LastSeenAt);
    }

    [Fact]
    public async Task A_handset_that_changes_hands_moves_to_its_new_owner()
    {
        // Otherwise the previous owner's notifications keep arriving on a phone that is not theirs.
        var previousOwner = Guid.NewGuid();
        var newOwner = Guid.NewGuid();
        var token = NewToken();

        await RegisterAsync(previousOwner, token, DevicePlatform.Android);
        await RegisterAsync(newOwner, token, DevicePlatform.Android);

        (await ActiveTokensAsync(previousOwner)).Should().BeEmpty();
        (await ActiveTokensAsync(newOwner)).Should().ContainSingle().Which.Should().Be(token);
        (await RowsForTokenAsync(token)).Should().HaveCount(1, "the unique index forbids a second row");
    }

    [Fact]
    public async Task Revoking_stops_push_to_that_device()
    {
        var user = Guid.NewGuid();
        var token = NewToken();
        await RegisterAsync(user, token, DevicePlatform.Android);

        var revoked = await RevokeAsync(user, token);

        revoked.Should().BeTrue();
        (await ActiveTokensAsync(user)).Should().BeEmpty();
    }

    [Fact]
    public async Task Revoking_leaves_the_other_devices_alone()
    {
        var user = Guid.NewGuid();
        var phone = NewToken();
        var tablet = NewToken();
        await RegisterAsync(user, phone, DevicePlatform.Android);
        await RegisterAsync(user, tablet, DevicePlatform.Ios);

        await RevokeAsync(user, phone);

        (await ActiveTokensAsync(user)).Should().ContainSingle().Which.Should().Be(tablet);
    }

    [Fact]
    public async Task I_cannot_revoke_somebody_elses_device()
    {
        // Revoking by token alone would let anyone silence anyone else's phone.
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var token = NewToken();
        await RegisterAsync(owner, token, DevicePlatform.Android);

        var revoked = await RevokeAsync(attacker, token);

        revoked.Should().BeFalse();
        (await ActiveTokensAsync(owner)).Should().ContainSingle("the owner's device is untouched");
    }

    [Fact]
    public async Task Revoking_twice_is_harmless()
    {
        var user = Guid.NewGuid();
        var token = NewToken();
        await RegisterAsync(user, token, DevicePlatform.Android);

        (await RevokeAsync(user, token)).Should().BeTrue();
        (await RevokeAsync(user, token)).Should().BeFalse("already revoked, and that is not an error");
    }

    [Fact]
    public async Task A_token_the_gateway_rejected_stops_being_used()
    {
        var user = Guid.NewGuid();
        var token = NewToken();
        await RegisterAsync(user, token, DevicePlatform.Android);

        await RetireAsync(token, "UNREGISTERED");

        (await ActiveTokensAsync(user)).Should().BeEmpty();
    }

    [Fact]
    public async Task A_returning_device_becomes_reachable_again()
    {
        // Signed out and back in, or a token the gateway rejected that is now valid. It must not need a
        // second row.
        var user = Guid.NewGuid();
        var token = NewToken();
        await RegisterAsync(user, token, DevicePlatform.Android);
        await RevokeAsync(user, token);

        await RegisterAsync(user, token, DevicePlatform.Android);

        (await ActiveTokensAsync(user)).Should().ContainSingle().Which.Should().Be(token);
        (await RowsForTokenAsync(token)).Should().HaveCount(1);
    }

    [Fact]
    public async Task A_blank_token_is_refused()
    {
        var register = async () => await RegisterAsync(Guid.NewGuid(), "   ", DevicePlatform.Android);

        await register.Should().ThrowAsync<ArgumentException>();
    }

    // ── helpers ──

    private static string NewToken() => $"fcm-{Guid.NewGuid():N}";

    private async Task RegisterAsync(Guid userId, string token, DevicePlatform platform)
    {
        using var scope = Factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IDeviceTokenRegistry>();
        await registry.RegisterAsync(userId, token, platform);
    }

    private async Task<bool> RevokeAsync(Guid userId, string token)
    {
        using var scope = Factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IDeviceTokenRegistry>();
        return await registry.RevokeAsync(userId, token);
    }

    private async Task RetireAsync(string token, string reason)
    {
        using var scope = Factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IDeviceTokenRegistry>();
        await registry.RetireAsync(token, reason);
    }

    private async Task<List<string>> ActiveTokensAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IDeviceTokenRegistry>();
        return (await registry.GetActiveForUserAsync(userId)).Select(d => d.Token).ToList();
    }

    private async Task<List<DeviceToken>> RowsForTokenAsync(string token)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.DeviceTokens.AsNoTracking().Where(d => d.Token == token).ToListAsync();
    }
}
