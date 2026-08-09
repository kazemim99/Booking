using System.Security.Cryptography;
using System.Text;

namespace Booksy.ServiceCatalog.Domain.Aggregates.LedgerAggregate
{
    /// <summary>
    /// Produces <b>stable</b>, deterministic ledger event ids from the business identity of a money movement, so
    /// posting is idempotent across replays and redeliveries: the same movement always maps to the same id, and a
    /// redelivered domain event never double-posts (the ledger's unique <c>(EventId, Account)</c> constraint is the
    /// hard backstop). A raw domain-event <c>Id</c> is NOT used because re-raising an event mints a new one.
    /// </summary>
    public static class LedgerEventKeys
    {
        /// <summary>The single charge posting for a payment (a payment is charged/verified once).</summary>
        public static Guid Charge(Guid paymentId) => Deterministic($"charge:{paymentId:N}");

        /// <summary>A refund posting, keyed by payment + refund instant so distinct partial refunds are distinct but
        /// a redelivery of the same refund event is idempotent.</summary>
        public static Guid Refund(Guid paymentId, DateTime refundedAtUtc) => Deterministic($"refund:{paymentId:N}:{refundedAtUtc.Ticks}");

        /// <summary>A payout posting for a payout id.</summary>
        public static Guid Payout(Guid payoutId) => Deterministic($"payout:{payoutId:N}");

        /// <summary>Name-based deterministic GUID (RFC-4122 style, SHA-1) — same input always yields the same GUID.</summary>
        public static Guid Deterministic(string name)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(name));
            var bytes = new byte[16];
            Array.Copy(hash, bytes, 16);
            // set version (5) and variant bits so it's a well-formed GUID
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
            return new Guid(bytes);
        }
    }
}
