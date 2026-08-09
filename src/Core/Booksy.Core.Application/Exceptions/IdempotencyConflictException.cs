namespace Booksy.Core.Application.Exceptions
{
    /// <summary>
    /// Thrown when a request duplicates one that is still being processed (a concurrent in-flight reservation for the
    /// same idempotency key). Mapped to HTTP 409 Conflict — the client should retry, at which point the original
    /// request's stored result is returned.
    /// </summary>
    public sealed class IdempotencyConflictException : Exception
    {
        public IdempotencyConflictException(string requestType, string key)
            : base($"A request of type '{requestType}' with idempotency key '{key}' is already in progress. Retry shortly.")
        {
            RequestType = requestType;
            Key = key;
        }

        public string RequestType { get; }
        public string Key { get; }
    }
}
