using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events;

/// <summary>
/// Domain event raised when a provider's location is updated
/// </summary>
public sealed record ProviderLocationUpdatedEvent : DomainEvent
{
    public ProviderId ProviderId { get; }
    public string ProviderName { get; }
    public BusinessAddress? PreviousAddress { get; }
    public BusinessAddress NewAddress { get; }
    public Coordinates? PreviousCoordinates { get; }
    public Coordinates NewCoordinates { get; }
    public LocationChangeType ChangeType { get; }
    public string? ChangeReason { get; }
    public DateTime EffectiveDate { get; }
    public string UpdatedByUserId { get; }
    public double DistanceMoved { get; }
    public IReadOnlyList<string> AffectedAppointmentIds { get; }
    public LocationChangeImpact Impact { get; }
    public bool RequiresClientNotification { get; }
    public string? ForwardingInstructions { get; }

    public ProviderLocationUpdatedEvent(
        ProviderId providerId,
        string providerName,
        BusinessAddress newAddress,
        Coordinates newCoordinates,
        LocationChangeType changeType,
        string updatedByUserId,
        DateTime effectiveDate,
        IReadOnlyList<string> affectedAppointmentIds,
        BusinessAddress? previousAddress = null,
        Coordinates? previousCoordinates = null,
        string? changeReason = null,
        string? forwardingInstructions = null) : base()
    {
        ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        ProviderName = !string.IsNullOrWhiteSpace(providerName)
            ? providerName
            : throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
        PreviousAddress = previousAddress;
        NewAddress = newAddress ?? throw new ArgumentNullException(nameof(newAddress));
        PreviousCoordinates = previousCoordinates;
        NewCoordinates = newCoordinates ?? throw new ArgumentNullException(nameof(newCoordinates));
        ChangeType = changeType;
        ChangeReason = changeReason;
        EffectiveDate = effectiveDate;
        UpdatedByUserId = !string.IsNullOrWhiteSpace(updatedByUserId)
            ? updatedByUserId
            : throw new ArgumentException("Updated by user ID cannot be null or empty", nameof(updatedByUserId));

        DistanceMoved = CalculateDistanceMoved();
        AffectedAppointmentIds = affectedAppointmentIds ?? Array.Empty<string>().ToList().AsReadOnly();
        Impact = CalculateImpact();
        RequiresClientNotification = ShouldNotifyClients();
        ForwardingInstructions = forwardingInstructions;
    }

    /// <summary>
    /// Check if this is a major location change (> 5km)
    /// </summary>
    public bool IsMajorLocationChange => DistanceMoved > 5.0;

    /// <summary>
    /// Check if this is a minor location change (< 1km)
    /// </summary>
    public bool IsMinorLocationChange => DistanceMoved < 1.0;

