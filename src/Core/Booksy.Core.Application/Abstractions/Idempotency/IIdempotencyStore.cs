namespace Booksy.Core.Application.Abstractions.Idempotency
{
    public enum IdempotencyState
    {
        /// <summary>No reservation existed — the caller successfully reserved it and must process the request.</summary>
        Reserved = 0,
        /// <summary>A reservation exists and is still being processed (a concurrent duplicate is in flight).</summary>
        InFlight = 1,
        /// <summary>A reservation exists and completed — the stored result should be returned.</summary>
        Completed = 2
    }

    /// <summary>The outcome of attempting to reserve an idempotency key.</summary>
    public sealed record IdempotencyOutcome(IdempotencyState State, string? ResultJson);

    /// <summary>
    /// Atomic, database-backed idempotency reservations. The reserve is an insert against a unique
    /// <c>(RequestType, Key)</c> constraint, so exactly one concurrent caller wins the reservation; the rest observe
    /// <see cref="IdempotencyState.InFlight"/> (or <see cref="IdempotencyState.Completed"/> once the winner finishes).
    /// A stale in-flight reservation (a crashed processor) is reclaimable so a key never gets stuck forever.
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>Atomically reserve the key, reclaiming a stale in-flight reservation older than <paramref name="staleAfter"/>.</summary>
        Task<IdempotencyOutcome> TryReserveAsync(string requestType, string key, TimeSpan staleAfter, CancellationToken cancellationToken = default);

        /// <summary>Mark a reservation completed and store the serialized result (returned to future duplicates).</summary>
        Task CompleteAsync(string requestType, string key, string resultJson, CancellationToken cancellationToken = default);

        /// <summary>Release a reservation (delete it) so a failed request can be retried.</summary>
        Task ReleaseAsync(string requestType, string key, CancellationToken cancellationToken = default);
    }
}
