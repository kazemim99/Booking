// ========================================
// Analytics/IAnalyticsService.cs
// ========================================
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Booksy.Infrastructure.External.Analytics;
using Microsoft.Extensions.Options;



/// <summary>
/// Mixpanel analytics implementation
/// </summary>
public class MixpanelService : IAnalyticsService
{
    private readonly HttpClient _httpClient;
    private readonly MixpanelSettings _settings;
    private readonly ILogger<MixpanelService> _logger;

    public MixpanelService(
        HttpClient httpClient,
        IOptions<MixpanelSettings> settings,
        ILogger<MixpanelService> logger)
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
            var eventData = new
            {
                @event = eventName,
                properties = new Dictionary<string, object>
                {
                    ["token"] = _settings.ProjectToken,
                    ["distinct_id"] = userId,
                    ["time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    eventData.properties[prop.Key] = prop.Value;
                }
            }

            var json = JsonSerializer.Serialize(eventData);
            var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            await _httpClient.GetAsync($"https://api.mixpanel.com/track/?data={encodedData}", cancellationToken);

            _logger.LogDebug("Mixpanel event {EventName} tracked for user {UserId}", eventName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track Mixpanel event {EventName}", eventName);
        }
    }

    public async Task TrackPageViewAsync(
        string pageName,
        string userId,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        var pageProperties = new Dictionary<string, object>
        {
            ["page"] = pageName
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                pageProperties[prop.Key] = prop.Value;
            }
        }

        await TrackEventAsync("Page Viewed", userId, pageProperties, cancellationToken);
    }

    public async Task IdentifyUserAsync(
        string userId,
        Dictionary<string, object> traits,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profileData = new
            {
                token = _settings.ProjectToken,
                distinct_id = userId,
                set = traits
            };

            var json = JsonSerializer.Serialize(profileData);
            var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            await _httpClient.GetAsync($"https://api.mixpanel.com/engage/?data={encodedData}", cancellationToken);

            _logger.LogDebug("Mixpanel user {UserId} identified", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify Mixpanel user {UserId}", userId);
        }
    }

    public async Task TrackRevenueAsync(
        string userId,
        decimal amount,
        string currency,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        var revenueProperties = new Dictionary<string, object>
        {
            ["amount"] = amount,
            ["currency"] = currency
        };

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                revenueProperties[prop.Key] = prop.Value;
            }
        }

        await TrackEventAsync("Revenue", userId, revenueProperties, cancellationToken);
    }
}

// ========================================
// Booksy.Infrastructure.External.csproj
// ========================================
/*
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

 

  <ItemGroup>
    <ProjectReference Include="..\Booksy.Infrastructure.Core\Booksy.Infrastructure.Core.csproj" />
  </ItemGroup>

</Project>
*/