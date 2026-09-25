using AsanRezerve.ServiceCatalog.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Services;

/// <summary>
/// Service for generating URLs based on the current HTTP context
/// </summary>
public sealed class UrlService : IUrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// The address browsers reach this API at (App:PublicBaseUrl), e.g. https://back.nahalkmi.ir.
    /// Behind a TLS-terminating proxy the request itself says http://, and image URLs built from it
    /// were blocked by HTTPS pages as mixed content (2026-09-19). Null falls back to the request.
    /// </summary>
    private readonly string? _publicBaseUrl;

    public UrlService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        var configured = configuration["App:PublicBaseUrl"];
        _publicBaseUrl = string.IsNullOrWhiteSpace(configured) ? null : configured.TrimEnd('/');
    }

    public string GetBaseUrl()
    {
        if (_publicBaseUrl != null)
        {
            return _publicBaseUrl;
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return string.Empty;
        }

        // Build the base URL: {scheme}://{host}{pathBase}
        // Example: http://localhost:5010/api
        return $"{request.Scheme}://{request.Host}{request.PathBase}";
    }

    public string ToAbsoluteUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        // If the path is already an absolute URL (starts with http:// or https://), return it as-is
        if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        var baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            return relativePath;
        }

        // Ensure the path starts with a slash
        var path = relativePath.StartsWith('/') ? relativePath : $"/{relativePath}";

        return $"{baseUrl}{path}";
    }
}
