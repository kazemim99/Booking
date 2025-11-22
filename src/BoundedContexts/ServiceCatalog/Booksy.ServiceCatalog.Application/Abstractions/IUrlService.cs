namespace Booksy.ServiceCatalog.Application.Abstractions;

/// <summary>
/// Service for generating URLs based on the current HTTP context
/// </summary>
public interface IUrlService
{
    /// <summary>
    /// Gets the base URL of the current request (e.g., http://localhost:5010/api)
    /// </summary>
    string GetBaseUrl();

    /// <summary>
    /// Converts a relative path to an absolute URL using the current request's base URL
    /// </summary>
    /// <param name="relativePath">Relative path (e.g., uploads/providers/xxx/image.webp)</param>
    /// <returns>Absolute URL (e.g., http://localhost:5010/api/uploads/providers/xxx/image.webp)</returns>
    string ToAbsoluteUrl(string relativePath);
}
