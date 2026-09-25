using System.Globalization;
using System.Text.RegularExpressions;
using AsanRezerve.Core.Domain.Abstractions.Rules;
using AsanRezerve.Core.Domain.Exceptions;

namespace AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate
{
    /// <summary>
    /// A discount a customer can receive on a booking (openspec/changes/add-discounts-and-campaigns).
    ///
    /// <para><b>Two owners, one model.</b> A <see cref="PromotionOwner.Provider"/> promotion is a salon's own offer on
    /// its own services. A <see cref="PromotionOwner.Platform"/> promotion is an admin campaign that applies only at
    /// salons that joined it (<see cref="CampaignEnrollment"/>). Every discount is funded by the salon that honours
    /// it (decision 2026-09-25), so a booking's price simply is the discounted price.</para>
    ///
    /// <para><b>Usage.</b> <see cref="RedemptionCount"/> is the number of bookings currently holding this discount. It is
    /// the aggregate's concurrency token: every redemption and release updates this row, so two customers racing for
    /// the last use serialize and the loser's request fails with a conflict instead of both getting it.</para>
    ///
    /// <para><b>Time.</b> The validity window is made of UTC instants and is compared with the moment of booking. Days
    /// of the week and the daily window describe the salon's clock and are compared with the appointment's start,
    /// which is already a salon wall-clock value (see <c>SalonTime</c>).</para>
    /// </summary>
    public sealed class Promotion : AggregateRoot<Guid>
    {
        public const int MaxTitleLength = 80;
        public const int MaxDescriptionLength = 500;
        public const int MaxTargetedServices = 100;
        public const decimal MinPercentage = 1m;
        public const decimal MaxPercentage = 90m;
        public const decimal MaxAmount = 1_000_000_000m;

        private static readonly Regex CodePattern = new("^[A-Z0-9-]{4,20}$", RegexOptions.Compiled);

        private List<Guid> _serviceIds = new();

        public PromotionOwner Owner { get; private set; }

        /// <summary>The salon that runs this promotion; null for a platform campaign.</summary>
        public ProviderId? ProviderId { get; private set; }

        public string Title { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public PromotionActivation Activation { get; private set; }

        /// <summary>Upper-case, only for <see cref="PromotionActivation.Code"/>.</summary>
        public string? Code { get; private set; }

        public DiscountKind DiscountKind { get; private set; }

        /// <summary>A percentage (1–90) or an amount in Toman, per <see cref="DiscountKind"/>.</summary>
        public decimal DiscountValue { get; private set; }

        /// <summary>Upper bound of a percentage discount, in Toman.</summary>
        public decimal? MaxDiscountAmount { get; private set; }

        public decimal? MinimumSubtotal { get; private set; }
        public bool NewCustomersOnly { get; private set; }

        /// <summary>Services the discount applies to; empty means every service of the visit.</summary>
        public IReadOnlyList<Guid> ServiceIds => _serviceIds;

        /// <summary>Bit per day (<c>1 &lt;&lt; (int)DayOfWeek</c>); 0 means every day.</summary>
        public int DaysOfWeekMask { get; private set; }

        public TimeOnly? DailyStartTime { get; private set; }
        public TimeOnly? DailyEndTime { get; private set; }
        public DateTime StartsAt { get; private set; }
        public DateTime? EndsAt { get; private set; }
        public int? TotalUsageLimit { get; private set; }
        public int? PerCustomerLimit { get; private set; }
        public int RedemptionCount { get; private set; }
        public PromotionStatus Status { get; private set; }

        /// <summary>Paused by an administrator: the salon cannot resume it on its own.</summary>
        public bool PausedByPlatform { get; private set; }

        public string Currency { get; private set; } = PlatformCurrency.Code;
        public Guid CreatedBy { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }

        public IReadOnlyCollection<DayOfWeek> DaysOfWeek =>
            Enum.GetValues<DayOfWeek>().Where(d => (DaysOfWeekMask & (1 << (int)d)) != 0).ToList();

        private Promotion()
        {
        }

        public static Promotion CreateForProvider(ProviderId providerId, PromotionTerms terms, Guid createdBy, DateTime nowUtc)
        {
            if (providerId is null)
                throw new DomainValidationException(nameof(ProviderId), "تخفیف باید متعلق به یک سالن باشد.");

            return Create(PromotionOwner.Provider, providerId, terms, createdBy, nowUtc);
        }

        public static Promotion CreatePlatformCampaign(PromotionTerms terms, Guid createdBy, DateTime nowUtc) =>
            Create(PromotionOwner.Platform, null, terms, createdBy, nowUtc);

        private static Promotion Create(
            PromotionOwner owner, ProviderId? providerId, PromotionTerms terms, Guid createdBy, DateTime nowUtc)
        {
            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                Owner = owner,
                ProviderId = providerId,
                Status = PromotionStatus.Active,
                CreatedBy = createdBy,
                CreatedAt = nowUtc,
            };
            promotion.Apply(terms, nowUtc);
            return promotion;
        }

