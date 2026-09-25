using System.Globalization;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Policies;
using AsanRezerve.ServiceCatalog.Domain.Repositories;

namespace AsanRezerve.ServiceCatalog.Application.Promotions
{
    // Contracts for openspec/changes/add-discounts-and-campaigns. Enums travel as their names ("Percentage",
    // "Automatic", ...) and are parsed case-insensitively; times of day as "HH:mm"; instants as UTC ISO-8601.

    /// <summary>What a salon or an admin submits to create or edit a promotion.</summary>
    public sealed record PromotionTermsInput(
        string? Title,
        string? Description,
        string? Activation,
        string? Code,
        string? DiscountKind,
        decimal DiscountValue,
        decimal? MaxDiscountAmount,
        decimal? MinimumSubtotal,
        bool NewCustomersOnly,
        IReadOnlyList<Guid>? ServiceIds,
        IReadOnlyList<int>? DaysOfWeek,
        string? DailyStartTime,
        string? DailyEndTime,
        DateTime? StartsAt,
        DateTime? EndsAt,
        int? TotalUsageLimit,
        int? PerCustomerLimit)
    {
        /// <summary>
        /// Parses the request into domain terms. Shape errors (unknown enum names, malformed times) are refused here in
        /// Persian; every business rule is the aggregate's (design: the domain is the single validator).
        /// </summary>
        public PromotionTerms ToTerms(DateTime nowUtc) => new(
            Title ?? string.Empty,
            Description,
            ParseEnum<PromotionActivation>(Activation, nameof(Activation), "نوع فعال‌سازی تخفیف معتبر نیست."),
            Code,
            ParseEnum<DiscountKind>(DiscountKind, nameof(DiscountKind), "نوع تخفیف معتبر نیست."),
            DiscountValue,
            MaxDiscountAmount,
            MinimumSubtotal,
            NewCustomersOnly,
            ServiceIds,
            ParseDays(DaysOfWeek),
            ParseTime(DailyStartTime, nameof(DailyStartTime)),
            ParseTime(DailyEndTime, nameof(DailyEndTime)),
            AsUtc(StartsAt) ?? nowUtc,
            AsUtc(EndsAt),
            TotalUsageLimit,
            PerCustomerLimit);

        private static TEnum ParseEnum<TEnum>(string? value, string property, string message) where TEnum : struct, Enum
        {
            if (!string.IsNullOrWhiteSpace(value)
                && Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed)
                && Enum.IsDefined(parsed)
                && !int.TryParse(value, out _))
                return parsed;

            throw new DomainValidationException(property, message);
        }

        private static IReadOnlyCollection<DayOfWeek>? ParseDays(IReadOnlyList<int>? days)
        {
            if (days is null)
                return null;
            if (days.Any(d => d is < 0 or > 6))
                throw new DomainValidationException(nameof(DaysOfWeek), "روز هفته معتبر نیست.");
            return days.Distinct().Select(d => (DayOfWeek)d).ToList();
        }

