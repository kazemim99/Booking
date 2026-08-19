using Booksy.Core.Application.Services.Notifications;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.UserNotificationPreferencesAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Notifications;

/// <summary>
/// C7 notification-delivery-reliability, against a real PostgreSQL.
///
/// <para>De-duplication is the one piece that cannot be proven with mocks: the guarantee lives in the
/// <c>NotificationDeliveries</c> composite primary key, so what matters is how the real table behaves when the
/// same (event, channel, recipient) tuple is claimed twice, and whether a failed tuple stays claimable. These
/// tests drive the real delivery log and the real dispatcher, substituting only the SMS
/// gateway so a send can be made to fail on demand.</para>
/// </summary>
public class NotificationDeliveryReliabilityTests : ServiceCatalogIntegrationTestBase
{
    public NotificationDeliveryReliabilityTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    /// <summary>An SMS gateway whose next outcome the test controls, recording every send it is asked to make.</summary>
    private sealed class ScriptedSmsGateway : ISmsNotificationService
    {
        public List<string> Sent { get; } = new();
        public bool NextSendFails { get; set; }

        public Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
            string phoneNumber, string message,
            Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            if (NextSendFails)
            {
                NextSendFails = false;
                return Task.FromResult<(bool, string?, string?)>((false, null, "gateway 503"));
            }

            Sent.Add(phoneNumber);
            return Task.FromResult<(bool, string?, string?)>((true, $"sms-{Sent.Count}", null));
        }

        public Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
            List<string> phoneNumbers, string message,
            Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Builds a dispatcher wired to the real delivery log and preferences repository, with a scripted SMS
    /// gateway in place of the configured one.
    /// </summary>
    private NotificationDispatcher CreateDispatcher(ScriptedSmsGateway sms) =>
        new(Scope.ServiceProvider.GetRequiredService<IEmailNotificationService>(),
            sms,
            Scope.ServiceProvider.GetRequiredService<IPushNotificationService>(),
            Scope.ServiceProvider.GetRequiredService<IInAppNotificationService>(),
            Scope.ServiceProvider.GetRequiredService<IUserNotificationPreferencesRepository>(),
            Scope.ServiceProvider.GetRequiredService<INotificationDeliveryLog>(),
            new NotificationDispatchOptions { ReliableDispatch = true },
            NullLogger<NotificationDispatcher>.Instance);

    private static Notification SmsNotification(Guid recipientId, string phone, Guid sourceEventId)
    {
        var notification = Notification.CreateImmediate(
            UserId.From(recipientId),
            NotificationType.BookingConfirmation,
            NotificationChannel.SMS,
            "Booking confirmed",
            "Your booking is confirmed",
            NotificationPriority.High,
            plainTextBody: "Your booking is confirmed",
            recipientPhone: phone);

        notification.SetSourceEvent(sourceEventId);
        return notification;
    }

    /// <summary>
    /// The shape a redelivered lifecycle event produces: the same event handled twice, each time building its own
    /// notification. The customer must hear about it once.
    /// </summary>
    [Fact]
    public async Task RedeliveredEvent_SendsOnlyOneSmsPerRecipient()
    {
        var sms = new ScriptedSmsGateway();
        var dispatcher = CreateDispatcher(sms);

        var eventId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        const string phone = "+989120000001";

        var first = await dispatcher.DispatchAsync(SmsNotification(recipientId, phone, eventId));
        var second = await dispatcher.DispatchAsync(SmsNotification(recipientId, phone, eventId));

        first.Delivered.Should().Be(1);
        second.Delivered.Should().Be(0);
        second.Skipped.Should().Be(1);
        sms.Sent.Should().ContainSingle();

        // One durable row per (event, channel, recipient), closed as Delivered.
        var deliveries = await DbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(d => d.EventId == eventId)
            .ToListAsync();

        deliveries.Should().ContainSingle();
        deliveries[0].Channel.Should().Be(nameof(NotificationChannel.SMS));
        deliveries[0].Recipient.Should().Be(phone);
        deliveries[0].Status.Should().Be("Delivered");
    }

