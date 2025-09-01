// ========================================
// Analytics/IAnalyticsService.cs
// ========================================
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booksy.Infrastructure.External.Analytics;

/// <summary>
/// Google Analytics implementation
/// </summary>
public class GoogleAnalyticsService : IAnalyticsService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleAnalyticsSettings _settings;
    private readonly ILogger<GoogleAnalyticsService> _logger;

    public GoogleAnalyticsService(
        HttpClient httpClient,
        IOptions<GoogleAnalyticsSettings> settings,
        ILogger<GoogleAnalyticsService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task TrackEventAsync(
        string eventName,
        string userId,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                client_id = userId,
                user_id = userId,
                events = new[]
                {
                    new
                    {
                        name = eventName,
                        @params = properties ?? new Dictionary<string, object>()
                    }
                }
            };

            await SendToGoogleAnalyticsAsync(payload, cancellationToken);

            _logger.LogDebug("Event {EventName} tracked for user {UserId}", eventName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track event {EventName}", eventName);
        }
    }

    public async Task TrackPageViewAsync(
        string pageName,
        string userId,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        await TrackEventAsync("page_view", userId,
            new Dictionary<string, object>
            {
                ["page_title"] = pageName,
                ["page_location"] = properties?.GetValueOrDefault("url") ?? pageName
            },
            cancellationToken);
    }

    public async Task IdentifyUserAsync(
        string userId,
        Dictionary<string, object> traits,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                client_id = userId,
                user_id = userId,
                user_properties = traits
            };

            await SendToGoogleAnalyticsAsync(payload, cancellationToken);

            _logger.LogDebug("User {UserId} identified", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify user {UserId}", userId);
        }
    }

    public async Task TrackRevenueAsync(
        string userId,
        decimal amount,
        string currency,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        var eventProperties = new Dictionary<string, object>
        {
            ["value"] = amount,
            ["currency"] = currency
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                eventProperties[prop.Key] = prop.Value;
            }
        }

        await TrackEventAsync("purchase", userId, eventProperties, cancellationToken);
    }

    private async Task SendToGoogleAnalyticsAsync(object payload, CancellationToken cancellationToken)
    {
        var url = $"https://www.google-analytics.com/mp/collect?measurement_id={_settings.MeasurementId}&api_secret={_settings.ApiSecret}";

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google Analytics returned {StatusCode}", response.StatusCode);
        }
    }
}



public class GoogleAnalyticsSettings
{
    public string MeasurementId { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}