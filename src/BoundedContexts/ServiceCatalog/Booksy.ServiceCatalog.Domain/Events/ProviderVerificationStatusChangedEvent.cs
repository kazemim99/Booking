using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events;

/// <summary>
/// Domain event raised when a provider's verification status is changed
/// </summary>
public sealed record ProviderVerificationStatusChangedEvent : DomainEvent
{
    public ProviderId ProviderId { get; }
    public string ProviderName { get; }
    public ProviderVerificationStatus PreviousStatus { get; }
    public ProviderVerificationStatus NewStatus { get; }
    public VerificationChangeReason ChangeReason { get; }
    public string? ChangeDetails { get; }
    public string VerifiedByUserId { get; }
    public DateTime VerificationDate { get; }
    public string? VerificationReference { get; }
    public IReadOnlyList<VerificationDocument> VerificationDocuments { get; }
    public DateTime? ExpirationDate { get; }
    public VerificationLevel VerificationLevel { get; }
    public VerificationImpact Impact { get; }
    public bool AffectsServiceAvailability { get; }
    public IReadOnlyList<ServiceId> AffectedServices { get; }
    public bool RequiresClientNotification { get; }
    public string? ComplianceNotes { get; }

    public ProviderVerificationStatusChangedEvent(
        ProviderId providerId,
        string providerName,
        ProviderVerificationStatus previousStatus,
        ProviderVerificationStatus newStatus,
        VerificationChangeReason changeReason,
        string verifiedByUserId,
        VerificationLevel verificationLevel,
        IReadOnlyList<ServiceId> affectedServices,
        string? changeDetails = null,
        string? verificationReference = null,
        IReadOnlyList<VerificationDocument>? verificationDocuments = null,
        DateTime? expirationDate = null,
        string? complianceNotes = null) : base()
    {
        ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        ProviderName = !string.IsNullOrWhiteSpace(providerName)
            ? providerName
            : throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangeReason = changeReason;
        ChangeDetails = changeDetails;
        VerifiedByUserId = !string.IsNullOrWhiteSpace(verifiedByUserId)
            ? verifiedByUserId
            : throw new ArgumentException("Verified by user ID cannot be null or empty", nameof(verifiedByUserId));
        VerificationDate = DateTime.UtcNow;
        VerificationReference = verificationReference;
        VerificationDocuments = verificationDocuments ?? Array.Empty<VerificationDocument>().ToList().AsReadOnly();
        ExpirationDate = expirationDate;
        VerificationLevel = verificationLevel;
        AffectedServices = affectedServices ?? Array.Empty<ServiceId>().ToList().AsReadOnly();

        Impact = CalculateImpact();
        AffectsServiceAvailability = DetermineServiceAvailabilityImpact();
        RequiresClientNotification = ShouldNotifyClients();
        ComplianceNotes = complianceNotes;
    }

    /// <summary>
    /// Check if this is a verification upgrade
    /// </summary>
    public bool IsVerificationUpgrade => (int)NewStatus > (int)PreviousStatus;

    /// <summary>
    /// Check if this is a verification downgrade
    /// </summary>
    public bool IsVerificationDowngrade => (int)NewStatus < (int)PreviousStatus;

    /// <summary>
    /// Check if provider is now fully verified
    /// </summary>
    public bool IsNowFullyVerified => NewStatus == ProviderVerificationStatus.Verified;

    /// <summary>
    /// Check if provider verification was revoked
    /// </summary>
    public bool IsVerificationRevoked => NewStatus == ProviderVerificationStatus.Revoked;

    /// <summary>
    /// Check if provider verification is pending
    /// </summary>
    public bool IsVerificationPending => NewStatus == ProviderVerificationStatus.Pending;

    /// <summary>
    /// Check if verification has an expiration date
    /// </summary>
    public bool HasExpirationDate => ExpirationDate.HasValue;

    /// <summary>
    /// Check if verification is expiring soon (within 30 days)
    /// </summary>
    public bool IsExpiringSoon => ExpirationDate.HasValue &&
                                  ExpirationDate.Value <= DateTime.UtcNow.AddDays(30);

    /// <summary>
    /// Calculate the impact of the verification status change
    /// </summary>
    private VerificationImpact CalculateImpact()
    {
        var trustImpact = GetTrustImpact();
        var businessImpact = GetBusinessImpact();
        var complianceImpact = GetComplianceImpact();
        var marketingImpact = GetMarketingImpact();

        return new VerificationImpact
        {
            TrustImpact = trustImpact,
            BusinessImpact = businessImpact,
            ComplianceImpact = complianceImpact,
            MarketingImpact = marketingImpact,
            AffectedServicesCount = AffectedServices.Count,
            CustomerConfidenceLevel = CalculateCustomerConfidence()
        };
    }

