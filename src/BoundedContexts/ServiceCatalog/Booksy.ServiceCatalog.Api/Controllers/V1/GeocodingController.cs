using System.Text.Json.Nodes;
using Booksy.ServiceCatalog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Booksy.ServiceCatalog.Api.Controllers.V1;

/// <summary>
/// Place lookup for the map picker, so clients never call the geocoder themselves.
///
/// <para>Anonymous on purpose: onboarding picks a location before any provider exists. Rate-limited per
/// caller, because the upstream budget is shared by everyone using this deployment.</para>
///
/// <para>Answers the upstream's JSON as the payload of the host's standard envelope (clients read
/// `data`), and 503 when the lookup is unavailable — callers treat that as "no result" and keep
/// whatever the user typed.</para>
///
/// <para>The parsed JSON is returned as an object rather than a <c>ContentResult</c>: a ContentResult
/// sets Content-Length for the raw upstream body, the envelope middleware then writes a longer body
/// over it, and Kestrel fails the request with 500 (production, 2026-09-19).</para>
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("public-api")]
public sealed class GeocodingController : ControllerBase
{
    private readonly IGeocodingProvider _geocoding;

    public GeocodingController(IGeocodingProvider geocoding) => _geocoding = geocoding;

    /// <summary>Place name to candidates (e.g. «پارس آباد, اردبیل»).</summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { message = "q is required" });
        }

        var json = await _geocoding.SearchAsync(q, limit, cancellationToken);
        return json is null ? StatusCode(StatusCodes.Status503ServiceUnavailable) : Ok(JsonNode.Parse(json));
    }

    /// <summary>Coordinates to an address, for a tap on the map.</summary>
    [HttpGet("reverse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Reverse(
        [FromQuery] double lat,
        [FromQuery] double lon,
        CancellationToken cancellationToken = default)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180)
        {
            return BadRequest(new { message = "lat/lon out of range" });
        }

        var json = await _geocoding.ReverseAsync(lat, lon, cancellationToken);
        return json is null ? StatusCode(StatusCodes.Status503ServiceUnavailable) : Ok(JsonNode.Parse(json));
    }
}
