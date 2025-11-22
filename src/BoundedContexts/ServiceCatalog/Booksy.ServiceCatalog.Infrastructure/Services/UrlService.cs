using Booksy.ServiceCatalog.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Booksy.ServiceCatalog.Infrastructure.Services;

/// <summary>
/// Service for generating URLs based on the current HTTP context
/// </summary>
public sealed class UrlService : IUrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UrlService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBaseUrl()
    {
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