    /// <summary>
    /// A transient gateway failure must leave the tuple claimable, otherwise de-duplication would convert one
    /// 503 into a notification the customer never receives.
    /// </summary>
    [Fact]
    public async Task TransientFailure_IsRetriedAndThenDelivered()
    {
        var sms = new ScriptedSmsGateway { NextSendFails = true };
        var dispatcher = CreateDispatcher(sms);

        var eventId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        const string phone = "+989120000002";

        var failed = await dispatcher.DispatchAsync(SmsNotification(recipientId, phone, eventId));

        failed.Failed.Should().Be(1);
        sms.Sent.Should().BeEmpty();

        var afterFailure = await DbContext.NotificationDeliveries
            .AsNoTracking()
            .SingleAsync(d => d.EventId == eventId);
        afterFailure.Status.Should().Be("Failed");

        // The retry — the same event dispatched again once the gateway recovered.
        var retried = await dispatcher.DispatchAsync(SmsNotification(recipientId, phone, eventId));

        retried.Delivered.Should().Be(1);
        sms.Sent.Should().ContainSingle();

        DbContext.ChangeTracker.Clear();
        var afterRetry = await DbContext.NotificationDeliveries
            .AsNoTracking()
            .SingleAsync(d => d.EventId == eventId);
        afterRetry.Status.Should().Be("Delivered");
        afterRetry.AttemptCount.Should().Be(2, "the second claim recorded another attempt against the same tuple");
    }

    /// <summary>
    /// Two different recipients of the same event are different tuples — de-duplication must not collapse the
    /// customer's notification and the provider's into one.
    /// </summary>
    [Fact]
    public async Task SameEvent_DifferentRecipients_AreNotDeduplicated()
    {
        var sms = new ScriptedSmsGateway();
        var dispatcher = CreateDispatcher(sms);

        var eventId = Guid.NewGuid();

        await dispatcher.DispatchAsync(SmsNotification(Guid.NewGuid(), "+989120000003", eventId));
        await dispatcher.DispatchAsync(SmsNotification(Guid.NewGuid(), "+989120000004", eventId));

        sms.Sent.Should().HaveCount(2);

        var deliveries = await DbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(d => d.EventId == eventId)
            .ToListAsync();

        deliveries.Should().HaveCount(2);
    }

    /// <summary>
    /// The preference gate at its most consequential: a customer who turned SMS off gets no marketing-grade SMS,
    /// but still gets the refund receipt.
    /// </summary>
    [Fact]
    public async Task DisabledChannel_IsSkipped_ExceptForNonSuppressibleNotifications()
    {
        var sms = new ScriptedSmsGateway();
        var dispatcher = CreateDispatcher(sms);

        var recipientId = Guid.NewGuid();
        var preferencesRepository = Scope.ServiceProvider.GetRequiredService<IUserNotificationPreferencesRepository>();

        var preferences = UserNotificationPreferences.CreateDefault(UserId.From(recipientId));
        preferences.DisableChannels(NotificationChannel.SMS);
        await preferencesRepository.SaveAsync(preferences);
        // Repositories only stage changes here; the unit of work commits. There is no request pipeline around
        // this test, so commit explicitly or the dispatcher's preference lookup would miss the row.
        await DbContext.SaveChangesAsync();

        var reminder = Notification.CreateImmediate(
            UserId.From(recipientId), NotificationType.BookingReminder, NotificationChannel.SMS,
            "Reminder", "Tomorrow at 10", NotificationPriority.Normal,
            recipientPhone: "+989120000005");
        reminder.SetSourceEvent(Guid.NewGuid());

        var refund = Notification.CreateImmediate(
            UserId.From(recipientId), NotificationType.PaymentRefunded, NotificationChannel.SMS,
            "Refund issued", "Your refund is on its way", NotificationPriority.High,
            recipientPhone: "+989120000005");
        refund.SetSourceEvent(Guid.NewGuid());

        var reminderOutcome = await dispatcher.DispatchAsync(reminder);
        var refundOutcome = await dispatcher.DispatchAsync(refund);

        reminderOutcome.Skipped.Should().Be(1);
        reminderOutcome.Delivered.Should().Be(0);
        refundOutcome.Delivered.Should().Be(1, "money movement ignores channel preferences");
        sms.Sent.Should().ContainSingle();
    }
}
