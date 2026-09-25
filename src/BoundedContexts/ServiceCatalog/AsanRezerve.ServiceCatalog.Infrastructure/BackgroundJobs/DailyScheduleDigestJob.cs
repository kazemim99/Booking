using System.Buffers.Binary;
using System.Security.Cryptography;
using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.ServiceCatalog.Application.Services.Notifications;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Tells each salon, once a morning, how many appointments its day holds.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is a job and not a handler.</b> Every other notification in this system is raised by
    /// a command handler at the moment something happened, which is what lets the intent commit with the
    /// business change. Nothing in the business causes a digest: it is caused by the morning arriving, so
    /// something has to go looking for it.</para>
    ///
    /// <para><b>Why the count is taken here rather than captured earlier.</b> The raiser's rule is to capture
    /// the copy's inputs when the thing happened, because the sweep must never re-read a subject that may
    /// have changed. That rule is kept — for a digest, the thing that happens IS the count being taken.
    /// Raising the intent on the day's first booking would announce "you have 1 appointment" to a salon that
    /// ends up with nine.</para>
    ///
    /// <para><b>Salon-local.</b> There is no provider timezone anywhere in this system; booking times are
    /// salon wall-clock values in a single frame (FOLLOW-UPS #63). So <see cref="SendAt"/> is 08:00 in the
    /// same frame the bookings live in: <see cref="RunAsync"/> takes the salon's clock, and
    /// <see cref="ExecuteAsync"/> reads its UTC clock through <see cref="SalonTime"/>. Passing the UTC clock
    /// straight through sent the "08:00" digest at 11:30 salon time (QA 2026-09-24).</para>
    /// </remarks>
    public sealed class DailyScheduleDigestJob
    {
        /// <summary>The morning moment the digest is for.</summary>
        public static readonly TimeSpan SendAt = TimeSpan.FromHours(8);

        /// <summary>
        /// After this, the day is skipped rather than announced late.
        /// </summary>
        /// <remarks>
        /// A host restarting at lunchtime must not then tell a salon about a schedule it has already worked
        /// through — that notification is worse than none, because it reads as current. The window only ever
        /// matters on a restart; while the host is up, the timer hits <see cref="SendAt"/> within minutes.
        /// </remarks>
        public static readonly TimeSpan LatestSend = TimeSpan.FromHours(12);

        /// <summary>Statuses that put an appointment on the day's schedule.</summary>
        /// <remarks>
        /// <c>Completed</c> is included because a digest sent late in its window can follow an early
        /// appointment, and dropping it would undercount the day the salon is being told about. A
        /// <c>Requested</c> booking is deliberately excluded: it is not an appointment yet, and counting it
        /// would tell the salon it has work booked that it has not agreed to do.
        /// </remarks>
        private static readonly BookingStatus[] OnTheSchedule =
        {
            BookingStatus.Confirmed,
            BookingStatus.Completed,
        };

        private readonly ServiceCatalogDbContext _context;
        private readonly INotificationRaiser _raiser;
        private readonly IDateTimeProvider _clock;
        private readonly ILogger<DailyScheduleDigestJob> _logger;

        public DailyScheduleDigestJob(
            ServiceCatalogDbContext context,
            INotificationRaiser raiser,
            IDateTimeProvider clock,
            ILogger<DailyScheduleDigestJob> logger)
        {
            _context = context;
            _raiser = raiser;
            _clock = clock;
            _logger = logger;
        }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken = default) =>
            RunAsync(SalonTime.FromUtc(_clock.UtcNow), cancellationToken);

        /// <summary>
        /// One pass at an explicit moment. Public so the behaviour under test — which morning, and whether
        /// it is still morning — can actually be tested; a job that only read a clock could only be observed
        /// at 08:00 real time.
        /// </summary>
        /// <returns>How many digests were raised.</returns>
        public async Task<int> RunAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            var timeOfDay = now.TimeOfDay;
            if (timeOfDay < SendAt || timeOfDay >= LatestSend)
                return 0;

            var dayStart = now.Date;
            var dayEnd = dayStart.AddDays(1);

            var counts = await _context.Bookings
                .AsNoTracking()
                .Where(b => b.TimeSlot.StartTime >= dayStart && b.TimeSlot.StartTime < dayEnd)
                .Where(b => OnTheSchedule.Contains(b.Status))
                .GroupBy(b => b.ProviderId)
                .Select(g => new { ProviderId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // A salon with nothing on is not told it has nothing on: decided with the user 2026-09-20. It
            // falls out of the query rather than being filtered afterwards — a day with no bookings produces
            // no group, so there is nothing to suppress.
            if (counts.Count == 0)
                return 0;

            var providerIds = counts.Select(c => c.ProviderId).ToList();

            var salons = await _context.Providers
                .AsNoTracking()
                .Where(p => providerIds.Contains(p.Id))
                .Select(p => new { p.Id, p.OwnerId, p.Profile.BusinessName })
                .ToListAsync(cancellationToken);

            var byId = salons.ToDictionary(s => s.Id);
            var raised = 0;

            foreach (var entry in counts)
            {
                if (!byId.TryGetValue(entry.ProviderId, out var salon))
                    continue;

                // Addressed to the owner's user id. The inbox, the preference lookup and the device registry
                // are all keyed by user, so a provider id here would reach nobody at all.
                var recipientId = salon.OwnerId.Value;
                if (recipientId == Guid.Empty)
                    continue;

                await _raiser.RaiseAsync(
                    NotificationEventCode.DailyScheduleDigest,
                    recipientId: recipientId,

                    // Per salon per day, which is what makes a timer that sees the same morning sixteen times
                    // send one message. It must vary by day or tomorrow's digest would be swallowed as a
                    // duplicate of today's.
                    dedupKey: DigestKey(entry.ProviderId.Value, dayStart),
                    parameters: new Dictionary<string, string>
                    {
                        [NotificationParameter.BusinessName] = salon.BusinessName,
                        [NotificationParameter.Count] = entry.Count.ToString(),
                    },
                    subjectType: ProviderSubject,
                    subjectId: entry.ProviderId.Value,
                    cancellationToken: cancellationToken);

                raised++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (raised > 0)
                _logger.LogInformation("Raised {Count} daily schedule digests for {Day:yyyy-MM-dd}", raised, dayStart);

            return raised;
        }

        /// <summary>The subject a digest is filed under — the salon, not any one booking.</summary>
        public const string ProviderSubject = "Provider";

        /// <summary>
        /// A stable id for "this salon, this day", derived rather than stored.
        /// </summary>
        /// <remarks>
        /// The de-duplication key is a Guid and the outbox's unique index is what actually enforces
        /// once-per-day, so the key has to carry the day inside it. Hashing is not for secrecy here; it is
        /// the cheapest way to fold 20 bytes into 16 without inventing a collision between one salon's
        /// Tuesday and another's Wednesday.
        /// </remarks>
        internal static Guid DigestKey(Guid providerId, DateTime day)
        {
            Span<byte> seed = stackalloc byte[20];
            providerId.TryWriteBytes(seed);
            BinaryPrimitives.WriteInt32LittleEndian(seed[16..], DateOnly.FromDateTime(day).DayNumber);

            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(seed, hash);

            return new Guid(hash[..16]);
        }
    }
}
