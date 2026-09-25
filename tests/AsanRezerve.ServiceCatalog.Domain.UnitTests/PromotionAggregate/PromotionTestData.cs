using AsanRezerve.ServiceCatalog.Domain.Aggregates.PromotionAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using DayOfWeek = AsanRezerve.ServiceCatalog.Domain.Enums.DayOfWeek;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests.PromotionAggregate;

/// <summary>Builders for promotion tests. Every clock value is explicit; nothing reads the wall clock.</summary>
internal static class PromotionTestData
{
    /// <summary>A Thursday. 06:30 UTC is 10:00 at the salon.</summary>
    public static readonly DateTime Now = new(2026, 10, 1, 6, 30, 0, DateTimeKind.Utc);

    /// <summary>Salon wall-clock time of an appointment: Saturday 3 Oct 2026, 11:00.</summary>
    public static readonly DateTime SaturdayEleven = new(2026, 10, 3, 11, 0, 0, DateTimeKind.Unspecified);

    /// <summary>Thursday 8 Oct 2026, 11:00 at the salon.</summary>
    public static readonly DateTime ThursdayEleven = new(2026, 10, 8, 11, 0, 0, DateTimeKind.Unspecified);

    public static readonly Guid Admin = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Owner = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    public static PromotionTerms Terms(
        string title = "تخفیف پاییزه",
        PromotionActivation activation = PromotionActivation.Automatic,
        string? code = null,
        DiscountKind kind = DiscountKind.Percentage,
        decimal value = 20m,
        decimal? max = null,
        decimal? minimumSubtotal = null,
        bool newCustomersOnly = false,
        IReadOnlyCollection<Guid>? serviceIds = null,
        IReadOnlyCollection<DayOfWeek>? days = null,
        TimeOnly? dailyStart = null,
        TimeOnly? dailyEnd = null,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        int? totalLimit = null,
        int? perCustomerLimit = null,
        string? description = null) =>
        new(title, description, activation, code, kind, value, max, minimumSubtotal, newCustomersOnly,
            serviceIds, days, dailyStart, dailyEnd, startsAt ?? Now.AddDays(-1), endsAt, totalLimit, perCustomerLimit);

    public static Promotion ProviderPromotion(PromotionTerms? terms = null, ProviderId? providerId = null) =>
        Promotion.CreateForProvider(providerId ?? ProviderId.New(), terms ?? Terms(), Owner, Now);

    public static Promotion Campaign(PromotionTerms? terms = null) =>
        Promotion.CreatePlatformCampaign(terms ?? Terms(title: "کمپین نوروز"), Admin, Now);

    public static PromotionContext Context(
        decimal amount = 1_000_000m,
        DateTime? appointment = null,
        bool newCustomer = false,
        DateTime? now = null,
        params PricedLine[] lines) =>
        new(lines.Length > 0 ? lines : new[] { new PricedLine(Guid.NewGuid(), amount) },
            appointment ?? SaturdayEleven,
            now ?? Now,
            newCustomer);
}