        /// <summary>
        /// Replaces the terms. Bookings already made keep the discount they were given; only the code is frozen once
        /// used, because customers were told it.
        /// </summary>
        public void Update(PromotionTerms terms, DateTime nowUtc)
        {
            if (Status == PromotionStatus.Ended)
                throw Refused("PROMOTION_ENDED", "این تخفیف پایان یافته و قابل ویرایش نیست.");

            var newCode = terms.Activation == PromotionActivation.Code ? NormalizeCode(terms.Code) : null;
            if (RedemptionCount > 0 && !string.Equals(newCode, Code, StringComparison.Ordinal))
                throw Refused("PROMOTION_CODE_LOCKED", "کد تخفیفی که استفاده شده است قابل تغییر نیست.");

            Apply(terms, nowUtc);
            UpdatedAt = nowUtc;
        }

        public void Pause(bool byPlatform, DateTime nowUtc)
        {
            EnsureNotEnded();
            if (Status == PromotionStatus.Paused)
                throw Refused("PROMOTION_ALREADY_PAUSED", "این تخفیف از قبل متوقف است.");

            Status = PromotionStatus.Paused;
            PausedByPlatform = byPlatform;
            UpdatedAt = nowUtc;
        }

        public void Resume(bool byPlatform, DateTime nowUtc)
        {
            EnsureNotEnded();
            if (Status != PromotionStatus.Paused)
                throw Refused("PROMOTION_NOT_PAUSED", "این تخفیف متوقف نیست.");
            if (PausedByPlatform && !byPlatform)
                throw Refused("PROMOTION_PAUSED_BY_PLATFORM",
                    "این تخفیف توسط پشتیبانی متوقف شده است؛ برای فعال‌سازی با پشتیبانی تماس بگیرید.");

            Status = PromotionStatus.Active;
            PausedByPlatform = false;
            UpdatedAt = nowUtc;
        }

        public void End(DateTime nowUtc)
        {
            EnsureNotEnded();
            Status = PromotionStatus.Ended;
            EndedAt = nowUtc;
            UpdatedAt = nowUtc;
        }

        public PromotionState StateAt(DateTime nowUtc)
        {
            if (Status == PromotionStatus.Ended) return PromotionState.Ended;
            if (Status == PromotionStatus.Paused) return PromotionState.Paused;
            if (nowUtc < StartsAt) return PromotionState.Scheduled;
            if (EndsAt.HasValue && nowUtc >= EndsAt.Value) return PromotionState.Expired;
            if (TotalUsageLimit.HasValue && RedemptionCount >= TotalUsageLimit.Value) return PromotionState.Exhausted;
            return PromotionState.Active;
        }