    /// <summary>
    /// Assess trust impact
    /// </summary>
    private TrustImpact GetTrustImpact()
    {
        return (PreviousStatus, NewStatus) switch
        {
            (_, ProviderVerificationStatus.Verified) => TrustImpact.High,
            (ProviderVerificationStatus.Verified, _) when IsVerificationDowngrade => TrustImpact.Negative,
            (_, ProviderVerificationStatus.Revoked) => TrustImpact.Severe,
            (ProviderVerificationStatus.Revoked, _) => TrustImpact.High,
            (_, ProviderVerificationStatus.Suspended) => TrustImpact.Negative,
            (ProviderVerificationStatus.Suspended, _) => TrustImpact.Medium,
            _ => TrustImpact.Low
        };
    }

    /// <summary>
    /// Assess business impact
    /// </summary>
    private BusinessImpact GetBusinessImpact()
    {
        var baseImpact = NewStatus switch
        {
            ProviderVerificationStatus.Verified => BusinessImpact.Positive,
            ProviderVerificationStatus.Revoked => BusinessImpact.Critical,
            ProviderVerificationStatus.Suspended => BusinessImpact.High,
            ProviderVerificationStatus.Rejected => BusinessImpact.Medium,
            ProviderVerificationStatus.Pending => BusinessImpact.Low,
            _ => BusinessImpact.Minimal
        };

        // Adjust based on affected services
        if (AffectedServices.Count > 10 && baseImpact == BusinessImpact.Medium)
            baseImpact = BusinessImpact.High;

        return baseImpact;
    }

    /// <summary>
    /// Assess compliance impact
    /// </summary>
    private ComplianceImpact GetComplianceImpact()
    {
        return NewStatus switch
        {
            ProviderVerificationStatus.Verified => ComplianceImpact.Compliant,
            ProviderVerificationStatus.Revoked => ComplianceImpact.NonCompliant,
            ProviderVerificationStatus.Suspended => ComplianceImpact.AtRisk,
            ProviderVerificationStatus.Rejected => ComplianceImpact.NonCompliant,
            ProviderVerificationStatus.Pending => ComplianceImpact.Unknown,
            _ => ComplianceImpact.Unknown
        };
    }

    /// <summary>
    /// Assess marketing impact
    /// </summary>
    private MarketingImpact GetMarketingImpact()
    {
        return NewStatus switch
        {
            ProviderVerificationStatus.Verified => MarketingImpact.Positive,
            ProviderVerificationStatus.Revoked => MarketingImpact.Negative,
            ProviderVerificationStatus.Suspended => MarketingImpact.Negative,
            ProviderVerificationStatus.Rejected => MarketingImpact.Neutral,
            _ => MarketingImpact.Neutral
        };
    }

    /// <summary>
    /// Calculate customer confidence level
    /// </summary>
    private decimal CalculateCustomerConfidence()
    {
        var baseConfidence = NewStatus switch
        {
            ProviderVerificationStatus.Verified => 0.95m,
            ProviderVerificationStatus.Pending => 0.6m,
            ProviderVerificationStatus.Rejected => 0.3m,
            ProviderVerificationStatus.Suspended => 0.2m,
            ProviderVerificationStatus.Revoked => 0.1m,
            _ => 0.5m
        };

        // Adjust based on verification level
        var levelMultiplier = VerificationLevel switch
        {
            VerificationLevel.Basic => 1.0m,
            VerificationLevel.Standard => 1.1m,
            VerificationLevel.Premium => 1.2m,
            VerificationLevel.Enterprise => 1.3m,
            _ => 1.0m
        };

        return Math.Min(1.0m, baseConfidence * levelMultiplier);
    }

    /// <summary>
    /// Determine if service availability is affected
    /// </summary>
    private bool DetermineServiceAvailabilityImpact()
    {
        return NewStatus == ProviderVerificationStatus.Revoked ||
               NewStatus == ProviderVerificationStatus.Suspended ||
               (PreviousStatus == ProviderVerificationStatus.Verified && IsVerificationDowngrade);
    }

    /// <summary>
    /// Determine if clients should be notified
    /// </summary>
    private bool ShouldNotifyClients()
    {
        return NewStatus switch
        {
            ProviderVerificationStatus.Verified => true,
            ProviderVerificationStatus.Revoked => true,
            ProviderVerificationStatus.Suspended => true,
            _ => false
        };
    }

