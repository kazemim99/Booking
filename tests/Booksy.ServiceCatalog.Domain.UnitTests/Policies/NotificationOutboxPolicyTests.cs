using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Policies;

namespace Booksy.ServiceCatalog.Domain.UnitTests.Policies;

/// <summary>
/// The outbox state machine: when a row may be picked up, and where a failed one goes.
/// </summary>
/// <remarks>
/// These are the rules that decide whether a notification is sent once, sent late, or given up on. The
/// atomicity of a claim is a database property and is proved against a real Postgres in the integration
/// suite; what is pinned here is the decision, which must hold regardless of how it is executed.
/// </remarks>
public class NotificationOutboxPolicyTests
{
    private static readonly DateTime Now = new(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

    // ── Due-ness ──

    [Fact]
    public void An_unscheduled_row_is_due_immediately()
    {
        Assert.True(NotificationOutboxPolicy.IsDue(scheduledFor: null, Now));
    }

    [Fact]
    public void A_row_scheduled_in_the_future_is_not_due()
    {
        Assert.False(NotificationOutboxPolicy.IsDue(Now.AddMinutes(1), Now));
    }

    [Fact]
    public void A_row_is_due_the_moment_its_time_arrives()
    {
        // Boundary: a reminder scheduled for exactly now must fire on this sweep, not the next one.
        Assert.True(NotificationOutboxPolicy.IsDue(Now, Now));
    }

    // ── Claiming ──

    [Fact]
    public void A_pending_due_row_can_be_claimed()
    {
        Assert.True(NotificationOutboxPolicy.IsClaimable(
            NotificationOutboxState.Pending, claimedUntil: null, scheduledFor: null, Now));
    }

    [Fact]
    public void A_pending_row_that_is_not_yet_due_cannot_be_claimed()
    {
        Assert.False(NotificationOutboxPolicy.IsClaimable(
            NotificationOutboxState.Pending, claimedUntil: null, Now.AddHours(2), Now));
    }

    [Fact]
    public void A_row_held_by_a_live_claim_is_invisible_to_another_sweep()
    {
        // The whole point of the lease: a second sweep running while the first still holds the row must not
        // take it, or the recipient gets the same notification twice.
        Assert.False(NotificationOutboxPolicy.IsClaimable(
            NotificationOutboxState.Claimed, Now.AddMinutes(3), scheduledFor: null, Now));
    }

    [Fact]
    public void A_row_whose_claim_lease_expired_is_available_again()
    {
        // A sweep that died mid-row leaves this behind. Nobody detects the death; the lease just runs out.
        Assert.True(NotificationOutboxPolicy.IsClaimable(
            NotificationOutboxState.Claimed, Now.AddSeconds(-1), scheduledFor: null, Now));
    }

    [Theory]
    [InlineData(NotificationOutboxState.Processed)]
    [InlineData(NotificationOutboxState.DeadLettered)]
    [InlineData(NotificationOutboxState.Cancelled)]
    public void A_terminal_row_is_never_claimed_again(string terminalState)
    {
        Assert.False(NotificationOutboxPolicy.IsClaimable(
            terminalState, claimedUntil: null, scheduledFor: null, Now));
    }

    // ── Failure and giving up ──

    [Fact]
    public void A_failed_row_returns_to_pending_with_a_backoff()
    {
        var outcome = NotificationOutboxPolicy.OnFailure(attemptCountAfterFailure: 1, Now);

        Assert.Equal(NotificationOutboxState.Pending, outcome.State);
        Assert.Equal(Now.AddSeconds(5), outcome.RetryAt);
    }

    [Fact]
    public void Each_successive_failure_waits_longer()
    {
        var delays = Enumerable.Range(1, 4)
            .Select(n => NotificationOutboxPolicy.OnFailure(n, Now).RetryAt!.Value - Now)
            .ToList();

        Assert.Equal(
            new[]
            {
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(45),
                TimeSpan.FromSeconds(135),
            },
            delays);
    }

    [Fact]
    public void A_row_that_exhausts_its_attempts_is_dead_lettered_not_retried()
    {
        var outcome = NotificationOutboxPolicy.OnFailure(
            attemptCountAfterFailure: NotificationOutboxPolicy.MaxAttempts,
            Now);

        Assert.Equal(NotificationOutboxState.DeadLettered, outcome.State);
        Assert.Null(outcome.RetryAt);
    }

    [Fact]
    public void The_last_attempt_within_budget_still_retries()
    {
        // Boundary, in the direction that matters: giving up one attempt early silently loses a notification.
        var outcome = NotificationOutboxPolicy.OnFailure(
            attemptCountAfterFailure: NotificationOutboxPolicy.MaxAttempts - 1,
            Now);

        Assert.Equal(NotificationOutboxState.Pending, outcome.State);
        Assert.NotNull(outcome.RetryAt);
    }

    [Fact]
    public void Attempt_budget_matches_the_notification_aggregate()
    {
        // Two different answers to "how many tries" would mean an outbox row and the send it produces give up
        // at different points, which is the kind of thing only production finds.
        Assert.Equal(Notification.MaxRetryAttempts, NotificationOutboxPolicy.MaxAttempts);
    }

    [Fact]
    public void Backoff_never_goes_backwards_for_a_nonsensical_attempt_number()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), NotificationOutboxPolicy.BackoffFor(0));
        Assert.Equal(TimeSpan.FromSeconds(5), NotificationOutboxPolicy.BackoffFor(-3));
    }
}