        /// <summary>
        /// Whether this promotion applies to the visit and how much it takes off. Pure: never changes the promotion.
        /// </summary>
        public PromotionEvaluation Evaluate(PromotionContext context, int customerPriorUses)
        {
            var state = StateAt(context.NowUtc);
            var stateReason = state switch
            {
                PromotionState.Scheduled => "این تخفیف هنوز شروع نشده است.",
                PromotionState.Expired => "مهلت این تخفیف تمام شده است.",
                PromotionState.Exhausted => "ظرفیت استفاده از این تخفیف تکمیل شده است.",
                PromotionState.Paused or PromotionState.Ended => "این تخفیف در حال حاضر فعال نیست.",
                _ => null
            };
            if (stateReason is not null)
                return PromotionEvaluation.NotEligible(stateReason);

            if (PerCustomerLimit.HasValue && customerPriorUses >= PerCustomerLimit.Value)
                return PromotionEvaluation.NotEligible("شما قبلاً از این تخفیف استفاده کرده‌اید.");

            if (NewCustomersOnly && !context.IsNewCustomer)
                return PromotionEvaluation.NotEligible("این تخفیف فقط برای اولین نوبت در این سالن است.");

            var appointmentDay = (DayOfWeek)(int)context.AppointmentStart.DayOfWeek;
            if (DaysOfWeekMask != 0 && (DaysOfWeekMask & (1 << (int)appointmentDay)) == 0)
                return PromotionEvaluation.NotEligible("این تخفیف در روز انتخاب‌شده معتبر نیست.");

            if (DailyStartTime.HasValue && DailyEndTime.HasValue)
            {
                var at = TimeOnly.FromDateTime(context.AppointmentStart);
                if (at < DailyStartTime.Value || at >= DailyEndTime.Value)
                    return PromotionEvaluation.NotEligible(
                        $"این تخفیف فقط برای نوبت‌های ساعت {DailyStartTime.Value:HH\\:mm} تا {DailyEndTime.Value:HH\\:mm} است.");
            }

            var lines = _serviceIds.Count == 0
                ? context.Lines
                : context.Lines.Where(l => _serviceIds.Contains(l.ServiceId)).ToList();
            if (lines.Count == 0)
                return PromotionEvaluation.NotEligible("این تخفیف شامل خدمات انتخاب‌شده نمی‌شود.");

            var eligibleSubtotal = lines.Sum(l => l.Amount);
            if (MinimumSubtotal.HasValue && eligibleSubtotal < MinimumSubtotal.Value)
                return PromotionEvaluation.NotEligible(
                    $"حداقل مبلغ برای این تخفیف {MinimumSubtotal.Value.ToString("N0", CultureInfo.InvariantCulture)} تومان است.");

            var discount = CalculateDiscount(eligibleSubtotal);
            if (discount <= 0)
                return PromotionEvaluation.NotEligible("مبلغ این نوبت برای این تخفیف کافی نیست.");

            return PromotionEvaluation.Eligible(discount, eligibleSubtotal);
        }

        /// <summary>
        /// Counts one more booking holding this discount. The caller has already priced the visit through
        /// <see cref="Evaluate"/>; this re-checks the limits against the state being saved, which the concurrency
        /// token then guards.
        /// </summary>
        public void RecordRedemption(int customerPriorUses, DateTime nowUtc)
        {
            var state = StateAt(nowUtc);
            if (state == PromotionState.Exhausted)
                throw Refused("PROMOTION_EXHAUSTED", "ظرفیت استفاده از این تخفیف تکمیل شده است.");
            if (state != PromotionState.Active)
                throw Refused("PROMOTION_NOT_ACTIVE", "این تخفیف در حال حاضر فعال نیست.");
            if (PerCustomerLimit.HasValue && customerPriorUses >= PerCustomerLimit.Value)
                throw Refused("PROMOTION_CUSTOMER_LIMIT", "شما قبلاً از این تخفیف استفاده کرده‌اید.");

            RedemptionCount++;
        }

        /// <summary>A booking holding this discount was cancelled; its use goes back. Accepted in any status.</summary>
        public void ReleaseRedemption()
        {
            if (RedemptionCount > 0)
                RedemptionCount--;
        }

        public bool CanBeJoinedAt(DateTime nowUtc) =>
            Owner == PromotionOwner.Platform
            && StateAt(nowUtc) is PromotionState.Scheduled or PromotionState.Active or PromotionState.Paused
                or PromotionState.Exhausted;

        public static string? NormalizeCode(string? code) =>
            string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

        private decimal CalculateDiscount(decimal eligibleSubtotal)
        {
            var raw = DiscountKind == DiscountKind.Percentage
                ? eligibleSubtotal * DiscountValue / 100m
                : DiscountValue;

            if (DiscountKind == DiscountKind.Percentage && MaxDiscountAmount.HasValue)
                raw = Math.Min(raw, MaxDiscountAmount.Value);

            raw = Math.Min(raw, eligibleSubtotal * Policies.PromotionPricing.MaxDiscountShare);
            return Math.Floor(raw);
        }