    /// <summary>
    /// Get required follow-up actions
    /// </summary>
    public IEnumerable<string> GetRequiredActions()
    {
        var actions = new List<string>();

        switch (NewStatus)
        {
            case ProviderVerificationStatus.Verified:
                actions.Add("Update provider badge/status display");
                actions.Add("Enable verified provider features");
                if (IsVerificationUpgrade)
                    actions.Add("Send verification success notification");
                break;

            case ProviderVerificationStatus.Revoked:
                actions.Add("Disable provider services");
                actions.Add("Cancel pending appointments");
                actions.Add("Send revocation notification");
                actions.Add("Remove verification badges");
                break;

            case ProviderVerificationStatus.Suspended:
                actions.Add("Temporarily disable new bookings");
                actions.Add("Review existing appointments");
                actions.Add("Send suspension notification");
                break;

            case ProviderVerificationStatus.Pending:
                actions.Add("Schedule verification review");
                actions.Add("Request additional documentation if needed");
                break;

            case ProviderVerificationStatus.Rejected:
                actions.Add("Send rejection notification with reasons");
                actions.Add("Provide reapplication guidelines");
                break;
        }

        if (AffectsServiceAvailability)
        {
            actions.Add("Update service availability status");
            actions.Add("Notify affected clients");
        }

        if (HasExpirationDate)
        {
            actions.Add("Schedule verification renewal reminder");
        }

        return actions;
    }

    /// <summary>
    /// Get verification status summary
    /// </summary>
    public string GetVerificationSummary()
    {
        var direction = IsVerificationUpgrade ? "upgraded" :
                       IsVerificationDowngrade ? "downgraded" : "changed";

        return $"Provider '{ProviderName}' verification status {direction} from {PreviousStatus} to {NewStatus}";
    }

    /// <summary>
    /// Get customer-facing message
    /// </summary>
    public string GetCustomerMessage()
    {
        return NewStatus switch
        {
            ProviderVerificationStatus.Verified => "This provider has been verified and meets our quality standards.",
            ProviderVerificationStatus.Suspended => "This provider's verification is temporarily suspended. Please check back later.",
            ProviderVerificationStatus.Revoked => "This provider's verification has been revoked. Services may not be available.",
            ProviderVerificationStatus.Pending => "This provider's verification is under review.",
            ProviderVerificationStatus.Rejected => "This provider has not completed verification.",
            _ => "Verification status has been updated."
        };
    }
}

/// <summary>
/// Provider verification status
/// </summary>
public enum ProviderVerificationStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3,
    Suspended = 4,
    Revoked = 5
}

/// <summary>
/// Verification change reasons
/// </summary>
public enum VerificationChangeReason
{
    InitialVerification = 1,
    DocumentsUpdated = 2,
    ComplianceReview = 3,
    CustomerComplaint = 4,
    QualityIssue = 5,
    LegalRequirement = 6,
    PolicyViolation = 7,
    SystemReview = 8,
    ProviderRequest = 9,
    Renewal = 10
}

/// <summary>
/// Verification levels
/// </summary>
public enum VerificationLevel
{
    Basic = 1,
    Standard = 2,
    Premium = 3,
    Enterprise = 4
}

/// <summary>
/// Trust impact levels
/// </summary>
public enum TrustImpact
{
    Severe = 1,    // Major trust loss
    Negative = 2,  // Trust decrease
    Low = 3,       // Minimal impact
    Medium = 4,    // Some trust gain
    High = 5       // Significant trust gain
}

/// <summary>
/// Business impact levels
/// </summary>
public enum BusinessImpact
{
    Critical = 1,  // Severe negative impact
    High = 2,      // Major negative impact
    Medium = 3,    // Moderate negative impact
    Low = 4,       // Minor negative impact
    Minimal = 5,   // No significant impact
    Positive = 6   // Positive business impact
}

/// <summary>
/// Compliance impact levels
/// </summary>
public enum ComplianceImpact
{
    NonCompliant = 1,
    AtRisk = 2,
    Unknown = 3,
    Compliant = 4
}

/// <summary>
/// Marketing impact levels
/// </summary>
public enum MarketingImpact
{
    Negative = 1,
    Neutral = 2,
    Positive = 3
}

/// <summary>
/// Verification document information
/// </summary>
public class VerificationDocument
{
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public DateTime SubmissionDate { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public bool IsValid { get; init; }
}

/// <summary>
/// Comprehensive verification impact assessment
/// </summary>
public class VerificationImpact
{
    public TrustImpact TrustImpact { get; init; }
    public BusinessImpact BusinessImpact { get; init; }
    public ComplianceImpact ComplianceImpact { get; init; }
    public MarketingImpact MarketingImpact { get; init; }
    public int AffectedServicesCount { get; init; }
    public decimal CustomerConfidenceLevel { get; init; }
}