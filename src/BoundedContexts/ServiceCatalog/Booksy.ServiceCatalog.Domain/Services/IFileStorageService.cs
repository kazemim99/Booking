namespace Booksy.ServiceCatalog.Domain.Services;

/// <summary>
/// Service for storing and retrieving files
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload an image file and return the storage result with URLs
    /// </summary>
    Task<StorageResult> UploadImageAsync(
        Guid providerId,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an image file by its URL
    /// </summary>
    Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the full URL for a relative path
    /// </summary>
    string GetImageUrl(string relativePath);
}

/// <summary>
/// Result of image storage operation
/// </summary>
public record StorageResult(
    string OriginalUrl,
    string ThumbnailUrl,
    string MediumUrl);