        private void Apply(PromotionTerms terms, DateTime nowUtc)
        {
            if (terms is null)
                throw new DomainValidationException(nameof(PromotionTerms), "مشخصات تخفیف الزامی است.");

            var title = terms.Title?.Trim();
            if (string.IsNullOrEmpty(title))
                throw new DomainValidationException(nameof(Title), "عنوان تخفیف الزامی است.");
            if (title.Length > MaxTitleLength)
                throw new DomainValidationException(nameof(Title), $"عنوان تخفیف حداکثر {MaxTitleLength} کاراکتر است.");

            var description = string.IsNullOrWhiteSpace(terms.Description) ? null : terms.Description.Trim();
            if (description?.Length > MaxDescriptionLength)
                throw new DomainValidationException(nameof(Description), $"توضیحات حداکثر {MaxDescriptionLength} کاراکتر است.");

            string? code = null;
            if (terms.Activation == PromotionActivation.Code)
            {
                code = NormalizeCode(terms.Code);
                if (code is null)
                    throw new DomainValidationException(nameof(Code), "برای تخفیف کددار، کد الزامی است.");
                if (!CodePattern.IsMatch(code))
                    throw new DomainValidationException(nameof(Code),
                        "کد تخفیف باید ۴ تا ۲۰ کاراکتر و فقط شامل حروف انگلیسی، عدد و خط تیره باشد.");
            }

            switch (terms.DiscountKind)
            {
                case DiscountKind.Percentage:
                    if (terms.DiscountValue < MinPercentage || terms.DiscountValue > MaxPercentage)
                        throw new DomainValidationException(nameof(DiscountValue), "درصد تخفیف باید بین ۱ تا ۹۰ باشد.");
                    if (terms.MaxDiscountAmount.HasValue && terms.MaxDiscountAmount.Value <= 0)
                        throw new DomainValidationException(nameof(MaxDiscountAmount), "سقف تخفیف باید بیشتر از صفر باشد.");
                    break;
                case DiscountKind.FixedAmount:
                    if (terms.DiscountValue <= 0 || terms.DiscountValue > MaxAmount)
                        throw new DomainValidationException(nameof(DiscountValue), "مبلغ تخفیف باید بیشتر از صفر باشد.");
                    if (terms.DiscountValue != Math.Floor(terms.DiscountValue))
                        throw new DomainValidationException(nameof(DiscountValue), "مبلغ تخفیف باید به تومان و بدون اعشار باشد.");
                    if (terms.MaxDiscountAmount.HasValue)
                        throw new DomainValidationException(nameof(MaxDiscountAmount), "سقف تخفیف فقط برای تخفیف درصدی است.");
                    break;
                default:
                    throw new DomainValidationException(nameof(DiscountKind), "نوع تخفیف معتبر نیست.");
            }

            if (terms.MinimumSubtotal is < 0)
                throw new DomainValidationException(nameof(MinimumSubtotal), "حداقل مبلغ نمی‌تواند منفی باشد.");

            var serviceIds = (terms.ServiceIds ?? Array.Empty<Guid>()).Where(id => id != Guid.Empty).Distinct().ToList();
            if (Owner == PromotionOwner.Platform && serviceIds.Count > 0)
                throw new DomainValidationException(nameof(ServiceIds), "کمپین پلتفرم نمی‌تواند به خدمات خاصی محدود شود.");
            if (serviceIds.Count > MaxTargetedServices)
                throw new DomainValidationException(nameof(ServiceIds), $"حداکثر {MaxTargetedServices} خدمت قابل انتخاب است.");

            if (terms.DailyStartTime.HasValue != terms.DailyEndTime.HasValue)
                throw new DomainValidationException(nameof(DailyStartTime), "ساعت شروع و پایان بازه روزانه را هر دو وارد کنید.");
            if (terms.DailyStartTime.HasValue && terms.DailyStartTime.Value >= terms.DailyEndTime!.Value)
                throw new DomainValidationException(nameof(DailyStartTime), "ساعت شروع باید قبل از ساعت پایان باشد.");

            if (terms.EndsAt.HasValue && terms.EndsAt.Value <= terms.StartsAt)
                throw new DomainValidationException(nameof(EndsAt), "تاریخ پایان باید بعد از تاریخ شروع باشد.");
            if (terms.EndsAt.HasValue && terms.EndsAt.Value <= nowUtc)
                throw new DomainValidationException(nameof(EndsAt), "تاریخ پایان گذشته است.");

            if (terms.TotalUsageLimit is < 1)
                throw new DomainValidationException(nameof(TotalUsageLimit), "سقف کل استفاده باید حداقل ۱ باشد.");
            if (terms.PerCustomerLimit is < 1)
                throw new DomainValidationException(nameof(PerCustomerLimit), "سقف استفاده هر مشتری باید حداقل ۱ باشد.");

            Title = title;
            Description = description;
            Activation = terms.Activation;
            Code = code;
            DiscountKind = terms.DiscountKind;
            DiscountValue = terms.DiscountValue;
            MaxDiscountAmount = terms.DiscountKind == DiscountKind.Percentage ? terms.MaxDiscountAmount : null;
            MinimumSubtotal = terms.MinimumSubtotal is > 0 ? terms.MinimumSubtotal : null;
            NewCustomersOnly = terms.NewCustomersOnly;
            _serviceIds = serviceIds;
            DaysOfWeekMask = (terms.DaysOfWeek ?? Array.Empty<DayOfWeek>())
                .Aggregate(0, (mask, day) => mask | (1 << (int)day));
            DailyStartTime = terms.DailyStartTime;
            DailyEndTime = terms.DailyEndTime;
            StartsAt = DateTime.SpecifyKind(terms.StartsAt, DateTimeKind.Utc);
            EndsAt = terms.EndsAt.HasValue ? DateTime.SpecifyKind(terms.EndsAt.Value, DateTimeKind.Utc) : null;
            TotalUsageLimit = terms.TotalUsageLimit;
            PerCustomerLimit = terms.PerCustomerLimit;
        }

