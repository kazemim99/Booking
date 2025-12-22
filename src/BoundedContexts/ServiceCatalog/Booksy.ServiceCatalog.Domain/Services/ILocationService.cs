using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Services;

/// <summary>
/// Domain service for geolocation operations and distance calculations
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Find providers within a specified radius of a location
    /// </summary>
    /// <param name="centerLocation">The center point for the search</param>
    /// <param name="radiusInKilometers">Search radius in kilometers</param>
    /// <param name="serviceCategory">Optional service category filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of providers with their distances</returns>
    Task<IEnumerable<ProviderLocationResult>> FindProvidersNearLocationAsync(
        Coordinates centerLocation,
        double radiusInKilometers,
        ServiceCategory? serviceCategory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate travel time between two locations
    /// </summary>
    /// <param name="origin">Starting location</param>
    /// <param name="destination">Destination location</param>
    /// <param name="travelMode">Mode of transportation</param>
    /// <param name="departureTime">Optional departure time for traffic considerations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Travel time and distance information</returns>
    Task<TravelTimeResult> CalculateTravelTimeAsync(
        Coordinates origin,
        Coordinates destination,
        TravelMode travelMode = TravelMode.Driving,
        DateTime? departureTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get optimal service areas for a provider based on location and capacity
    /// </summary>
    /// <param name="providerId">Provider identifier</param>
    /// <param name="maxTravelTime">Maximum acceptable travel time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recommended service area boundaries</returns>
    Task<ServiceAreaRecommendation> GetOptimalServiceAreaAsync(
        ProviderId providerId,
        TimeSpan maxTravelTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if a location is within a provider's service area
    /// </summary>
    /// <param name="providerId">Provider identifier</param>
    /// <param name="clientLocation">Client's location</param>
    /// <param name="serviceType">Type of service requested</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service area validation result</returns>
    Task<ServiceAreaValidation> ValidateLocationInServiceAreaAsync(
        ProviderId providerId,
        Coordinates clientLocation,
        ServiceCategory serviceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find the nearest available provider for a specific service
    /// </summary>
    /// <param name="clientLocation">Client's location</param>
    /// <param name="serviceId">Requested service</param>
    /// <param name="preferredDateTime">Preferred appointment time</param>
    /// <param name="maxDistance">Maximum acceptable distance in kilometers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Nearest provider with availability</returns>
    Task<NearestProviderResult?> FindNearestAvailableProviderAsync(
        Coordinates clientLocation,
        ServiceId serviceId,
        DateTime preferredDateTime,
        double maxDistance = 50.0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate service delivery zones based on demand density
    /// </summary>
    /// <param name="centerLocation">Central location</param>
    /// <param name="serviceCategory">Service category</param>
    /// <param name="demandData">Historical demand data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimized delivery zones</returns>
    Task<IEnumerable<ServiceDeliveryZone>> CalculateOptimalDeliveryZonesAsync(
        Coordinates centerLocation,
        ServiceCategory serviceCategory,
        IEnumerable<DemandDataPoint> demandData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get location accessibility score for disabled clients
    /// </summary>
    /// <param name="providerLocation">Provider's location</param>
    /// <param name="accessibilityRequirements">Required accessibility features</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Accessibility assessment</returns>
    Task<AccessibilityAssessment> AssessLocationAccessibilityAsync(
        BusinessAddress providerLocation,
        AccessibilityRequirements accessibilityRequirements,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predict optimal provider locations based on market analysis
    /// </summary>
    /// <param name="targetArea">Geographic area for analysis</param>
    /// <param name="serviceCategory">Service category</param>
    /// <param name="competitorData">Competitor location data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Location recommendations with scoring</returns>
    Task<IEnumerable<LocationRecommendation>> PredictOptimalLocationsAsync(
        GeographicBounds targetArea,
        ServiceCategory serviceCategory,
        IEnumerable<CompetitorLocation> competitorData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate location-based pricing adjustments
    /// </summary>
    /// <param name="baseLocation">Base provider location</param>
    /// <param name="serviceLocation">Actual service delivery location</param>
    /// <param name="serviceType">Type of service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing adjustment factors</returns>
    Task<LocationPricingAdjustment> CalculateLocationPricingAsync(
        Coordinates baseLocation,
        Coordinates serviceLocation,
        ServiceCategory serviceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate business address and suggest corrections
    /// </summary>
    /// <param name="address">Address to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Address validation with suggestions</returns>
    Task<AddressValidationResult> ValidateAndSuggestAddressAsync(
        BusinessAddress address,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of provider location search
/// </summary>
public class ProviderLocationResult
{
    public ProviderId ProviderId { get; init; } = null!;
    public string ProviderName { get; init; } = string.Empty;
    public BusinessAddress Address { get; init; } = null!;
    public Coordinates Coordinates { get; init; } = null!;
    public double DistanceInKilometers { get; init; }
    public Rating AverageRating { get; init; } = null!;
    public bool IsCurrentlyOpen { get; init; }
    public TimeSpan? EstimatedTravelTime { get; init; }
    public IEnumerable<ServiceCategory> AvailableServices { get; init; } = Array.Empty<ServiceCategory>();
}

/// <summary>
/// Travel time calculation result
/// </summary>
public class TravelTimeResult
{
    public TimeSpan EstimatedTravelTime { get; init; }
    public double DistanceInKilometers { get; init; }
    public TravelMode TravelMode { get; init; }
    public string RouteDescription { get; init; } = string.Empty;
    public bool HasTrafficDelays { get; init; }
    public TimeSpan? TrafficDelay { get; init; }
    public IEnumerable<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Service area recommendation
/// </summary>
public class ServiceAreaRecommendation
{
    public ProviderId ProviderId { get; init; } = null!;
    public Coordinates CenterPoint { get; init; } = null!;
    public double RecommendedRadiusKm { get; init; }
    public IEnumerable<GeographicBounds> OptimalZones { get; init; } = Array.Empty<GeographicBounds>();
    public decimal EstimatedMarketPotential { get; init; }
    public int EstimatedTravelTimeMinutes { get; init; }
    public string RecommendationReason { get; init; } = string.Empty;
}

/// <summary>
/// Service area validation result
/// </summary>
public class ServiceAreaValidation
{
    public bool IsWithinServiceArea { get; init; }
    public double DistanceFromProviderKm { get; init; }
    public TimeSpan EstimatedTravelTime { get; init; }
    public decimal? AdditionalTravelFee { get; init; }
    public string ValidationMessage { get; init; } = string.Empty;
    public ServiceAreaValidationLevel ValidationLevel { get; init; }
}

/// <summary>
/// Nearest provider search result
/// </summary>
public class NearestProviderResult
{
    public ProviderId ProviderId { get; init; } = null!;
    public string ProviderName { get; init; } = string.Empty;
    public double DistanceInKilometers { get; init; }
    public TimeSpan EstimatedTravelTime { get; init; }
    public DateTime EarliestAvailableTime { get; init; }
    public Price EstimatedPrice { get; init; } = null!;
    public Rating ProviderRating { get; init; } = null!;
    public bool RequiresAdditionalTravel { get; init; }
}

/// <summary>
/// Service delivery zone
/// </summary>
public class ServiceDeliveryZone
{
    public string ZoneId { get; init; } = string.Empty;
    public string ZoneName { get; init; } = string.Empty;
    public GeographicBounds Boundaries { get; init; } = null!;
    public int DemandLevel { get; init; }
    public decimal SuggestedPricingMultiplier { get; init; }
    public TimeSpan AverageServiceTime { get; init; }
    public int EstimatedServiceCapacity { get; init; }
}

/// <summary>
/// Accessibility assessment result
/// </summary>
public class AccessibilityAssessment
{
    public AccessibilityScore OverallScore { get; init; }
    public bool HasWheelchairAccess { get; init; }
    public bool HasElevatorAccess { get; init; }
    public bool HasAccessibleParking { get; init; }
    public bool HasAccessibleRestrooms { get; init; }
    public IEnumerable<string> AccessibilityFeatures { get; init; } = Array.Empty<string>();
    public IEnumerable<string> AccessibilityLimitations { get; init; } = Array.Empty<string>();
    public string AccessibilityNotes { get; init; } = string.Empty;
}

/// <summary>
/// Location recommendation for new providers
/// </summary>
public class LocationRecommendation
{
    public Coordinates RecommendedLocation { get; init; } = null!;
    public decimal ViabilityScore { get; init; }
    public decimal CompetitionDensity { get; init; }
    public decimal MarketDemand { get; init; }
    public decimal AccessibilityScore { get; init; }
    public string RecommendationReason { get; init; } = string.Empty;
    public IEnumerable<string> SuccessFactors { get; init; } = Array.Empty<string>();
    public IEnumerable<string> RiskFactors { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Location-based pricing adjustment
/// </summary>
public class LocationPricingAdjustment
{
    public decimal TravelFeeAmount { get; init; }
    public decimal PricingMultiplier { get; init; }
    public string AdjustmentReason { get; init; } = string.Empty;
    public bool IsTravelFeeRequired { get; init; }
    public TimeSpan AdditionalTravelTime { get; init; }
    public double AdditionalDistanceKm { get; init; }
}

/// <summary>
/// Address validation result
/// </summary>
public class AddressValidationResult
{
    public bool IsValid { get; init; }
    public BusinessAddress? CorrectedAddress { get; init; }
    public Coordinates? SuggestedCoordinates { get; init; }
    public IEnumerable<string> ValidationErrors { get; init; } = Array.Empty<string>();
    public IEnumerable<BusinessAddress> AlternativeSuggestions { get; init; } = Array.Empty<BusinessAddress>();
    public AddressValidationConfidence ConfidenceLevel { get; init; }
}

/// <summary>
/// Geographic bounds for area definitions
/// </summary>
public class GeographicBounds
{
    public Coordinates NorthEast { get; init; } = null!;
    public Coordinates SouthWest { get; init; } = null!;
    public string? Name { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Demand data point for analysis
/// </summary>
public class DemandDataPoint
{
    public Coordinates Location { get; init; } = null!;
    public DateTime Timestamp { get; init; }
    public int DemandLevel { get; init; }
    public ServiceCategory ServiceCategory { get; init; }
    public decimal Revenue { get; init; }
}

/// <summary>
/// Competitor location data
/// </summary>
public class CompetitorLocation
{
    public string CompetitorName { get; init; } = string.Empty;
    public Coordinates Location { get; init; } = null!;
    public IEnumerable<ServiceCategory> Services { get; init; } = Array.Empty<ServiceCategory>();
    public Rating? EstimatedRating { get; init; }
    public decimal? EstimatedPricing { get; init; }
    public int? EstimatedCapacity { get; init; }
}

/// <summary>
/// Accessibility requirements
/// </summary>
public class AccessibilityRequirements
{
    public bool RequiresWheelchairAccess { get; init; }
    public bool RequiresElevatorAccess { get; init; }
    public bool RequiresAccessibleParking { get; init; }
    public bool RequiresAccessibleRestrooms { get; init; }
    public bool RequiresSignLanguageSupport { get; init; }
    public IEnumerable<string> AdditionalRequirements { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Travel modes for distance calculations
/// </summary>
public enum TravelMode
{
    Driving,
    Walking,
    PublicTransport,
    Cycling,
    Taxi
}

/// <summary>
/// Service area validation levels
/// </summary>
public enum ServiceAreaValidationLevel
{
    WithinPrimaryArea,
    WithinExtendedArea,
    OutsideAreaButServicable,
    OutsideServiceArea
}

/// <summary>
/// Accessibility scoring
/// </summary>
public enum AccessibilityScore
{
    Excellent,
    Good,
    Fair,
    Poor,
    Inaccessible
}

/// <summary>
/// Address validation confidence levels
/// </summary>
public enum AddressValidationConfidence
{
    High,
    Medium,
    Low,
    Invalid
}