    /// <summary>
    /// Check if the change affects the same city
    /// </summary>
    public bool IsSameCity =>
        PreviousAddress?.City?.Equals(NewAddress.City, StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Check if the change affects the same postal code
    /// </summary>
    public bool IsSamePostalCode =>
        PreviousAddress?.PostalCode?.Equals(NewAddress.PostalCode, StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Check if this is an immediate change
    /// </summary>
    public bool IsImmediateChange => EffectiveDate <= DateTime.UtcNow.AddHours(1);

    /// <summary>
    /// Get advance notice period
    /// </summary>
    public TimeSpan AdvanceNotice => EffectiveDate > DateTime.UtcNow
        ? EffectiveDate - DateTime.UtcNow
        : TimeSpan.Zero;

    /// <summary>
    /// Calculate distance moved between locations
    /// </summary>
    private double CalculateDistanceMoved()
    {
        if (PreviousCoordinates == null) return 0.0;
        return PreviousCoordinates.DistanceToInKilometers(NewCoordinates);
    }

    /// <summary>
    /// Calculate the impact of the location change
    /// </summary>
    private LocationChangeImpact CalculateImpact()
    {
        var severity = GetChangeSeverity();
        var clientImpact = GetClientImpact();
        var businessImpact = GetBusinessImpact();
        var accessibilityImpact = GetAccessibilityImpact();

        return new LocationChangeImpact
        {
            Severity = severity,
            ClientImpact = clientImpact,
            BusinessImpact = businessImpact,
            AccessibilityImpact = accessibilityImpact,
            AffectedAppointmentsCount = AffectedAppointmentIds.Count,
            EstimatedClientRetention = CalculateClientRetention()
        };
    }

    /// <summary>
    /// Determine the severity of the location change
    /// </summary>
    private LocationChangeSeverity GetChangeSeverity()
    {
        if (ChangeType == LocationChangeType.NewBranch || ChangeType == LocationChangeType.AddressCorrection)
        {
            return LocationChangeSeverity.Low;
        }

        return DistanceMoved switch
        {
            > 50 => LocationChangeSeverity.Critical,
            > 20 => LocationChangeSeverity.High,
            > 5 => LocationChangeSeverity.Medium,
            > 1 => LocationChangeSeverity.Low,
            _ => LocationChangeSeverity.Minimal
        };
    }

    /// <summary>
    /// Assess client impact
    /// </summary>
    private ClientImpact GetClientImpact()
    {
        if (ChangeType == LocationChangeType.AddressCorrection) return ClientImpact.Minimal;

        var impactFactor = DistanceMoved switch
        {
            > 50 => 4.0,
            > 20 => 3.0,
            > 10 => 2.0,
            > 5 => 1.5,
            > 2 => 1.2,
            _ => 1.0
        };

        // Adjust for number of affected appointments
        if (AffectedAppointmentIds.Count > 20) impactFactor *= 1.5;
        if (AffectedAppointmentIds.Count > 50) impactFactor *= 2.0;

        return impactFactor switch
        {
            >= 8.0 => ClientImpact.Severe,
            >= 4.0 => ClientImpact.High,
            >= 2.0 => ClientImpact.Medium,
            >= 1.2 => ClientImpact.Low,
            _ => ClientImpact.Minimal
        };
    }

    /// <summary>
    /// Assess business impact
    /// </summary>
    private BusinessImpact GetBusinessImpact()
    {
        var baseImpact = ChangeType switch
        {
            LocationChangeType.Closure => BusinessImpact.Critical,
            LocationChangeType.Relocation => DistanceMoved > 10 ? BusinessImpact.High : BusinessImpact.Medium,
            LocationChangeType.Expansion => BusinessImpact.Low,
            LocationChangeType.NewBranch => BusinessImpact.Minimal,
            LocationChangeType.AddressCorrection => BusinessImpact.Minimal,
            LocationChangeType.Temporary => BusinessImpact.Medium,
            _ => BusinessImpact.Medium
        };

        // Adjust based on affected appointments
        if (AffectedAppointmentIds.Count > 50 && baseImpact < BusinessImpact.High)
        {
            baseImpact = BusinessImpact.High;
        }

        return baseImpact;
    }

    /// <summary>
    /// Assess accessibility impact
    /// </summary>
    private AccessibilityImpact GetAccessibilityImpact()
    {
        // This would be enhanced with real accessibility data
        // For now, we use distance and change type as proxies

        if (ChangeType == LocationChangeType.Closure) return AccessibilityImpact.Severe;

        return DistanceMoved switch
        {
            > 30 => AccessibilityImpact.High,
            > 10 => AccessibilityImpact.Medium,
            > 2 => AccessibilityImpact.Low,
            _ => AccessibilityImpact.Minimal
        };
    }

    /// <summary>
    /// Calculate estimated client retention percentage
    /// </summary>
    private decimal CalculateClientRetention()
    {
        // Simplified retention model based on distance and change type
        var baseRetention = ChangeType switch
        {
            LocationChangeType.Closure => 0.2m,
            LocationChangeType.Relocation => 0.7m,
            LocationChangeType.Expansion => 0.95m,
            LocationChangeType.NewBranch => 1.0m,
            LocationChangeType.AddressCorrection => 1.0m,
            LocationChangeType.Temporary => 0.85m,
            _ => 0.8m
        };

        // Adjust based on distance
        var distancePenalty = DistanceMoved switch
        {
            > 50 => 0.4m,
            > 20 => 0.2m,
            > 10 => 0.1m,
            > 5 => 0.05m,
            _ => 0m
        };

        return Math.Max(0.1m, baseRetention - distancePenalty);
    }

    /// <summary>
    /// Determine if clients should be notified
    /// </summary>
    private bool ShouldNotifyClients()
    {
        return ChangeType != LocationChangeType.AddressCorrection &&
               (DistanceMoved > 0.5 || AffectedAppointmentIds.Any());
    }

    /// <summary>
    /// Get recommended notification timeline
    /// </summary>
    public TimeSpan GetRecommendedNotificationTime()
    {
        if (IsImmediateChange) return TimeSpan.Zero;

        return Impact.Severity switch
        {
            LocationChangeSeverity.Critical => TimeSpan.FromDays(30),
            LocationChangeSeverity.High => TimeSpan.FromDays(14),
            LocationChangeSeverity.Medium => TimeSpan.FromDays(7),
            LocationChangeSeverity.Low => TimeSpan.FromDays(3),
            _ => TimeSpan.FromDays(1)
        };
    }

    /// <summary>
    /// Get required actions for this location change
    /// </summary>
    public IEnumerable<string> GetRequiredActions()
    {
        var actions = new List<string>();

        if (RequiresClientNotification)
        {
            actions.Add("Send location change notifications to affected clients");
        }

        if (AffectedAppointmentIds.Any())
        {
            actions.Add("Review and update appointment logistics");
            actions.Add("Provide updated directions to clients");
        }

        if (IsMajorLocationChange)
        {
            actions.Add("Update marketing materials with new location");
            actions.Add("Update online listings and maps");
            actions.Add("Plan relocation logistics");
        }

        if (ChangeType == LocationChangeType.Closure)
        {
            actions.Add("Arrange alternative service locations");
            actions.Add("Process appointment cancellations or transfers");
        }

        actions.Add("Update business location in system settings");

        return actions;
    }

    /// <summary>
    /// Get location change summary
    /// </summary>
    public string GetLocationChangeSummary()
    {
        var previousLocation = PreviousAddress?.ToString() ?? "Unknown";
        var newLocation = NewAddress.ToString();
        var distance = DistanceMoved > 0 ? $" ({DistanceMoved:F1}km away)" : "";

        return $"Provider '{ProviderName}' location updated from {previousLocation} to {newLocation}{distance}";
    }
}

/// <summary>
/// Types of location changes
/// </summary>
public enum LocationChangeType
{
    Relocation = 1,      // Moving to a new location
    Expansion = 2,       // Adding additional location
    NewBranch = 3,       // Opening new branch
    Closure = 4,         // Closing current location
    AddressCorrection = 5, // Correcting address details
    Temporary = 6        // Temporary location change
}

/// <summary>
/// Severity levels for location changes
/// </summary>
public enum LocationChangeSeverity
{
    Minimal = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

/// <summary>
/// Client impact levels
/// </summary>
public enum ClientImpact
{
    Minimal = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Severe = 5
}



/// <summary>
/// Accessibility impact levels
/// </summary>
public enum AccessibilityImpact
{
    Minimal = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Severe = 5
}

/// <summary>
/// Comprehensive location change impact assessment
/// </summary>
public class LocationChangeImpact
{
    public LocationChangeSeverity Severity { get; init; }
    public ClientImpact ClientImpact { get; init; }
    public BusinessImpact BusinessImpact { get; init; }
    public AccessibilityImpact AccessibilityImpact { get; init; }
    public int AffectedAppointmentsCount { get; init; }
    public decimal EstimatedClientRetention { get; init; }
}