        private void EnsureNotEnded()
        {
            if (Status == PromotionStatus.Ended)
                throw Refused("PROMOTION_ENDED", "این تخفیف پایان یافته است.");
        }

        private static BusinessRuleViolationException Refused(string code, string message) =>
            new(nameof(Promotion), message, code);
    }

    /// <summary>Everything a salon or an admin sets on a promotion. Validated by <see cref="Promotion"/>.</summary>
    public sealed record PromotionTerms(
        string Title,
        string? Description,
        PromotionActivation Activation,
        string? Code,
        DiscountKind DiscountKind,
        decimal DiscountValue,
        decimal? MaxDiscountAmount,
        decimal? MinimumSubtotal,
        bool NewCustomersOnly,
        IReadOnlyCollection<Guid>? ServiceIds,
        IReadOnlyCollection<DayOfWeek>? DaysOfWeek,
        TimeOnly? DailyStartTime,
        TimeOnly? DailyEndTime,
        DateTime StartsAt,
        DateTime? EndsAt,
        int? TotalUsageLimit,
        int? PerCustomerLimit);

    /// <summary>One service line of the visit being priced, at its list price.</summary>
    public sealed record PricedLine(Guid ServiceId, decimal Amount);

    /// <summary>
    /// The visit a promotion is evaluated against. <paramref name="AppointmentStart"/> is the salon's wall clock;
    /// <paramref name="NowUtc"/> is the instant of booking.
    /// </summary>
    public sealed record PromotionContext(
        IReadOnlyList<PricedLine> Lines,
        DateTime AppointmentStart,
        DateTime NowUtc,
        bool IsNewCustomer)
    {
        public decimal Subtotal => Lines.Sum(l => l.Amount);
    }

    public sealed record PromotionEvaluation(bool IsEligible, decimal Discount, decimal EligibleSubtotal, string? Reason)
    {
        public static PromotionEvaluation Eligible(decimal discount, decimal eligibleSubtotal) =>
            new(true, discount, eligibleSubtotal, null);

        public static PromotionEvaluation NotEligible(string reason) => new(false, 0m, 0m, reason);
    }
}
