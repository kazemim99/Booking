using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Domain.Events;

/// <summary>
/// Domain event raised when a service price is changed
/// </summary>
public sealed record ServicePriceChangedEvent : DomainEvent
{
    public ServiceId ServiceId { get; }
    public ProviderId ProviderId { get; }
    public string ServiceName { get; }
    public Money PreviousPrice { get; }
    public Money NewPrice { get; }
    public Money PriceDifference { get; }
    public decimal PercentageChange { get; }
    public PriceChangeReason ChangeReason { get; }
    public string? ChangeJustification { get; }
    public DateTime EffectiveDate { get; }
    public string ChangedByUserId { get; }
    public bool AffectsExistingBookings { get; }
    public IReadOnlyList<string> AffectedBookingIds { get; }
    public PriceChangeImpact Impact { get; }
    public bool RequiresClientNotification { get; }

    public ServicePriceChangedEvent(
        ServiceId serviceId,
        ProviderId providerId,
        string serviceName,
        Money previousPrice,
        Money newPrice,
        PriceChangeReason changeReason,
        string changedByUserId,
        DateTime effectiveDate,
        IReadOnlyList<string> affectedBookingIds,
        string? changeJustification = null) : base()
    {
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        ServiceName = !string.IsNullOrWhiteSpace(serviceName)
            ? serviceName
            : throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));
        PreviousPrice = previousPrice ?? throw new ArgumentNullException(nameof(previousPrice));
        NewPrice = newPrice ?? throw new ArgumentNullException(nameof(newPrice));

        if (previousPrice.Currency != newPrice.Currency)
        {
            throw new ArgumentException("Previous and new prices must be in the same currency");
        }

        PriceDifference = newPrice.Subtract(previousPrice);
        PercentageChange = previousPrice.Amount != 0
            ? Math.Round(((newPrice.Amount - previousPrice.Amount) / previousPrice.Amount) * 100, 2)
            : 0;

        ChangeReason = changeReason;
        ChangeJustification = changeJustification;
        EffectiveDate = effectiveDate;
        ChangedByUserId = !string.IsNullOrWhiteSpace(changedByUserId)
            ? changedByUserId
            : throw new ArgumentException("Changed by user ID cannot be null or empty", nameof(changedByUserId));

        AffectedBookingIds = affectedBookingIds ?? Array.Empty<string>().ToList().AsReadOnly();
        AffectsExistingBookings = AffectedBookingIds.Any();

        Impact = CalculateImpact();
        RequiresClientNotification = ShouldNotifyClients();
    }

    /// <summary>
    /// Check if this is a price increase
    /// </summary>
    public bool IsPriceIncrease => PriceDifference.Amount > 0;

    /// <summary>
    /// Check if this is a price decrease
    /// </summary>
    public bool IsPriceDecrease => PriceDifference.Amount < 0;

    /// <summary>
    /// Check if this is a significant price change (more than 10%)
    /// </summary>
    public bool IsSignificantChange => Math.Abs(PercentageChange) >= 10;

    /// <summary>
    /// Check if this change takes effect immediately
    /// </summary>
    public bool IsImmediateChange => EffectiveDate <= DateTime.UtcNow.AddHours(1);

    /// <summary>
    /// Get the advance notice period for this change
    /// </summary>
    public TimeSpan AdvanceNotice => EffectiveDate > DateTime.UtcNow
        ? EffectiveDate - DateTime.UtcNow
        : TimeSpan.Zero;

    /// <summary>
    /// Calculate the impact of the price change
    /// </summary>
    private PriceChangeImpact CalculateImpact()
    {
        var severity = GetChangeSeverity();
        var customerImpact = GetCustomerImpact();
        var businessImpact = GetBusinessImpact();

        return new PriceChangeImpact
        {
            Severity = severity,
            CustomerImpact = customerImpact,
            BusinessImpact = businessImpact,
            AffectedBookingsCount = AffectedBookingIds.Count,
            EstimatedRevenueImpact = CalculateRevenueImpact()
        };
    }

    /// <summary>
    /// Determine the severity of the price change
    /// </summary>
    private PriceChangeSeverity GetChangeSeverity()
    {
        var absPercentage = Math.Abs(PercentageChange);

        return absPercentage switch
        {
            >= 50 => PriceChangeSeverity.Extreme,
            >= 25 => PriceChangeSeverity.Major,
            >= 10 => PriceChangeSeverity.Significant,
            >= 5 => PriceChangeSeverity.Moderate,
            > 0 => PriceChangeSeverity.Minor,
            _ => PriceChangeSeverity.None
        };
    }

    /// <summary>
    /// Assess customer impact
    /// </summary>
    private CustomerImpact GetCustomerImpact()
    {
        if (IsPriceDecrease) return CustomerImpact.Positive;

        return Math.Abs(PercentageChange) switch
        {
            >= 25 => CustomerImpact.High,
            >= 10 => CustomerImpact.Medium,
            >= 5 => CustomerImpact.Low,
            _ => CustomerImpact.Minimal
        };
    }

    /// <summary>
    /// Assess business impact
    /// </summary>
    private BusinessImpact GetBusinessImpact()
    {
        var bookingImpactFactor = AffectedBookingIds.Count switch
        {
            > 50 => 2.0m,
            > 20 => 1.5m,
            > 10 => 1.2m,
            > 0 => 1.1m,
            _ => 1.0m
        };

        var adjustedPercentage = Math.Abs(PercentageChange) * bookingImpactFactor;

        return adjustedPercentage switch
        {
            >= 30 => BusinessImpact.Critical,
            >= 15 => BusinessImpact.High,
            >= 8 => BusinessImpact.Medium,
            >= 3 => BusinessImpact.Low,
            _ => BusinessImpact.Minimal
        };
    }

    /// <summary>
    /// Calculate estimated revenue impact
    /// </summary>
    private decimal CalculateRevenueImpact()
    {
        // Simplified calculation - in real implementation, this would consider
        // historical booking volume, demand elasticity, etc.
        return PriceDifference.Amount * AffectedBookingIds.Count;
    }

    /// <summary>
    /// Determine if clients should be notified
    /// </summary>
    private bool ShouldNotifyClients()
    {
        // Notify if price increases, significant changes, or affects existing bookings
        return IsPriceIncrease || IsSignificantChange || AffectsExistingBookings;
    }

    /// <summary>
    /// Get recommended notification timing
    /// </summary>
    public TimeSpan GetRecommendedNotificationTiming()
    {
        if (IsImmediateChange) return TimeSpan.Zero;

        return Impact.Severity switch
        {
            PriceChangeSeverity.Extreme => TimeSpan.FromDays(14),
            PriceChangeSeverity.Major => TimeSpan.FromDays(7),
            PriceChangeSeverity.Significant => TimeSpan.FromDays(3),
            PriceChangeSeverity.Moderate => TimeSpan.FromDays(1),
            _ => TimeSpan.FromHours(6)
        };
    }

    /// <summary>
    /// Get required approvals for this price change
    /// </summary>
    public IEnumerable<string> GetRequiredApprovals()
    {
        var approvals = new List<string>();

        if (Impact.Severity >= PriceChangeSeverity.Major)
        {
            approvals.Add("Management Approval");
        }

        if (Impact.BusinessImpact >= BusinessImpact.High)
        {
            approvals.Add("Financial Controller Approval");
        }

        if (AffectedBookingIds.Count > 20)
        {
            approvals.Add("Customer Service Manager Approval");
        }

        if (ChangeReason == PriceChangeReason.Emergency)
        {
            approvals.Add("Emergency Change Authorization");
        }

        return approvals;
    }

    /// <summary>
    /// Get formatted price change summary
    /// </summary>
    public string GetPriceChangeSummary()
    {
        var direction = IsPriceIncrease ? "increased" : "decreased";
        var formattedOld = PreviousPrice.ToString();
        var formattedNew = NewPrice.ToString();
        var percentage = Math.Abs(PercentageChange);

        return $"Service '{ServiceName}' price {direction} from {formattedOld} to {formattedNew} ({percentage:F1}% change)";
    }
}

