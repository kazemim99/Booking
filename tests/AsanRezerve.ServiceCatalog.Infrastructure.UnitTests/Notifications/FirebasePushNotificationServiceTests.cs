using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Infrastructure.Notifications.Push;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AsanRezerve.ServiceCatalog.Infrastructure.UnitTests.Notifications;

/// <summary>
/// How a push is addressed, fanned out, and reported.
/// </summary>
/// <remarks>
/// <para>The behaviour under test replaced a stub that returned
/// <c>(true, Guid.NewGuid(), null)</c> without sending anything. The most important assertions here are the
/// negative ones: that an unconfigured environment and a recipient with no device are reported honestly
/// rather than as deliveries.</para>
///
/// <para>The gateway is faked because the alternative is a Firebase project and a network; everything above
/// it — fan-out, retirement, how a partial failure is summarised — is ours and is what can be got wrong.</para>
/// </remarks>
public class FirebasePushNotificationServiceTests
{
    private readonly IDeviceTokenRegistry _devices = Substitute.For<IDeviceTokenRegistry>();
    private readonly IFirebaseMessagingGateway _gateway = Substitute.For<IFirebaseMessagingGateway>();
    private readonly FirebasePushNotificationService _service;

    private static readonly Guid Recipient = Guid.NewGuid();

    public FirebasePushNotificationServiceTests()
    {
        _gateway.IsConfigured.Returns(true);
        _service = new FirebasePushNotificationService(
            _devices, _gateway, NullLogger<FirebasePushNotificationService>.Instance);
    }

    [Fact]
    public async Task An_unconfigured_environment_reports_a_skip_not_a_delivery()
    {
        // The defect this whole class exists to prevent: a delivery log that says yes when nothing was sent.
        _gateway.IsConfigured.Returns(false);

        var (success, messageId, error) = await _service.SendPushAsync(Recipient, "عنوان", "متن");

        success.Should().BeFalse();
        messageId.Should().BeNull();
        error.Should().Be(PushUnavailable.NotConfigured);
        await _gateway.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default!);
    }

    [Fact]
    public async Task A_recipient_with_no_device_is_reported_as_unreachable()
    {
        GivenDevices();

        var (success, _, error) = await _service.SendPushAsync(Recipient, "عنوان", "متن");

        success.Should().BeFalse();
        error.Should().Be(PushUnavailable.NoDevice, "the dispatcher turns this into a skip, not a failure");
    }

    [Fact]
    public async Task Every_registered_device_receives_the_push()
    {
        GivenDevices("token-a", "token-b", "token-c");
        GivenGatewayAccepts();

        var (success, messageId, _) = await _service.SendPushAsync(Recipient, "عنوان", "متن");

        success.Should().BeTrue();
        messageId.Should().NotBeNull();
        await _gateway.Received(3).SendAsync(
            Arg.Any<string>(), "عنوان", "متن", Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task One_failing_device_does_not_stop_the_others()
    {
        GivenDevices("broken", "working");
        _gateway.SendAsync("broken", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new PushSendResult(false, null, "temporary failure"));
        _gateway.SendAsync("working", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new PushSendResult(true, "msg-1", null));

        var (success, messageId, _) = await _service.SendPushAsync(Recipient, "عنوان", "متن");

        success.Should().BeTrue("the person did receive it, on the handset that worked");
        messageId.Should().Be("msg-1");
    }

    [Fact]
    public async Task Failing_on_every_device_is_a_failure()
    {
        GivenDevices("a", "b");
        _gateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new PushSendResult(false, null, "gateway down"));

        var (success, _, error) = await _service.SendPushAsync(Recipient, "عنوان", "متن");

        success.Should().BeFalse();
        error.Should().NotBe(PushUnavailable.NoDevice, "this one should burn a retry — it may work next time");
    }

    [Fact]
    public async Task A_token_the_gateway_calls_dead_is_retired()
    {
        // Nothing else can tell us an app was uninstalled, so acting on this is the only thing keeping the
        // table from filling with addresses that will never be reachable.
        GivenDevices("stale");
        _gateway.SendAsync("stale", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new PushSendResult(false, null, "UNREGISTERED", TokenIsDead: true));

        await _service.SendPushAsync(Recipient, "عنوان", "متن");

        await _devices.Received(1).RetireAsync("stale", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_token_that_merely_failed_is_kept()
    {
        GivenDevices("flaky");
        _gateway.SendAsync("flaky", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new PushSendResult(false, null, "timeout"));

        await _service.SendPushAsync(Recipient, "عنوان", "متن");

        await _devices.DidNotReceiveWithAnyArgs().RetireAsync(default!, default!);
    }

    [Fact]
    public async Task A_dead_token_among_several_does_not_stop_the_rest()
    {
        GivenDevices("dead", "alive");
        _gateway.SendAsync("dead", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new PushSendResult(false, null, "UNREGISTERED", TokenIsDead: true));
        _gateway.SendAsync("alive", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new PushSendResult(true, "msg-2", null));

        var (success, _, _) = await _service.SendPushAsync(Recipient, "عنوان", "متن");

        success.Should().BeTrue();
        await _devices.Received(1).RetireAsync("dead", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private void GivenDevices(params string[] tokens)
    {
        var devices = tokens
            .Select(t => new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = Recipient,
                Token = t,
                Platform = DevicePlatform.Android,
            })
            .ToList();

        _devices.GetActiveForUserAsync(Recipient, Arg.Any<CancellationToken>())
            .Returns(devices);
    }

    private void GivenGatewayAccepts() =>
        _gateway.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(call => new PushSendResult(true, $"msg-{call.ArgAt<string>(0)}", null));
}
