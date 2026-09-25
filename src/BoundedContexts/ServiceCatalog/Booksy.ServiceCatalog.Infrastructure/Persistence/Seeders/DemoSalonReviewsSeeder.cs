using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Booksy.ServiceCatalog.Infrastructure.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Gives the demo salon a history of customer reviews: mixed ratings, Persian comments that match them, the optional
    /// dimension ratings, the salon's reply on most, and helpful/not-helpful votes — all published, and the salon's rating
    /// computed from them (openspec/changes/_inline/customer-reviews-and-nahal-seed: «برای سالن نهال یکسری کامنت با پاسخ
    /// به صورت random و rate»).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every review stands on what a real one stands on: a completed visit of the reviewer's own (a booking in the past,
    /// marked done), the review written after it, published by moderation, and the salon's reply approved. That is why
    /// <see cref="ReviewSeeder"/> produced nothing for two weeks: it only reviews completed bookings, and no seeder makes
    /// any. The reviewers are real people in the person directory, so the listing names them («مریم ر.») — the host
    /// creates them (it is the one project that reaches both contexts) and passes their ids in.
    /// </para>
    /// <para>
    /// "Random" is a fixed seed: every environment gets the same reviews, and a test can say what they are. Seeding twice
    /// adds nothing — its reviews carry <see cref="Marker"/> as their moderator, which nothing else writes.
    /// </para>
    /// </remarks>
    public sealed class DemoSalonReviewsSeeder
    {
        /// <summary>The salon the product is demonstrated with (see <see cref="ProviderSeeder.DemoProviders"/>).</summary>
        public const string DemoSalonName = "سالن نهال";

        /// <summary>Who "moderated" the seeded reviews — how they are told apart from real ones, and found to remove.</summary>
        public const string Marker = "DemoSalonReviewsSeeder";

        /// <summary>How many reviews the salon gets.</summary>
        public const int ReviewCount = 18;

        /// <summary>The seed every environment uses.</summary>
        public const int Seed = 1403;

        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<DemoSalonReviewsSeeder> _logger;

        public DemoSalonReviewsSeeder(ServiceCatalogDbContext context, ILogger<DemoSalonReviewsSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>One review to write: who (an index into the reviewers), when, what they said, and the salon's answer.</summary>
        public sealed record PlannedReview(
            int Reviewer,
            int DaysAgo,
            int Hour,
            decimal Rating,
            string Comment,
            ReviewDimensionRatings Dimensions,
            string? Reply,
            int HoursToReview,
            int HoursToReply);

        // ── What people write, by how the visit went. A women's salon: cut, colour, keratin, bridal make-up. ──

        private static readonly string[] Excellent =
        {
            "رنگ موهام دقیقاً همونی شد که می‌خواستم. خیلی با حوصله مشاوره دادن و نتیجه عالی بود.",
            "کوتاهی موهام فوق‌العاده شد، همه ازم می‌پرسن کجا رفتم. حتماً دوباره میام.",
            "محیط خیلی تمیز و آرومه و پرسنل بسیار مؤدب هستن. از احیای مو واقعاً راضی‌ام.",
            "برای آرایش عروسی خواهرم رفتیم، کار بی‌نقص بود و تا آخر شب هیچ تغییری نکرد.",
            "سر وقت نوبتم رو شروع کردن و اصلاً معطل نشدم. کیفیت کار هم عالی بود.",
            "بهترین سالنی که تا حالا رفتم؛ دقیق، تمیز و با سلیقه. به دوستام هم معرفی کردم.",
            "کراتینه موهام رو اینجا انجام دادم، موهام خیلی نرم و براق شده. ممنونم.",
            "خانم آرایشگر خیلی حرفه‌ای بود و دقیق گوش داد که چی می‌خوام. نتیجه از انتظارم بهتر شد.",
            "هم برخوردشون گرم و صمیمی بود هم کارشون تمیز. قیمت‌ها هم منصفانه‌ست.",
            "اولین بار بود می‌رفتم و خیلی راضی برگشتم. از این به بعد مشتری ثابتشونم.",
            "هایلایت موهام خیلی طبیعی و قشنگ شد. دستشون درد نکنه.",
            "اصلاح ابرو و میکاپم خیلی ظریف و تمیز انجام شد، دقیقاً همون چیزی که دوست داشتم.",
        };

        private static readonly string[] Good =
        {
            "در کل راضی بودم و کار تمیز انجام شد، فقط حدود ربع ساعت منتظر موندم.",
            "رنگ خوب دراومد ولی یه کم از چیزی که فکر می‌کردم تیره‌تر شد. برخوردشون خوب بود.",
            "کار خوب بود و محیط تمیز، فقط سالن اون روز شلوغ بود و کمی عجله داشتن.",
            "کوتاهی رو دوست داشتم؛ فقط کاش توضیح بیشتری درباره مراقبت بعدش می‌دادن.",
            "کیفیت کار نسبت به قیمت خوبه. دفعه بعد هم احتمالاً همین‌جا میام.",
            "پرسنل مهربون بودن و کار خوب انجام شد، فقط جای پارک نزدیکش پیدا نمیشه.",
            "از براشینگ راضی بودم ولی تا شب یه کم حالتش رو از دست داد.",
            "کار مرتب بود. فقط کاش نوبت‌ها با فاصله بیشتری گذاشته بشه که عجله نباشه.",
        };

        private static readonly string[] Average =
        {
            "بد نبود ولی انتظار بیشتری داشتم. رنگ یکدست درنیومد و مجبور شدم دوباره برم.",
            "کار متوسط بود. نیم ساعت دیرتر از نوبتم شروع شد و کسی هم توضیحی نداد.",
            "کوتاهی معمولی بود، چیز خاصی نبود. برای قیمتش انتظار بیشتری داشتم.",
            "برخورد خوب بود ولی نتیجه کار اونی که می‌خواستم نشد.",
            "محیط تمیز بود ولی خیلی شلوغ بود و حس کردم با عجله کار کردن.",
            "نتیجه قابل قبول بود، ولی هزینه نهایی از چیزی که اول گفته بودن بیشتر شد.",
        };

        private static readonly string[] Poor =
        {
            "متأسفانه رنگ موهام اصلاً اون چیزی که خواسته بودم نشد و کلی هزینه کردم.",
            "یک ساعت منتظر موندم و آخرش هم با عجله کارم رو انجام دادن. راضی نبودم.",
            "کوتاهی نامرتب شد و مجبور شدم جای دیگه درستش کنم.",
            "برخورد یکی از پرسنل مناسب نبود و حس خوبی نداشتم.",
            "نوبتم رو بدون هماهنگی جابه‌جا کردن و کار هم کیفیت خوبی نداشت.",
        };

        // ── What the salon answers, in its own voice. ──

        private static readonly string[] ThanksReplies =
        {
            "خیلی ممنون از لطف و انرژی خوبتون! خوشحالیم که راضی بودید، منتظر دیدار دوباره‌تون هستیم. 🌸",
            "سپاس از اعتمادتون عزیزم. رضایت شما بزرگ‌ترین انگیزه تیم ماست.",
            "ممنون که وقت گذاشتید و نظرتون رو نوشتید. همیشه در خدمتتون هستیم.",
            "چه خوب که نتیجه رو دوست داشتید! ممنون از معرفی‌تون به دوستانتون. ❤️",
            "لطف دارید. خوشحالیم که تجربه خوبی در سالن نهال داشتید.",
        };

        private static readonly string[] ImproveReplies =
        {
            "ممنون از نظر دقیقتون. درباره زمان انتظار حق با شماست و نوبت‌دهی رو منظم‌تر می‌کنیم.",
            "سپاس از بازخوردتون. نکته‌ای که گفتید رو با همکارانمون در میان گذاشتیم تا دفعه بعد بهتر باشه.",
            "ممنون که گفتید. دفعه بعد قبل از شروع، توضیح کامل‌تری درباره نتیجه و مراقبت‌ها می‌دیم.",
        };

        private static readonly string[] ApologyReplies =
        {
            "از اینکه تجربه خوبی نداشتید واقعاً متأسفیم. لطفاً با شماره سالن تماس بگیرید تا بدون هزینه جبران کنیم.",
            "عذرخواهی می‌کنیم. این تجربه با استانداردهای ما فاصله داره؛ خوشحال می‌شیم فرصت جبران به ما بدید.",
            "ممنون که صادقانه نوشتید. موضوع رو بررسی کردیم و برای جبران با شما تماس می‌گیریم.",
        };

        /// <summary>
        /// The reviews to write, from a seeded random: mostly good, some middling, a few poor — as a salon worth
        /// showing really looks. Pure, so its shape is testable without a database.
        /// </summary>
        public static IReadOnlyList<PlannedReview> Plan(Random random, int reviewers, int count = ReviewCount)
        {
            if (reviewers < 1) throw new ArgumentOutOfRangeException(nameof(reviewers));

            var pools = new Dictionary<string[], Queue<string>>();
            string Draw(string[] pool)
            {
                if (!pools.TryGetValue(pool, out var queue) || queue.Count == 0)
                    pools[pool] = queue = new Queue<string>(pool.OrderBy(_ => random.Next()));
                return queue.Dequeue();
            }

            var plan = new List<PlannedReview>(count);
            for (var i = 0; i < count; i++)
            {
                var band = random.Next(100);
                var (rating, comments, replies, replyChance) = band switch
                {
                    < 55 => (Pick(random, 4.5m, 5.0m, 5.0m), Excellent, ThanksReplies, 65),
                    < 78 => (Pick(random, 3.5m, 4.0m, 4.0m), Good, ImproveReplies, 70),
                    < 91 => (Pick(random, 2.5m, 3.0m, 3.0m), Average, ImproveReplies, 90),
                    _ => (Pick(random, 1.0m, 1.5m, 2.0m), Poor, ApologyReplies, 100),
                };

                plan.Add(new PlannedReview(
                    Reviewer: random.Next(reviewers),
                    DaysAgo: random.Next(3, 150),
                    Hour: random.Next(10, 19),
                    Rating: rating,
                    Comment: Draw(comments),
                    Dimensions: new ReviewDimensionRatings(
                        Dimension(random, rating), Dimension(random, rating), Dimension(random, rating), Dimension(random, rating)),
                    Reply: random.Next(100) < replyChance ? Draw(replies) : null,
                    HoursToReview: random.Next(2, 30),
                    HoursToReply: random.Next(3, 48)));
            }

            return plan;
        }

        private static decimal Pick(Random random, params decimal[] values) => values[random.Next(values.Length)];

        /// <summary>A dimension near the overall verdict (within a star), or left out as a real customer often does.</summary>
        private static decimal? Dimension(Random random, decimal overall)
        {
            if (random.Next(100) >= 75) return null;
            var shift = Pick(random, -1.0m, -0.5m, 0m, 0m, 0.5m);
            return Math.Clamp(overall + shift, 1.0m, 5.0m);
        }

        /// <summary>The demo salon, when this environment has it and it has no seeded reviews yet.</summary>
        public async Task<Provider?> SalonToSeedAsync(string salonName, CancellationToken cancellationToken = default)
        {
            var salon = await _context.Providers.FirstOrDefaultAsync(p => p.Profile.BusinessName == salonName, cancellationToken);
            if (salon is null)
            {
                _logger.LogInformation("No salon named {Salon}; no demo reviews to seed", salonName);
                return null;
            }

            var seeded = await _context.Reviews.AnyAsync(
                r => r.ProviderId == salon.Id && r.ModeratedBy == Marker, cancellationToken);
            if (seeded)
            {
                _logger.LogInformation("{Salon} already has its demo reviews", salonName);
                return null;
            }

            return salon;
        }

        /// <summary>
        /// Writes the planned reviews for the salon, by these reviewers (people in the directory). Returns how many were
        /// written: zero when the salon is missing, already seeded, or has no service to have visited.
        /// </summary>
        public async Task<int> SeedAsync(
            string salonName, IReadOnlyList<Guid> reviewerIds, CancellationToken cancellationToken = default)
        {
            if (reviewerIds.Count < 2)
                throw new ArgumentException("Votes need someone besides the author.", nameof(reviewerIds));

            var salon = await SalonToSeedAsync(salonName, cancellationToken);
            if (salon is null) return 0;

            var services = await _context.Services
                .Where(s => s.ProviderId == salon.Id)
                .OrderByDescending(s => s.Status == ServiceStatus.Active)
                .ThenBy(s => s.Name)
                .ToListAsync(cancellationToken);
            if (services.Count == 0)
            {
                _logger.LogWarning("{Salon} has no services, so no visit to review", salonName);
                return 0;
            }

            var staff = await _context.OrganizationMemberships
                .Where(m => m.OrganizationId == salon.Id && m.Status == MembershipStatus.Active)
                .OrderBy(m => m.Id)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            var random = new Random(Seed);
            var plan = Plan(random, reviewerIds.Count);
            var today = SalonTime.Now.Date;
            var written = new List<(Booking Booking, Review Review, PlannedReview Plan, DateTime End)>();

            foreach (var planned in plan)
            {
                var service = services[random.Next(services.Count)];
                var staffId = staff.Count > 0 ? staff[random.Next(staff.Count)] : salon.Id.Value;
                var start = today.AddDays(-planned.DaysAgo).AddHours(planned.Hour);
                var reviewer = UserId.From(reviewerIds[planned.Reviewer]);

                // The visit: booked by the salon for this customer, and marked done — in the past, so both are legal.
                var booking = Booking.CreateConfirmedByProvider(
                    reviewer, salon.Id, service.Id, staffId, start, service.Duration, service.BasePrice,
                    service.BookingPolicy ?? BookingPolicy.Default);
                booking.Complete();
                booking.ClearDomainEvents();

                var review = Review.Create(
                    salon.Id, reviewer, booking.Id.Value, planned.Rating, planned.Comment,
                    isVerified: true, createdBy: Marker, dimensions: planned.Dimensions);
                review.Publish(Marker);
                if (planned.Reply is { } reply)
                {
                    review.AddProviderResponse(reply, Marker);
                    review.ApproveReply(Marker);
                }
                review.ClearDomainEvents();

                _context.Bookings.Add(booking);
                _context.Reviews.Add(review);
                written.Add((booking, review, planned, start.AddMinutes(service.Duration.Value)));
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Saving stamps "now" on everything; put each visit, review and reply back where it happened. Stored
            // instants are UTC; the visit's own times are the salon's clock.
            foreach (var (booking, review, planned, end) in written)
            {
                var visitEnd = SalonTime.ToUtc(end);
                var reviewedAt = visitEnd.AddHours(planned.HoursToReview);
                var repliedAt = reviewedAt.AddHours(planned.HoursToReply);

                DateTime? bookedAt = visitEnd.AddDays(-2);
                DateTime? publishedAt = reviewedAt.AddHours(1);
                DateTime? answeredAt = planned.Reply is null ? null : repliedAt;

                await _context.Bookings.Where(b => b.Id == booking.Id).ExecuteUpdateAsync(set => set
                    .SetProperty(b => b.RequestedAt, bookedAt.Value)
                    .SetProperty(b => b.ConfirmedAt, bookedAt)
                    .SetProperty(b => b.CompletedAt, (DateTime?)visitEnd), cancellationToken);

                await _context.Reviews.Where(r => r.Id == review.Id).ExecuteUpdateAsync(set => set
                    .SetProperty(r => r.CreatedAt, reviewedAt)
                    .SetProperty(r => r.ModeratedAt, publishedAt)
                    .SetProperty(r => r.FirstPublishedAt, publishedAt)
                    .SetProperty(r => r.ProviderResponseAt, answeredAt)
                    .SetProperty(r => r.LastModifiedAt, answeredAt ?? publishedAt),
                    cancellationToken);
            }

            await SeedVotesAsync(written.Select(w => w.Review).ToList(), reviewerIds, random, cancellationToken);

            await new ProviderRatingRecomputer(_context).RecomputeAsync(salon.Id, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Seeded {Count} demo reviews for {Salon} ({Replies} with the salon's reply)",
                written.Count, salonName, written.Count(w => w.Plan.Reply is not null));
            return written.Count;
        }

        /// <summary>
        /// Votes as rows by the other reviewers, never the author — the same shape <see cref="ReviewSeeder.PlanVotes"/>
        /// gives any seeded review — with each review's live tallies brought in line, as a real vote does.
        /// </summary>
        private async Task SeedVotesAsync(
            IReadOnlyList<Review> reviews, IReadOnlyList<Guid> reviewerIds, Random random, CancellationToken cancellationToken)
        {
            foreach (var review in reviews)
            {
                var voters = reviewerIds.Where(id => id != review.CustomerId.Value).OrderBy(_ => random.Next()).ToList();
                var votes = ReviewSeeder.PlanVotes(review.RatingValue, random).Take(voters.Count).ToList();
                if (votes.Count == 0) continue;

                for (var i = 0; i < votes.Count; i++)
                    _context.ReviewVotes.Add(ReviewVote.Cast(review.Id, UserId.From(voters[i]), votes[i], DateTime.UtcNow));

                var helpful = votes.Count(v => v);
                await _context.Reviews.Where(r => r.Id == review.Id).ExecuteUpdateAsync(set => set
                    .SetProperty(r => r.HelpfulVoteCount, helpful)
                    .SetProperty(r => r.NotHelpfulVoteCount, votes.Count - helpful), cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
