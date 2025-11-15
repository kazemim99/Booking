using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Generates realistic customer reviews for completed bookings
    /// - 60% of completed bookings receive reviews (industry standard)
    /// - Persian language comments with cultural authenticity
    /// - Rating distribution: 50% excellent, 25% good, 15% average, 10% poor
    /// - Includes helpful votes and provider responses
    /// </summary>
    public sealed class ReviewSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<ReviewSeeder> _logger;
        private readonly Random _random = new Random(67890); // Deterministic for consistent results

        // Persian review comments by rating category
        private readonly string[] _excellentComments = new[]
        {
            "عالی بود! خیلی راضی بودم از خدمات. حتما دوباره میام.",
            "کیفیت کار فوق‌العاده. آرایشگر خیلی حرفه‌ای و با تجربه بود.",
            "بهترین تجربه‌ای بود که داشتم. محیط تمیز و آرام، کارکنان مودب و ماهر.",
            "واقعا عالی بود. دقیقا همون چیزی که می‌خواستم رو انجام دادن.",
            "خیلی خوشحالم که این جا رو پیدا کردم. کیفیت عالی و قیمت مناسب.",
            "کار فوق‌العاده‌ای انجام دادن. همه دوستام رو معرفی کردم.",
            "بی‌نظیر بود! از نتیجه خیلی راضی‌ام. خیلی ممنونم.",
            "محیط بسیار تمیز و مرتب. کارکنان فوق‌العاده حرفه‌ای و مهربون.",
            "دقت و ظرافت کارشون واقعا قابل تحسین بود. حتما برمی‌گردم.",
            "بهترین سرویسی که تا حالا دریافت کردم. کیفیت فوق‌العاده.",
            "خیلی راضی هستم. آرایشگر خیلی با ذوق و حرفه‌ای بود.",
            "عالی بود! محیط آرام و دلنشین، کار حرفه‌ای و دقیق.",
            "واقعا ممنونم. نتیجه کار خیلی بهتر از انتظارم بود.",
            "فوق‌العاده! هم کیفیت کار و هم رفتار کارکنان عالی بود.",
            "بهترین انتخاب برای خدمات زیبایی. کاملا توصیه می‌کنم.",
            "خیلی حرفه‌ای کار می‌کنن. از نتیجه بیش از حد راضی‌ام."
        };

        private readonly string[] _goodComments = new[]
        {
            "خوب بود. کار خوبی انجام دادن ولی فضای انتظار کمی شلوغ بود.",
            "در کل راضی هستم. قیمت کمی بالا بود ولی کیفیت خوب بود.",
            "خدمات خوبی بود. فقط کمی دیر شروع شد.",
            "کار خوبی انجام شد. پیشنهاد می‌کنم قبلش حتما رزرو کنید.",
            "رضایت‌بخش بود. کیفیت کار خوب ولی زمان انتظار طولانی بود.",
            "خوب بود ولی می‌تونست بهتر باشه. در کل راضی‌ام.",
            "کار خوبی کردن. فقط کمی عجله داشتن.",
            "قابل قبول بود. قیمت مناسب و کیفیت خوب.",
            "تجربه خوبی بود. فقط محیط کمی کوچیک بود.",
            "در حد انتظار بود. کار درست و حسابی انجام شد.",
            "خوب بود ولی چند نکته کوچیک قابل بهبود داشت.",
            "رضایت‌بخش. نسبت به قیمت، کیفیت خوبی داشت."
        };

        private readonly string[] _averageComments = new[]
        {
            "نه خوب نه بد. متوسط بود.",
            "قابل قبول بود ولی انتظار بیشتری داشتم.",
            "معمولی بود. چیز خاصی نبود.",
            "به نظرم می‌تونست بهتر باشه. در حد متوسط بود.",
            "خیلی خاص نبود ولی قیمت مناسبی داشت.",
            "کیفیت متوسط. احتمالا بار دیگه نمیام.",
            "تجربه معمولی بود. چیز خاصی که بگم نداره.",
            "در حد متوسط بود. هم خوب نبود هم بد.",
            "انتظار بیشتری داشتم. نتیجه اونطور که می‌خواستم نشد.",
            "قیمت و کیفیت متناسب بود ولی چیز خاصی نبود."
        };

        private readonly string[] _poorComments = new[]
        {
            "متاسفانه راضی نبودم. کیفیت کار خوب نبود.",
            "نتیجه اونی که انتظار داشتم نبود. ناراضی‌ام.",
            "کیفیت کار پایین بود. توصیه نمی‌کنم.",
            "از نتیجه کار اصلا راضی نیستم. وقت و پولم هدر رفت.",
            "خیلی بد بود. هیچ حرفه‌ای‌گری ندیدم.",
            "اصلا توصیه نمی‌کنم. کیفیت خیلی پایین بود.",
            "تجربه بدی بود. اصلا انتظارم این نبود.",
            "ناامیدکننده بود. کیفیت کار اصلا خوب نبود."
        };

        // Provider response templates
        private readonly string[] _providerResponses = new[]
        {
            "خیلی ممنونم از نظر شما. خوشحالیم که رضایت داشتید.",
            "سپاسگزاریم از اعتماد شما. منتظر دیدار مجددتون هستیم.",
            "ممنون از بازخورد مثبت شما. افتخار ما بود که خدمت‌رسانی کردیم.",
            "از اینکه رضایت داشتید خوشحالیم. همیشه در خدمت شما هستیم.",
            "متشکریم از نظر شما. تلاش ما همیشه جلب رضایت مشتریان عزیز است.",
            "سپاسگزاریم از اعتماد و انتخاب شما. امیدواریم همیشه در خدمتتون باشیم.",
            "ممنون از حمایت شما. رضایت شما برای ما بسیار ارزشمنده.",
            "از بازخورد شما سپاسگزاریم. خوشحالیم که تونستیم انتظارات شما رو برآورده کنیم."
        };

        public ReviewSeeder(
            ServiceCatalogDbContext context,
            ILogger<ReviewSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.Reviews.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Reviews already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting review seeding with Persian comments...");

                // Get completed bookings (reviews can only be created for completed bookings)
                var completedBookings = await _context.Bookings
                    .Where(b => b.Status == BookingStatus.Completed)
                    .ToListAsync(cancellationToken);

                if (!completedBookings.Any())
                {
                    _logger.LogWarning("No completed bookings found. Skipping review seeding.");
                    return;
                }

                var reviews = new List<Review>();

                // 60% of completed bookings get reviews (industry standard conversion rate)
                var bookingsWithReviews = completedBookings
                    .Where(_ => _random.Next(100) < 60)
                    .ToList();

                foreach (var booking in bookingsWithReviews)
                {
                    var review = CreateReviewForBooking(booking);
                    if (review != null)
                    {
                        reviews.Add(review);
                    }
                }

                await _context.Reviews.AddRangeAsync(reviews, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully seeded {ReviewCount} reviews from {BookingCount} completed bookings ({Percentage:F1}%)",
                    reviews.Count,
                    completedBookings.Count,
                    (double)reviews.Count / completedBookings.Count * 100);

                LogReviewStatistics(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding reviews");
                throw;
            }
        }

        private Review? CreateReviewForBooking(Domain.Aggregates.BookingAggregate.Booking booking)
        {
            try
            {
                // Generate rating based on realistic distribution
                var (rating, comment) = GenerateRealisticReview();

                var review = Review.Create(
                    booking.ProviderId,
                    booking.CustomerId,
                    booking.Id.Value,
                    rating,
                    comment,
                    isVerified: true, // Verified because tied to actual booking
                    createdBy: "ReviewSeeder");

                // Add helpful votes (older reviews have more votes)
                var daysSinceBooking = (DateTime.UtcNow - booking.CompletedAt ?? DateTime.UtcNow).Days;
                var voteCount = Math.Min(daysSinceBooking / 2, 20); // Max 20 votes

                for (int i = 0; i < voteCount; i++)
                {
                    // Higher rated reviews get more helpful votes
                    var helpfulProbability = rating >= 4.0m ? 80 : (rating >= 3.0m ? 50 : 30);

                    if (_random.Next(100) < helpfulProbability)
                    {
                        review.MarkAsHelpful();
                    }
                    else
                    {
                        review.MarkAsNotHelpful();
                    }
                }

                // Add provider response (30% of reviews, higher for negative reviews)
                var responseChance = rating < 3.0m ? 70 : 30;
                if (_random.Next(100) < responseChance)
                {
                    var response = GetRandomProviderResponse();
                    review.AddProviderResponse(response, "ReviewSeeder");
                }

                return review;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create review for booking {BookingId}", booking.Id.Value);
                return null;
            }
        }

        private (decimal rating, string comment) GenerateRealisticReview()
        {
            // Distribution: 50% excellent (4.5-5.0), 25% good (3.5-4.4), 15% average (2.5-3.4), 10% poor (1.5-2.4)
            var distribution = _random.Next(100);

            if (distribution < 50) // 50% excellent
            {
                var rating = GenerateRatingInRange(4.5m, 5.0m);
                var comment = _excellentComments[_random.Next(_excellentComments.Length)];
                return (rating, comment);
            }
            else if (distribution < 75) // 25% good
            {
                var rating = GenerateRatingInRange(3.5m, 4.4m);
                var comment = _goodComments[_random.Next(_goodComments.Length)];
                return (rating, comment);
            }
            else if (distribution < 90) // 15% average
            {
                var rating = GenerateRatingInRange(2.5m, 3.4m);
                var comment = _averageComments[_random.Next(_averageComments.Length)];
                return (rating, comment);
            }
            else // 10% poor
            {
                var rating = GenerateRatingInRange(1.5m, 2.4m);
                var comment = _poorComments[_random.Next(_poorComments.Length)];
                return (rating, comment);
            }
        }

        private decimal GenerateRatingInRange(decimal min, decimal max)
        {
            var range = max - min;
            var randomValue = (decimal)_random.NextDouble() * range;
            var rating = min + randomValue;

            // Round to nearest 0.5 (half-star increments)
            rating = Math.Round(rating * 2, MidpointRounding.AwayFromZero) / 2;
            rating = Math.Max(1.0m, Math.Min(5.0m, rating));

            return rating;
        }

        private string GetRandomProviderResponse()
        {
            return _providerResponses[_random.Next(_providerResponses.Length)];
        }

        private void LogReviewStatistics(List<Review> reviews)
        {
            var statistics = new
            {
                Total = reviews.Count,
                Excellent = reviews.Count(r => r.RatingValue >= 4.5m),
                Good = reviews.Count(r => r.RatingValue >= 3.5m && r.RatingValue < 4.5m),
                Average = reviews.Count(r => r.RatingValue >= 2.5m && r.RatingValue < 3.5m),
                Poor = reviews.Count(r => r.RatingValue < 2.5m),
                WithProviderResponse = reviews.Count(r => !string.IsNullOrEmpty(r.ProviderResponse)),
                Verified = reviews.Count(r => r.IsVerified),
                AverageRating = reviews.Any() ? reviews.Average(r => r.RatingValue) : 0m,
                AverageHelpfulVotes = reviews.Any() ? reviews.Average(r => r.HelpfulCount) : 0
            };

            _logger.LogInformation(
                "Review Statistics: Total={Total}, Excellent={Excellent}, Good={Good}, Average={Average}, Poor={Poor}",
                statistics.Total,
                statistics.Excellent,
                statistics.Good,
                statistics.Average,
                statistics.Poor);

            _logger.LogInformation(
                "Review Engagement: ProviderResponses={Responses}, Verified={Verified}, AvgRating={AvgRating:F2}, AvgHelpfulVotes={AvgVotes:F1}",
                statistics.WithProviderResponse,
                statistics.Verified,
                statistics.AverageRating,
                statistics.AverageHelpfulVotes);
        }
    }
}
