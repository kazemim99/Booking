namespace Booksy.ServiceCatalog.Domain.Services;

/// <summary>
/// Service for optimizing images
/// </summary>
public interface IImageOptimizationService
{
    /// <summary>
    /// Optimize an image and return multiple sizes
    /// </summary>
    Task<OptimizedImage> OptimizeAsync(Stream sourceStream, ImageOptimizationOptions options, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for image optimization
/// </summary>
public record ImageOptimizationOptions(
    int ThumbnailSize = 200,
    int MediumSize = 800,
    int OriginalMaxSize = 2000,
    int Quality = 85,
    string Format = "webp");

/// <summary>
/// Result of image optimization
/// </summary>
public record OptimizedImage(
    byte[] Thumbnail,
    byte[] Medium,
    byte[] Original);
