using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// What the delivery log says happened, and whether it is true.
/// </summary>
/// <remarks>
/// <para>This table is the system's answer to "was this person actually told?", and the whole push rebuild
/// (task 4.3) happened because the old stub made it lie — it reported Delivered for messages that were never
/// sent. A log that records outcomes wrongly is worse than no log, because it is believed.</para>
///
/// <para>Tested against the real thing rather than a substitute: the recording is a raw <c>UPDATE</c> whose
/// null handling has already caused one silent failure (see the comment in <c>RecordOutcomeAsync</c> about
/// <c>DBNull</c> boxing, which would have left every tuple on Pending and disabled de-duplication without a
/// single error surfacing). Only a real database can catch that class of bug.</para>
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class NotificationDeliveryLogTests : ServiceCatalogIntegrationTestBase
{
    private const NotificationChannel Channel = NotificationChannel.SMS;

    public NotificationDeliveryLogTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task An_accepted_send_is_recorded_as_delivered()
    {
        var (eventId, recipient) = NewAttempt();

        await ClaimAsync(eventId, recipient);
        await RecordAsync(eventId, recipient, success: true, gatewayMessageId: "gw-123", error: null);

        var row = await ReadAsync(eventId, recipient);
        row!.Status.Should().Be(NotificationDelivery.Delivered);
        row.GatewayMessageId.Should().Be("gw-123");
        row.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task A_rejected_send_is_recorded_as_failed_with_the_reason()
    {
        var (eventId, recipient) = NewAttempt();

        await ClaimAsync(eventId, recipient);
        await RecordAsync(eventId, recipient, success: false, gatewayMessageId: null, error: "gateway refused");

        var row = await ReadAsync(eventId, recipient);
        row!.Status.Should().Be(NotificationDelivery.Failed);
        row.ErrorMessage.Should().Be("gateway refused");
    }

    [Fact]
    public async Task A_rejected_send_can_be_tried_again()
    {
        // The distinction that matters: Failed means "not yet delivered", not "done with". Treating a
        // recorded failure as final would turn one transient gateway error into a message nobody receives.
        var (eventId, recipient) = NewAttempt();

        await ClaimAsync(eventId, recipient);
        await RecordAsync(eventId, recipient, success: false, gatewayMessageId: null, error: "timeout");

        (await ClaimAsync(eventId, recipient)).Should().Be(NotificationDeliveryClaim.Claimed);
    }

    [Fact]
    public async Task An_accepted_send_is_never_sent_twice()
    {
        var (eventId, recipient) = NewAttempt();

        await ClaimAsync(eventId, recipient);
        await RecordAsync(eventId, recipient, success: true, gatewayMessageId: "gw-1", error: null);

        (await ClaimAsync(eventId, recipient)).Should().Be(
            NotificationDeliveryClaim.AlreadyDelivered,
            "this is the de-duplication gate — a swept-twice outbox row must not send twice");
    }

    [Fact]
    public async Task Recording_one_attempt_does_not_touch_another()
    {
        var (eventId, mine) = NewAttempt();
        var theirs = NewRecipient();

        await ClaimAsync(eventId, mine);
        await ClaimAsync(eventId, theirs);
        await RecordAsync(eventId, mine, success: false, gatewayMessageId: null, error: "refused");

        (await ReadAsync(eventId, theirs))!.Status.Should().Be(
            NotificationDelivery.Pending,
            "one recipient's failure says nothing about another's");
    }

    // ── arrange ──

    private static (Guid EventId, string Recipient) NewAttempt() => (Guid.NewGuid(), NewRecipient());

    private static string NewRecipient() => $"+98912{Guid.NewGuid().ToString("N")[..7]}";

    private async Task<NotificationDeliveryClaim> ClaimAsync(Guid eventId, string recipient)
    {
        using var scope = Factory.Services.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<INotificationDeliveryLog>();
        return await log.TryClaimAsync(eventId, Channel, recipient, Guid.NewGuid());
    }

    private async Task RecordAsync(
        Guid eventId, string recipient, bool success, string? gatewayMessageId, string? error)
    {
        using var scope = Factory.Services.CreateScope();
        var log = scope.ServiceProvider.GetRequiredService<INotificationDeliveryLog>();
        await log.RecordOutcomeAsync(eventId, Channel, recipient, success, gatewayMessageId, error);
    }

    private async Task<NotificationDelivery?> ReadAsync(Guid eventId, string recipient)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var channelName = Channel.ToString();

        return await db.NotificationDeliveries.AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.EventId == eventId && d.Channel == channelName && d.Recipient == recipient);
    }
}
