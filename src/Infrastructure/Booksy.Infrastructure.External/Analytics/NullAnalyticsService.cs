// ========================================
// Analytics/IAnalyticsService.cs
// ========================================
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Analytics;



/// <summary>
/// Null implementation for development/testing
/// </summary>
public class NullAnalyticsService : IAnalyticsService
{
    private readonly ILogger<NullAnalyticsService> _logger;

    public NullAnalyticsService(ILogger<NullAnalyticsService> logger)
    {
        _logger = logger;
    }

    public Task TrackEventAsync(string eventName, string userId, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analytics event: {EventName} for user {UserId}", eventName, userId);
        return Task.CompletedTask;
    }

    public Task TrackPageViewAsync(string pageName, string userId, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analytics page view: {PageName} for user {UserId}", pageName, userId);
        return Task.CompletedTask;
    }

    public Task IdentifyUserAsync(string userId, Dictionary<string, object> traits, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analytics identify user: {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task TrackRevenueAsync(string userId, decimal amount, string currency, Dictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analytics revenue: {Amount} {Currency} for user {UserId}", amount, currency, userId);
        return Task.CompletedTask;
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