/// <summary>
/// Reasons for price changes
/// </summary>
public enum PriceChangeReason
{
    MarketAdjustment = 1,
    CostIncrease = 2,
    DemandBased = 3,
    Promotional = 4,
    Seasonal = 5,
    Competitive = 6,
    QualityImprovement = 7,
    Emergency = 8,
    Regulatory = 9,
    Strategic = 10
}

/// <summary>
/// Severity levels for price changes
/// </summary>
public enum PriceChangeSeverity
{
    None = 0,
    Minor = 1,
    Moderate = 2,
    Significant = 3,
    Major = 4,
    Extreme = 5
}

/// <summary>
/// Customer impact assessment
/// </summary>
public enum CustomerImpact
{
    Positive = 1,  // Price decrease
    Minimal = 2,   // < 5% increase
    Low = 3,       // 5-10% increase
    Medium = 4,    // 10-25% increase
    High = 5       // > 25% increase
}



/// <summary>
/// Comprehensive impact assessment
/// </summary>
public class PriceChangeImpact
{
    public PriceChangeSeverity Severity { get; init; }
    public CustomerImpact CustomerImpact { get; init; }
    public BusinessImpact BusinessImpact { get; init; }
    public int AffectedBookingsCount { get; init; }
    public decimal EstimatedRevenueImpact { get; init; }
}