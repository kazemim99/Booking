namespace Booksy.Core.Domain.ValueObjects
{
    /// <summary>
    /// The clock a salon reads. A booking's time is a wall-clock value — «۱۰:۰۰» is ten o'clock at the salon, in no
    /// zone (FOLLOW-UPS #63) — so every "has it started?", "is it too late to cancel?", "is it today?" must compare
    /// it with THIS clock. The server's <see cref="DateTime.UtcNow"/> is three and a half hours behind it, and .NET
    /// compares DateTimes by their digits whatever their Kind, so comparing the two directly is silently wrong: it
    /// refused to mark a 10:00 appointment done at 10:33 (QA 2026-09-24).
    ///
    /// <para><b>Rule.</b> Instants (created-at, sent-at, scheduled-for) stay UTC. Where an instant meets a booking
    /// time, convert the instant with <see cref="FromUtc"/> — or the booking time with <see cref="ToUtc"/> when the
    /// result must itself be an instant, like a reminder's send time. Never relabel a wall-clock value as UTC.</para>
    ///
    /// <para><b>Why a fixed offset.</b> Booksy operates in Iran, which stopped observing daylight saving time in
    /// 2022, and there is no per-salon zone anywhere in the system. This is the one place a real per-salon zone
    /// would go.</para>
    /// </summary>
    public static class SalonTime
    {
        /// <summary>Iran Standard Time.</summary>
        public static readonly TimeSpan UtcOffset = new(3, 30, 0);

        /// <summary>The salon's clock right now.</summary>
        public static DateTime Now => FromUtc(DateTime.UtcNow);

        /// <summary>
        /// What the salon's clock shows at <paramref name="instant"/>. An unmarked value is taken to be UTC, the
        /// rule for instants everywhere in this system.
        /// </summary>
        public static DateTime FromUtc(DateTime instant)
        {
            var utc = instant.Kind == DateTimeKind.Local ? instant.ToUniversalTime() : instant;
            return DateTime.SpecifyKind(utc + UtcOffset, DateTimeKind.Unspecified);
        }

        /// <summary>The instant at which the salon's clock shows <paramref name="salonTime"/>.</summary>
        public static DateTime ToUtc(DateTime salonTime) =>
            DateTime.SpecifyKind(salonTime - UtcOffset, DateTimeKind.Utc);
    }
}
