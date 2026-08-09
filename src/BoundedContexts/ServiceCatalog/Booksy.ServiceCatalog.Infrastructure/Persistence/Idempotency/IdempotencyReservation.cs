namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Idempotency
{
    /// <summary>
    /// A database row reserving one idempotency key for one command type. The composite primary key
    /// <c>(RequestType, Key)</c> is the atomic serialization point: exactly one concurrent insert wins.
    /// </summary>
    public sealed class IdempotencyReservation
    {
        public string RequestType { get; set; } = default!;
        public string Key { get; set; } = default!;

        /// <summary>"InFlight" while processing; "Completed" once the handler returned and the result was stored.</summary>
        public string Status { get; set; } = "InFlight";

        public string? ResultJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public const string InFlight = "InFlight";
        public const string Completed = "Completed";
    }
}
