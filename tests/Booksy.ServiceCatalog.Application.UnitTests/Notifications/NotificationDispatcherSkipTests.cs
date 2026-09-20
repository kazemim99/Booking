using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Notifications;

/// <summary>
/// The difference between "could not be delivered" and "there was nothing to deliver to".
/// </summary>
/// <remarks>
/// <para>A recipient with no registered device, or an environment with no push credentials, will not improve
/// by being retried. If the dispatcher counted those as failures they would consume the notification's retry
/// budget and eventually dead-letter it — while its SMS and in-app copies had gone out perfectly well.</para>
///
/// <para>Tested here rather than in the integration suite because the integration fakes always succeed, so
/// the skip path is unreachable there.</para>
/// </remarks>
public class NotificationDispatcherSkipTests
{
    private readonly IEmailNotificationService _email = Substitute.For<IEmailNotificationService>();
    private readonly ISmsNotificationService _sms = Substitute.For<ISmsNotificationService>();
    private readonly IPushNotificationService _push = Substitute.For<IPushNotificationService>();
    private readonly IInAppNotificationService _inApp = Substitute.For<IInAppNotificationService>();
    private readonly IUserNotificationPreferencesRepository _preferences =
        Substitute.For<IUserNotificationPreferencesRepository>();
    private readonly INotificationDeliveryLog _deliveryLog = Substitute.For<INotificationDeliveryLog>();

    private readonly NotificationDispatcher _dispatcher;

    public NotificationDispatcherSkipTests()
    {
        // No stored preferences: absent preferences must never read as "everything disabled".
        _preferences.GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Aggregates.UserNotificationPreferencesAggregate.UserNotificationPreferences?)null);

        _deliveryLog.TryClaimAsync(
                Arg.Any<Guid>(), Arg.Any<NotificationChannel>(), Arg.Any<string>(), Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(NotificationDeliveryClaim.Claimed);

        _dispatcher = new NotificationDispatcher(
            _email, _sms, _push, _inApp, _preferences, _deliveryLog,
            new NotificationDispatchOptions { ReliableDispatch = true },
            NullLogger<NotificationDispatcher>.Instance);
    }

    [Fact]
    public async Task A_recipient_with_no_device_is_skipped_not_failed()
    {
        GivenPushReturns(PushUnavailable.NoDevice);
        var notification = PushOnlyNotification();

        var outcome = await _dispatcher.DispatchAsync(notification);

        outcome.Failed.Should().Be(0, "there is nothing to retry");
        outcome.Skipped.Should().Be(1);
        notification.Status.Should().NotBe(NotificationStatus.Failed);
    }

    [Fact]
    public async Task An_environment_without_push_configured_is_skipped_not_failed()
    {
        GivenPushReturns(PushUnavailable.NotConfigured);

        var outcome = await _dispatcher.DispatchAsync(PushOnlyNotification());

        outcome.Failed.Should().Be(0);
        outcome.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task A_genuine_push_failure_is_still_a_failure()
    {
        // The distinction only matters if real failures keep burning the retry budget as they should.
        GivenPushReturns("the gateway timed out");
        var notification = PushOnlyNotification();

        var outcome = await _dispatcher.DispatchAsync(notification);

        outcome.Failed.Should().Be(1);
        outcome.FirstError.Should().Be("the gateway timed out");
    }

    [Fact]
    public async Task A_skipped_push_does_not_consume_an_attempt_budget_on_the_other_channels()
    {
        // The case that would have bitten: push is unreachable, SMS is fine, and the notification must not
        // end up marked failed because of the channel that had nobody to deliver to.
        GivenPushReturns(PushUnavailable.NoDevice);
        _sms.SendSmsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns((true, "sms-1", (string?)null));

        var notification = Notification.Create(
            UserId.From(Guid.NewGuid()),
            NotificationType.BookingConfirmation,
            NotificationChannel.PushNotification | NotificationChannel.SMS,
            "نوبت شما تأیید شد",
            "متن",
            NotificationPriority.High);
        notification.SetRecipientContact(null, "+989120000000", "مریم");
        notification.Queue();

        var outcome = await _dispatcher.DispatchAsync(notification);

        outcome.Delivered.Should().Be(1, "the SMS went out");
        outcome.Skipped.Should().Be(1, "push had nobody to deliver to");
        outcome.Failed.Should().Be(0);
        notification.Status.Should().NotBe(NotificationStatus.Failed);
    }

    private void GivenPushReturns(string error) =>
        _push.SendPushAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns((false, (string?)null, (string?)error));

    private static Notification PushOnlyNotification()
    {
        var notification = Notification.Create(
            UserId.From(Guid.NewGuid()),
            NotificationType.BookingReminder,
            NotificationChannel.PushNotification,
            "یادآوری نوبت",
            "متن",
            NotificationPriority.Normal);

        notification.Queue();
        return notification;
    }
}
