namespace Booksy.ServiceCatalog.Domain.Policies
{
    /// <summary>
    /// The rules governing a notification outbox row: when it may be picked up, what happens when processing
    /// it fails, and when it stops being retried.
    /// </summary>
    /// <remarks>
    /// <para>Pure decisions, deliberately separate from the row itself so they can be tested without a
    /// database — the same reason <see cref="NotificationSuppressionPolicy"/> sits here rather than inside the
    /// dispatcher.</para>
    ///
    /// <para>The attempt cap and the backoff curve are taken from the notification aggregate
    /// (<c>Notification.MaxRetryAttempts</c> and <c>DeliveryAttempt</c>) rather than chosen afresh. A row that
    /// fails five times and a send that fails five times are the same kind of giving up, and two different
    /// answers to "how long until we try again" would be a bug waiting to be found in production.</para>
    /// </remarks>
    public static class NotificationOutboxPolicy
    {
        /// <summary>Attempts before a row is abandoned. Matches <c>Notification.MaxRetryAttempts</c>.</summary>
        public const int MaxAttempts = 5;

        /// <summary>
        /// How long a sweep holds a row before others may take it.
        /// </summary>
        /// <remarks>
        /// Long enough that an ordinary send — including a slow gateway — finishes inside it, short enough
        /// that a sweep killed mid-row does not strand that notification for long. A lease rather than a
        /// crash-detection mechanism: nobody has to notice the death for the row to come back.
        /// </remarks>
        public static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

        /// <summary>A row with no schedule is due immediately; a scheduled one is due once its time passes.</summary>
        public static bool IsDue(DateTime? scheduledFor, DateTime utcNow) =>
            scheduledFor is null || scheduledFor.Value <= utcNow;

        /// <summary>
        /// Whether a sweep may take this row: either it is pending and due, or a previous claim's lease has
        /// run out and the row is up for grabs again.
        /// </summary>
        public static bool IsClaimable(string state, DateTime? claimedUntil, DateTime? scheduledFor, DateTime utcNow)
        {
            if (!IsDue(scheduledFor, utcNow))
                return false;

            return state switch
            {
                NotificationOutboxState.Pending => true,
                NotificationOutboxState.Claimed => claimedUntil is not null && claimedUntil.Value <= utcNow,
                _ => false,
            };
        }

        public static bool HasExhaustedAttempts(int attemptCount) => attemptCount >= MaxAttempts;

        /// <summary>
        /// Delay before attempt <paramref name="attemptNumber"/> is retried: 5s, 15s, 45s, 135s, 405s.
        /// </summary>
        public static TimeSpan BackoffFor(int attemptNumber)
        {
            if (attemptNumber < 1)
                attemptNumber = 1;

            return TimeSpan.FromSeconds(5 * Math.Pow(3, attemptNumber - 1));
        }

        /// <summary>
        /// What a failed attempt turns the row into: pending again after a backoff, or dead-lettered once the
        /// budget is spent.
        /// </summary>
        /// <param name="attemptCountAfterFailure">The row's attempt count including the failure just seen.</param>
        public static OutboxFailureOutcome OnFailure(int attemptCountAfterFailure, DateTime utcNow)
        {
            if (HasExhaustedAttempts(attemptCountAfterFailure))
                return new OutboxFailureOutcome(NotificationOutboxState.DeadLettered, RetryAt: null);

            return new OutboxFailureOutcome(
                NotificationOutboxState.Pending,
                RetryAt: utcNow + BackoffFor(attemptCountAfterFailure));
        }
    }

    /// <summary>Where a failed row goes next.</summary>
    /// <param name="State">The state the row takes.</param>
    /// <param name="RetryAt">When it becomes due again; null once dead-lettered.</param>
    public sealed record OutboxFailureOutcome(string State, DateTime? RetryAt);

    /// <summary>
    /// The outbox row states, named in one place so the policy and the persistence row cannot drift apart.
    /// </summary>
    public static class NotificationOutboxState
    {
        public const string Pending = "Pending";
        public const string Claimed = "Claimed";
        public const string Processed = "Processed";
        public const string DeadLettered = "DeadLettered";
        public const string Cancelled = "Cancelled";
    }
}