        private static TimeOnly? ParseTime(string? value, string property)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            if (TimeOnly.TryParseExact(value.Trim(), new[] { "HH:mm", "HH:mm:ss" }, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var time))
                return time;
            throw new DomainValidationException(property, "ساعت باید به شکل HH:mm باشد.");
        }

        // Instants are UTC throughout the system; an unmarked value is read as UTC, a local one converted.
        private static DateTime? AsUtc(DateTime? value) => value switch
        {
            null => null,
            { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
            { } other => DateTime.SpecifyKind(other, DateTimeKind.Utc),
        };
    }

    public sealed record PromotionDto(
        Guid Id,
        string Owner,
        Guid? ProviderId,
        string? ProviderName,
        string Title,
        string? Description,
        string Activation,
        string? Code,
        string DiscountKind,
        decimal DiscountValue,
        decimal? MaxDiscountAmount,
        decimal? MinimumSubtotal,
        bool NewCustomersOnly,
        IReadOnlyList<Guid> ServiceIds,
        IReadOnlyList<int> DaysOfWeek,
        string? DailyStartTime,
        string? DailyEndTime,
        DateTime StartsAt,
        DateTime? EndsAt,
        int? TotalUsageLimit,
        int? PerCustomerLimit,
        string Status,
        string State,
        bool PausedByPlatform,
        int Uses,
        decimal TotalDiscount,
        int? JoinedSalons,
        string Currency,
        DateTime CreatedAt)
    {
        public static PromotionDto From(
            Promotion p, DateTime nowUtc, PromotionUsage? usage = null, int? joinedSalons = null, string? providerName = null) =>
            new(p.Id, p.Owner.ToString(), p.ProviderId?.Value, providerName, p.Title, p.Description,
                p.Activation.ToString(), p.Code, p.DiscountKind.ToString(), p.DiscountValue, p.MaxDiscountAmount,
                p.MinimumSubtotal, p.NewCustomersOnly, p.ServiceIds.ToList(),
                p.DaysOfWeek.Select(d => (int)d).OrderBy(d => d).ToList(),
                p.DailyStartTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                p.DailyEndTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                p.StartsAt, p.EndsAt, p.TotalUsageLimit, p.PerCustomerLimit, p.Status.ToString(),
                p.StateAt(nowUtc).ToString(), p.PausedByPlatform, usage?.Uses ?? p.RedemptionCount,
                usage?.TotalDiscount ?? 0m, joinedSalons, p.Currency, p.CreatedAt);
    }

    /// <summary>A platform campaign as a salon sees it: its terms and whether this salon is in.</summary>
    public sealed record CampaignForProviderDto(PromotionDto Campaign, bool IsJoined, DateTime? JoinedAt);

    public sealed record CampaignParticipantDto(Guid ProviderId, string? ProviderName, DateTime JoinedAt);

    public sealed record PromotionDetailsDto(PromotionDto Promotion, IReadOnlyList<CampaignParticipantDto> Participants);

    public sealed record PromotionPageDto(IReadOnlyList<PromotionDto> Items, int TotalCount, int Page, int PageSize);

    public sealed record AppliedDiscountDto(Guid PromotionId, string Title, string? Code, string Owner, decimal Amount)
    {
        public static AppliedDiscountDto? From(AppliedDiscount? d) =>
            d is null ? null : new(d.PromotionId, d.Title, d.Code, d.Owner.ToString(), d.Amount);
    }

    public sealed record PriceQuoteDto(
        decimal Subtotal,
        decimal Discount,
        decimal Total,
        string Currency,
        AppliedDiscountDto? AppliedDiscount,
        string CodeOutcome,
        string? CodeMessage)
    {
        public static PriceQuoteDto From(PriceQuote q) =>
            new(q.Subtotal, q.Discount, q.Total, q.Currency, AppliedDiscountDto.From(q.Applied),
                q.CodeOutcome.ToString(), q.CodeMessage);
    }

    /// <summary>
    /// An automatic offer on the public salon page. Never a code. Conditions travel as data so each client renders
    /// them in its own words; <see cref="ServiceIds"/> empty means every service.
    /// </summary>
    public sealed record PublicOfferDto(
        Guid Id,
        string Title,
        string? Description,
        string Owner,
        string DiscountKind,
        decimal DiscountValue,
        decimal? MaxDiscountAmount,
        decimal? MinimumSubtotal,
        bool NewCustomersOnly,
        IReadOnlyList<Guid> ServiceIds,
        IReadOnlyList<int> DaysOfWeek,
        string? DailyStartTime,
        string? DailyEndTime,
        DateTime? EndsAt)
    {
        public static PublicOfferDto From(Promotion p) =>
            new(p.Id, p.Title, p.Description, p.Owner.ToString(), p.DiscountKind.ToString(), p.DiscountValue,
                p.MaxDiscountAmount, p.MinimumSubtotal, p.NewCustomersOnly, p.ServiceIds.ToList(),
                p.DaysOfWeek.Select(d => (int)d).OrderBy(d => d).ToList(),
                p.DailyStartTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                p.DailyEndTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                p.EndsAt);
    }

    /// <summary>Lifecycle actions shared by salon and admin endpoints.</summary>
    public enum PromotionLifecycleAction
    {
        Pause,
        Resume,
        End
    }
}
