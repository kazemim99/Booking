using System.Reflection;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate;
using Booksy.ServiceCatalog.Domain.Aggregates.NotificationAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Xunit;

namespace Booksy.ServiceCatalog.Domain.UnitTests.NotificationAggregate;

/// <summary>
/// C7 notification-delivery-reliability. Locks in the delivery guarantees: exponential backoff between attempts,
/// at-most-once send (dedup — no re-send once Sent), a bounded retry budget, and a terminal dead-letter state that
/// is reached only after retries are exhausted and can never be retried or sent.
/// </summary>
public class NotificationReliabilityTests
{
    private static Notification NewImmediate() =>
        Notification.CreateImmediate(
            UserId.From(Guid.NewGuid()), NotificationType.BookingConfirmation, NotificationChannel.Email,
            "subject", "body", NotificationPriority.Normal, recipientEmail: "to@test.com");

    private static void ForceAttemptCount(Notification n, int value) =>
        typeof(Notification).GetField("<AttemptCount>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(n, value);

    // ---------------------------------------------------------------- Exponential backoff

    [Theory]
    [InlineData(1, 5)]     // 5
    [InlineData(2, 15)]    // 5 * 3^1
    [InlineData(3, 45)]    // 5 * 3^2
    [InlineData(4, 135)]   // 5 * 3^3
    [InlineData(5, 405)]   // 5 * 3^4
    public void Retry_delay_grows_exponentially_per_attempt(int attemptNumber, int expectedSeconds)
    {
        var attempt = DeliveryAttempt.Create(attemptNumber, NotificationChannel.Email);
        Assert.Equal(expectedSeconds, attempt.RetryDelaySeconds);
    }

    // ---------------------------------------------------------------- Dedup / at-most-once send

    [Fact]
    public void A_sent_notification_cannot_be_sent_again()
    {
        var n = NewImmediate();
        n.Send();
        Assert.Equal(NotificationStatus.Sent, n.Status);
        Assert.Equal(1, n.AttemptCount);

        // Second send is refused — no double delivery.
        Assert.Throws<InvalidOperationException>(() => n.Send());
    }

    // ---------------------------------------------------------------- Retry eligibility

    [Fact]
    public void A_fresh_failure_is_not_immediately_retryable_and_cannot_be_forced()
    {
        var n = NewImmediate();
        n.Send();
        n.MarkAsFailed("gateway 503");

        // Backoff has not elapsed → not yet retryable; PrepareForRetry must refuse.
        Assert.False(n.ShouldRetry());
        Assert.Throws<InvalidOperationException>(() => n.PrepareForRetry());
    }

    // ---------------------------------------------------------------- Dead-letter

    [Fact]
    public void Cannot_dead_letter_before_retries_are_exhausted()
    {
        var n = NewImmediate();
        n.Send();
        n.MarkAsFailed("gateway 503"); // only 1 attempt

        Assert.False(n.HasExhaustedRetries());
        var ex = Assert.Throws<InvalidOperationException>(() => n.MarkAsDeadLettered("giving up"));
        Assert.Contains("not exhausted", ex.Message);
    }

    [Fact]
    public void After_exhausting_retries_a_failing_notification_is_dead_lettered_and_is_terminal()
    {
        var n = NewImmediate();
        n.Send();
        n.MarkAsFailed("gateway 503");
        ForceAttemptCount(n, Notification.MaxRetryAttempts); // simulate the retry budget being used up

        Assert.True(n.HasExhaustedRetries());
        Assert.False(n.ShouldRetry(), "an exhausted notification is never retried");

        n.MarkAsDeadLettered("gateway permanently unavailable");

        Assert.Equal(NotificationStatus.DeadLettered, n.Status);
        // Terminal: cannot be sent or retried out of the dead-letter queue.
        Assert.Throws<InvalidOperationException>(() => n.Send());
        Assert.Throws<InvalidOperationException>(() => n.PrepareForRetry());
    }

    [Fact]
    public void Cannot_dead_letter_a_delivered_notification()
    {
        var n = NewImmediate();
        n.Send();
        n.MarkAsDelivered();
        ForceAttemptCount(n, Notification.MaxRetryAttempts);

        var ex = Assert.Throws<InvalidOperationException>(() => n.MarkAsDeadLettered("x"));
        Assert.Contains("Cannot dead-letter", ex.Message);
    }
}
