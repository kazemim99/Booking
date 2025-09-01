// ========================================
// Analytics/IAnalyticsService.cs
// ========================================
namespace Booksy.Infrastructure.External.Analytics;

/// <summary>
/// Analytics service interface
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Tracks an event
    /// </summary>
    Task TrackEventAsync(string eventName, string userId, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks a page view
    /// </summary>
    Task TrackPageViewAsync(string pageName, string userId, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies a user
    /// </summary>
    Task IdentifyUserAsync(string userId, Dictionary<string, object> traits, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tracks revenue
    /// </summary>
    Task TrackRevenueAsync(string userId, decimal amount, string currency, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default);
}
