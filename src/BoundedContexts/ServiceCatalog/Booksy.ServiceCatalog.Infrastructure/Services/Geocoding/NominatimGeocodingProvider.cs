using System.Globalization;
using Booksy.ServiceCatalog.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Geocoding;

/// <summary>
/// <see cref="IGeocodingProvider"/> over OpenStreetMap's Nominatim — keyless, and the same data as the
/// map tiles the picker shows.
///
/// <para>Its usage policy asks for an identifying User-Agent, results to be cached, and a light request
/// rate. The cache below is what keeps a busy onboarding session down to a handful of upstream calls:
/// the same city or the same pin is looked up repeatedly as a user moves back and forth in the wizard.</para>
///
/// <para>The upstream JSON is passed through untouched. Clients already parse this shape, and reshaping
/// it here would mean two places to change whenever a field is needed.</para>
/// </summary>
public sealed class NominatimGeocodingProvider : IGeocodingProvider
{
    /// <summary>Identifies this deployment to Nominatim, as its usage policy requires.</summary>
    public const string UserAgent = "BooksyProvider/1.0 (+https://provider.nahalkmi.ir)";

    private static readonly TimeSpan CacheFor = TimeSpan.FromHours(24);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NominatimGeocodingProvider> _logger;

    public NominatimGeocodingProvider(
        HttpClient http,
        IMemoryCache cache,
        ILogger<NominatimGeocodingProvider> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        }
    }

    public Task<string?> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<string?>(null);
        }

        var capped = Math.Clamp(limit, 1, 10);
        var path = "/search"
            + $"?q={Uri.EscapeDataString(query.Trim())}"
            + $"&format=jsonv2&accept-language=fa&countrycodes=ir&limit={capped}";

        return GetCachedAsync(path, cancellationToken);
    }

    public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        var path = $"/reverse?lat={lat}&lon={lon}&format=jsonv2&accept-language=fa&addressdetails=1";

        return GetCachedAsync(path, cancellationToken);
    }

    private async Task<string?> GetCachedAsync(string path, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue<string>(path, out var cached))
        {
            return cached;
        }

        try
        {
            using var response = await _http.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocoding upstream answered {Status} for {Path}", (int)response.StatusCode, path);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            // Only successful answers are cached: a transient failure must not be remembered for a day.
            _cache.Set(path, body, CacheFor);
            return body;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Never throws: the caller is filling in a form, and a failed lookup just means they keep
            // what they typed.
            _logger.LogWarning(ex, "Geocoding upstream unreachable for {Path}", path);
            return null;
        }
    }
}
