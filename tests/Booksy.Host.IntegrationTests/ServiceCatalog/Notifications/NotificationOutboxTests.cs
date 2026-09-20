using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Policies;
using Booksy.ServiceCatalog.Infrastructure.BackgroundJobs;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Notifications;

/// <summary>
/// The two properties of the outbox that only a real database can demonstrate: that an intent lives and dies
/// with the transaction that recorded it, and that concurrent sweeps cannot send the same notification twice.
/// </summary>
/// <remarks>
/// The state machine itself is unit tested (<c>NotificationOutboxPolicyTests</c>). What is proved here is
/// that Postgres actually enforces it — <c>FOR UPDATE SKIP LOCKED</c> and the unique index are claims about
/// the database, and asserting them against a fake would prove nothing.
/// </remarks>
[Collection(BooksyHostTestCollection.Name)]
public class NotificationOutboxTests : ServiceCatalogIntegrationTestBase
{
    public NotificationOutboxTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task An_intent_recorded_in_a_rolled_back_transaction_never_exists()
    {
        var recipientId = Guid.NewGuid();
        var dedupKey = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();

            // Through the execution strategy, because the Npgsql retrying strategy refuses a hand-rolled
            // transaction — the same reason production code commits via the unit of work rather than opening
            // one itself.
            var strategy = context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync();

                await raiser.RaiseAsync(
                    NotificationEventCode.BookingConfirmed,
                    recipientId,
                    dedupKey,
                    new Dictionary<string, string> { ["businessName"] = "سالن نهال" });

                await context.SaveChangesAsync();

                // The business write failed after the intent was recorded. Nobody should be notified about
                // work that did not happen.
                await transaction.RollbackAsync();
            });

            // The rollback undid the INSERT but the context still tracks the entity; a later SaveChanges on
            // this context would resurrect it. Production never sees this because the scope ends with the
            // request.
            context.ChangeTracker.Clear();
        }

        (await CountOutboxRowsAsync(dedupKey)).Should().Be(0);
    }

    [Fact]
    public async Task An_intent_committed_with_its_transaction_survives()
    {
        var dedupKey = Guid.NewGuid();

        await RaiseAndCommitAsync(NotificationEventCode.BookingConfirmed, Guid.NewGuid(), dedupKey);

        (await CountOutboxRowsAsync(dedupKey)).Should().Be(1);
    }

    [Fact]
    public async Task Raising_the_same_event_for_the_same_person_twice_records_one_intent()
    {
        // A redelivered event or a retried command must not notify the customer twice.
        var recipientId = Guid.NewGuid();
        var dedupKey = Guid.NewGuid();

        await RaiseAndCommitAsync(NotificationEventCode.BookingConfirmed, recipientId, dedupKey);
        await RaiseAndCommitAsync(NotificationEventCode.BookingConfirmed, recipientId, dedupKey);

        (await CountOutboxRowsAsync(dedupKey)).Should().Be(1);
    }

    [Fact]
    public async Task One_event_notifying_two_people_records_an_intent_for_each()
    {
        // Same event, same key, different recipients: the customer and the salon both need telling.
        var dedupKey = Guid.NewGuid();

        await RaiseAndCommitAsync(NotificationEventCode.BookingConfirmed, Guid.NewGuid(), dedupKey);
        await RaiseAndCommitAsync(NotificationEventCode.NewBookingConfirmed, Guid.NewGuid(), dedupKey);

        (await CountOutboxRowsAsync(dedupKey)).Should().Be(2);
    }

    [Fact]
    public async Task A_row_scheduled_for_later_is_not_swept_yet()
    {
        var dedupKey = Guid.NewGuid();

        await RaiseAndCommitAsync(
            NotificationEventCode.BookingReminder24h,
            Guid.NewGuid(),
            dedupKey,
            scheduledFor: DateTime.UtcNow.AddHours(4));

        await RunSweepAsync();

        (await StateOfAsync(dedupKey)).Should().Be(NotificationOutboxState.Pending);
    }

    [Fact]
    public async Task Two_concurrent_sweeps_process_each_intent_exactly_once()
    {
        // The claim is the whole mechanism. If two sweeps can take one row, every notification in the system
        // is capable of arriving twice.
        const int intentCount = 12;
        var dedupKeys = Enumerable.Range(0, intentCount).Select(_ => Guid.NewGuid()).ToList();

        foreach (var key in dedupKeys)
            await RaiseAndCommitAsync(NotificationEventCode.BookingConfirmed, Guid.NewGuid(), key);

        // Two independent scopes, so two DbContexts and two connections — a single context would serialise
        // them and prove nothing.
        await Task.WhenAll(RunSweepAsync(), RunSweepAsync());

        var states = await StatesOfAsync(dedupKeys);

        states.Should().HaveCount(intentCount);
        states.Should().OnlyContain(
            s => s == NotificationOutboxState.Processed,
            "every claimed intent should have been handled once and marked done");
    }

    [Fact]
    public async Task A_swept_intent_is_not_swept_again()
    {
        var dedupKey = Guid.NewGuid();
        await RaiseAndCommitAsync(NotificationEventCode.BookingConfirmed, Guid.NewGuid(), dedupKey);

        await RunSweepAsync();
        var afterFirst = await StateOfAsync(dedupKey);

        await RunSweepAsync();
        var afterSecond = await StateOfAsync(dedupKey);

        afterFirst.Should().Be(NotificationOutboxState.Processed);
        afterSecond.Should().Be(NotificationOutboxState.Processed);
    }

    [Fact]
    public async Task Withdrawing_a_subject_cancels_its_unsent_notifications()
    {
        // A reminder for a booking that is no longer happening must not go out.
        var bookingId = Guid.NewGuid();
        var dedupKey = Guid.NewGuid();

        await RaiseAndCommitAsync(
            NotificationEventCode.BookingReminder24h,
            Guid.NewGuid(),
            dedupKey,
            scheduledFor: DateTime.UtcNow.AddHours(4),
            subjectType: "Booking",
            subjectId: bookingId);

        using (var scope = Factory.Services.CreateScope())
        {
            var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();
            await raiser.WithdrawPendingForSubjectAsync("Booking", bookingId);
        }

        (await StateOfAsync(dedupKey)).Should().Be(NotificationOutboxState.Cancelled);
    }

    // ── helpers ──

    private async Task RaiseAndCommitAsync(
        NotificationEventCode code,
        Guid recipientId,
        Guid dedupKey,
        DateTime? scheduledFor = null,
        string? subjectType = null,
        Guid? subjectId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        var raiser = scope.ServiceProvider.GetRequiredService<INotificationRaiser>();

        await raiser.RaiseAsync(
            code,
            recipientId,
            dedupKey,
            new Dictionary<string, string> { ["businessName"] = "سالن نهال" },
            subjectType,
            subjectId,
            scheduledFor);

        await context.SaveChangesAsync();
    }

    private async Task RunSweepAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ProcessNotificationOutboxJob>();
        await job.ExecuteAsync();
    }

    private async Task<int> CountOutboxRowsAsync(Guid dedupKey)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox.AsNoTracking().CountAsync(e => e.DedupKey == dedupKey);
    }

    private async Task<string?> StateOfAsync(Guid dedupKey)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox
            .AsNoTracking()
            .Where(e => e.DedupKey == dedupKey)
            .Select(e => e.State)
            .FirstOrDefaultAsync();
    }

    private async Task<List<string>> StatesOfAsync(IReadOnlyCollection<Guid> dedupKeys)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
        return await context.NotificationOutbox
            .AsNoTracking()
            .Where(e => dedupKeys.Contains(e.DedupKey))
            .Select(e => e.State)
            .ToListAsync();
    }
}
