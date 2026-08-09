namespace Booksy.Core.Application.Abstractions.CQRS
{
    /// <summary>
    /// Marks a command that MUST be processed at-most-once per idempotency key. The <c>IdempotencyBehavior</c>
    /// atomically reserves the key in the database (unique <c>(RequestType, Key)</c>) before invoking the handler, so
    /// concurrent or retried identical requests never execute the handler (or its external side effects, e.g. a
    /// payment gateway charge) more than once. Money-moving commands implement this.
    /// </summary>
    public interface IRequireIdempotency
    {
        /// <summary>The idempotency key for this request (usually the client's <c>Idempotency-Key</c> header).</summary>
        Guid? IdempotencyKey { get; }
    }
}
