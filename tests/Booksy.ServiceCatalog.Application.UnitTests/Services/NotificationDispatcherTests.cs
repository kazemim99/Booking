using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Booksy.ServiceCatalog.Application.UnitTests.Services;

/// <summary>
/// C7 notification-delivery-reliability. The dispatcher is the single send path, so the delivery guarantees are
/// verified here: every named channel is attempted (not just the first), a channel the recipient disabled is
/// skipped, a de-duplicated tuple is never sent twice, and a failing send stays retryable until the budget runs
/// out and the notification is dead-lettered.
/// </summary>
public class NotificationDispatcherTests
{
    private readonly IEmailNotificationService _email = Substitute.For<IEmailNotificationService>();
    private readonly ISmsNotificationService _sms = Substitute.For<ISmsNotificationService>();
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();
    private readonly IInAppNotificationService _inApp = Substitute.For<IInAppNotificationService>();
    private readonly IUserNotificationPreferencesRepository _preferences =
        Substitute.For<IUserNotificationPreferencesRepository>();
    private readonly INotificationDeliveryLog _deliveryLog = Substitute.For<INotificationDeliveryLog>();

    private readonly UserId _recipient = UserId.From(Guid.NewGuid());

    public NotificationDispatcherTests()
    {
        _email.SendEmailAsync(default!, default!, default!, default, default, default, default)
            .ReturnsForAnyArgs((true, "email-1", (string?)null));
        _sms.SendSmsAsync(default!, default!, default, default)
            .ReturnsForAnyArgs((true, "sms-1", (string?)null));
        _push.SendPushAsync(default(Guid), default!, default!, default, default)
            .ReturnsForAnyArgs((true, "push-1", (string?)null));
        _inApp.SendToUserAsync(default, default!, default!, default!, default, default)
            .ReturnsForAnyArgs((true, (string?)null));

        _preferences.GetByUserIdAsync(default!, default).ReturnsForAnyArgs((UserNotificationPreferences?)null);
        _deliveryLog.TryClaimAsync(default, default, default!, default, default)
            .ReturnsForAnyArgs(NotificationDeliveryClaim.Claimed);
    }

    private NotificationDispatcher CreateSut(bool reliableDispatch = true) =>
        new(_email, _sms, _push, _inApp, _preferences, _deliveryLog,
            new NotificationDispatchOptions { ReliableDispatch = reliableDispatch },
            Substitute.For<ILogger<NotificationDispatcher>>());

    private Notification MultiChannelNotification(
        NotificationType type = NotificationType.BookingConfirmation,
        NotificationChannel channels = NotificationChannel.Email | NotificationChannel.SMS | NotificationChannel.InApp)
        => Notification.CreateImmediate(
            _recipient, type, channels, "subject", "body", NotificationPriority.High,
            plainTextBody: "plain", recipientEmail: "customer@test.com", recipientPhone: "+989123456789");

    private static UserNotificationPreferences PreferencesWith(NotificationChannel enabled)
    {
        var prefs = UserNotificationPreferences.CreateDefault(UserId.From(Guid.NewGuid()));
        prefs.UpdatePreferences(NotificationPreference.Create(enabled, NotificationType.All));
        return prefs;
    }

    // ---------------------------------------------------------------- Per-channel fan-out

    /// <summary>
    /// Lifecycle handlers request Email|SMS|InApp in a single value. The old dispatch switched on that combined
    /// value, matched no case, and threw "channel is not supported" — so every multi-channel notification failed
    /// wholesale and none of the three was ever sent.
    /// </summary>
    [Fact]
    public async Task Every_channel_named_by_a_combined_value_is_sent()
    {
        var sut = CreateSut();
        var notification = MultiChannelNotification();

        var outcome = await sut.DispatchAsync(notification);

        outcome.Delivered.Should().Be(3);
        outcome.Failed.Should().Be(0);
        notification.Status.Should().Be(NotificationStatus.Delivered);

        await _email.ReceivedWithAnyArgs(1).SendEmailAsync(default!, default!, default!, default, default, default, default);
        await _sms.ReceivedWithAnyArgs(1).SendSmsAsync(default!, default!, default, default);
        await _inApp.ReceivedWithAnyArgs(1).SendToUserAsync(default, default!, default!, default!, default, default);
    }

