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

/// <summary>Null-preserving absolute URLs for read models.</summary>
public static class UrlServiceExtensions
{
    /// <summary>
    /// A stored media path as a URL a browser on ANY host can load. Uploads are stored relative
    /// ("uploads/providers/…"); sent as-is, a Flutter web page resolves them against its own host, whose nginx answers
    /// index.html — so no customer ever saw a salon photo (QA walkthrough 2026-09-22). Every read model that sends a
    /// media URL goes through this. Absolute values pass through unchanged; no value stays no value.
    /// </summary>
    public static string? AbsoluteOrNull(this IUrlService urls, string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : urls.ToAbsoluteUrl(path);
}