    /// <summary>
    /// A channel with no address for this recipient cannot be retried into working, so it is skipped rather than
    /// failed — otherwise a customer with no e-mail on file would burn the retry budget and dead-letter a
    /// notification that was delivered by SMS.
    /// </summary>
    [Fact]
    public async Task A_channel_without_a_recipient_address_is_skipped_not_failed()
    {
        var sut = CreateSut();
        var notification = Notification.CreateImmediate(
            _recipient, NotificationType.BookingConfirmation,
            NotificationChannel.Email | NotificationChannel.SMS,
            "subject", "body", NotificationPriority.High,
            recipientEmail: null, recipientPhone: "+989123456789");

        var outcome = await sut.DispatchAsync(notification);

        outcome.Delivered.Should().Be(1);
        outcome.Skipped.Should().Be(1);
        outcome.Failed.Should().Be(0);
        notification.Status.Should().Be(NotificationStatus.Delivered);
        await _email.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default!, default!, default, default, default, default);
    }

    // ---------------------------------------------------------------- Preference gate

    [Fact]
    public async Task A_channel_the_recipient_disabled_is_not_used()
    {
        _preferences.GetByUserIdAsync(default!, default)
            .ReturnsForAnyArgs(PreferencesWith(NotificationChannel.Email | NotificationChannel.InApp));

        var sut = CreateSut();
        var notification = MultiChannelNotification(NotificationType.BookingReminder);

        var outcome = await sut.DispatchAsync(notification);

        outcome.Skipped.Should().Be(1);
        outcome.Delivered.Should().Be(2);
        await _sms.DidNotReceiveWithAnyArgs().SendSmsAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task A_refund_notification_is_sent_even_on_a_disabled_channel()
    {
        _preferences.GetByUserIdAsync(default!, default)
            .ReturnsForAnyArgs(PreferencesWith(NotificationChannel.None));

        var sut = CreateSut();
        var notification = MultiChannelNotification(NotificationType.PaymentRefunded);

        var outcome = await sut.DispatchAsync(notification);

        outcome.Delivered.Should().Be(3, "money movement is non-suppressible");
        await _sms.ReceivedWithAnyArgs(1).SendSmsAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Preferences_are_not_consulted_when_reliable_dispatch_is_off()
    {
        _preferences.GetByUserIdAsync(default!, default)
            .ReturnsForAnyArgs(PreferencesWith(NotificationChannel.None));

        var sut = CreateSut(reliableDispatch: false);

        var outcome = await sut.DispatchAsync(MultiChannelNotification(NotificationType.BookingReminder));

        outcome.Delivered.Should().Be(3, "the rollback path restores the previous unconditional behaviour");
        await _deliveryLog.DidNotReceiveWithAnyArgs().TryClaimAsync(default, default, default!, default, default);
    }

    // ---------------------------------------------------------------- De-duplication

    [Fact]
    public async Task A_channel_already_delivered_for_this_event_is_not_sent_again()
    {
        _deliveryLog.TryClaimAsync(default, NotificationChannel.SMS, default!, default, default)
            .ReturnsForAnyArgs(call => call.ArgAt<NotificationChannel>(1) == NotificationChannel.SMS
                ? NotificationDeliveryClaim.AlreadyDelivered
                : NotificationDeliveryClaim.Claimed);

        var sut = CreateSut();

        var outcome = await sut.DispatchAsync(MultiChannelNotification());

        outcome.Skipped.Should().Be(1);
        outcome.Delivered.Should().Be(2);
        await _sms.DidNotReceiveWithAnyArgs().SendSmsAsync(default!, default!, default, default);
    }

    /// <summary>
    /// Two dispatches of the same lifecycle event — the shape a CAP redelivery or a retried handler produces.
    /// The event id, not the notification id, is the dedup scope, so the second notification must find every
    /// channel already delivered and send nothing.
    /// </summary>
    [Fact]
    public async Task Redelivering_the_same_event_notifies_at_most_once_per_channel()
    {
        var eventId = Guid.NewGuid();
        var sent = new HashSet<(Guid, NotificationChannel, string)>();
        _deliveryLog.TryClaimAsync(default, default, default!, default, default).ReturnsForAnyArgs(call =>
            sent.Add((call.ArgAt<Guid>(0), call.ArgAt<NotificationChannel>(1), call.ArgAt<string>(2)))
                ? NotificationDeliveryClaim.Claimed
                : NotificationDeliveryClaim.AlreadyDelivered);

        var sut = CreateSut();

        var first = MultiChannelNotification();
        first.SetSourceEvent(eventId);
        var firstOutcome = await sut.DispatchAsync(first);

        var second = MultiChannelNotification();
        second.SetSourceEvent(eventId);
        var secondOutcome = await sut.DispatchAsync(second);

        firstOutcome.Delivered.Should().Be(3);
        secondOutcome.Delivered.Should().Be(0);
        secondOutcome.Skipped.Should().Be(3);

        await _sms.ReceivedWithAnyArgs(1).SendSmsAsync(default!, default!, default, default);
        await _email.ReceivedWithAnyArgs(1).SendEmailAsync(default!, default!, default!, default, default, default, default);
    }

    [Fact]
    public async Task A_notification_with_no_source_event_dedups_within_its_own_id()
    {
        var sut = CreateSut();
        var notification = MultiChannelNotification();

        await sut.DispatchAsync(notification);

        await _deliveryLog.Received(3).TryClaimAsync(
            notification.Id.Value, Arg.Any<NotificationChannel>(), Arg.Any<string>(),
            notification.Id.Value, Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Failure, retry, dead-letter

    [Fact]
    public async Task A_failing_channel_marks_the_notification_failed_and_leaves_it_retryable()
    {
        _sms.SendSmsAsync(default!, default!, default, default)
            .ReturnsForAnyArgs((false, (string?)null, (string?)"gateway 503"));

        var sut = CreateSut();
        var notification = MultiChannelNotification();

        var outcome = await sut.DispatchAsync(notification);

        outcome.Failed.Should().Be(1);
        outcome.FirstError.Should().Be("gateway 503");
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.HasExhaustedRetries().Should().BeFalse();
    }

    [Fact]
    public async Task A_throwing_gateway_fails_only_its_own_channel()
    {
        _sms.SendSmsAsync(default!, default!, default, default)
            .ThrowsAsyncForAnyArgs(new HttpRequestException("connection reset"));

        var sut = CreateSut();

        var outcome = await sut.DispatchAsync(MultiChannelNotification());

        outcome.Delivered.Should().Be(2);
        outcome.Failed.Should().Be(1);
    }

    [Fact]
    public async Task A_failure_is_recorded_against_the_delivery_log_so_the_tuple_stays_retryable()
    {
        _sms.SendSmsAsync(default!, default!, default, default)
            .ReturnsForAnyArgs((false, (string?)null, (string?)"gateway 503"));

        var sut = CreateSut();

        await sut.DispatchAsync(MultiChannelNotification());

        await _deliveryLog.Received(1).RecordOutcomeAsync(
            Arg.Any<Guid>(), NotificationChannel.SMS, Arg.Any<string>(),
            false, Arg.Any<string?>(), "gateway 503", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// The scheduled-notification sweep re-reads failed rows every cycle. A notification whose backoff has not
    /// elapsed must come back untouched, or the sweep would spend the whole retry budget in a few seconds.
    /// </summary>
    [Fact]
    public async Task A_failed_notification_still_inside_its_backoff_window_is_not_re_sent()
    {
        _sms.SendSmsAsync(default!, default!, default, default)
            .ReturnsForAnyArgs((false, (string?)null, (string?)"gateway 503"));

        var sut = CreateSut();
        var notification = MultiChannelNotification();

        await sut.DispatchAsync(notification);
        var attemptsAfterFirst = notification.AttemptCount;

        var outcome = await sut.DispatchAsync(notification);

        outcome.EntirelySkipped.Should().BeTrue();
        notification.AttemptCount.Should().Be(attemptsAfterFirst);
    }

    [Fact]
    public async Task An_exhausted_notification_is_dead_lettered_instead_of_retried()
    {
        var sut = CreateSut();
        var notification = MultiChannelNotification();

        // Simulate a notification that has already burned its whole retry budget.
        notification.Send();
        notification.MarkAsFailed("gateway 503");
        ForceAttemptCount(notification, Notification.MaxRetryAttempts);

        var outcome = await sut.DispatchAsync(notification);

        notification.Status.Should().Be(NotificationStatus.DeadLettered);
        outcome.Delivered.Should().Be(0);
        await _sms.DidNotReceiveWithAnyArgs().SendSmsAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task An_already_delivered_notification_is_never_sent_again()
    {
        var sut = CreateSut();
        var notification = MultiChannelNotification();

        await sut.DispatchAsync(notification);
        _email.ClearReceivedCalls();
        _sms.ClearReceivedCalls();

        var outcome = await sut.DispatchAsync(notification);

        outcome.EntirelySkipped.Should().BeTrue();
        await _email.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default!, default!, default, default, default, default);
        await _sms.DidNotReceiveWithAnyArgs().SendSmsAsync(default!, default!, default, default);
    }

    private static void ForceAttemptCount(Notification n, int value) =>
        typeof(Notification)
            .GetField("<AttemptCount>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(n, value);
}
