using Booksy.ServiceCatalog.Domain.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Storage;

/// <summary>
/// Local file system storage implementation
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IImageOptimizationService _imageOptimizationService;
    private readonly string _basePath;

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IImageOptimizationService imageOptimizationService)
    {
        _environment = environment;
        _configuration = configuration;
        _imageOptimizationService = imageOptimizationService;

        _basePath = configuration["FileStorage:BasePath"] ?? "wwwroot/uploads";
    }

    public async Task<StorageResult> UploadImageAsync(
        Guid providerId,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Generate unique image ID
        var imageId = Guid.NewGuid().ToString("N");

        // Optimize image
        var optimized = await _imageOptimizationService.OptimizeAsync(
            imageStream,
            new ImageOptimizationOptions(),
            cancellationToken);

        // Create provider directory
        var providerDir = Path.Combine(
            _environment.ContentRootPath,
            _basePath,
            "providers",
            providerId.ToString(),
            "gallery");

        Directory.CreateDirectory(providerDir);

        // Save files
        var thumbnailPath = Path.Combine(providerDir, $"{imageId}_thumb.webp");
        var mediumPath = Path.Combine(providerDir, $"{imageId}_medium.webp");
        var originalPath = Path.Combine(providerDir, $"{imageId}_original.webp");

        await File.WriteAllBytesAsync(thumbnailPath, optimized.Thumbnail, cancellationToken);
        await File.WriteAllBytesAsync(mediumPath, optimized.Medium, cancellationToken);
        await File.WriteAllBytesAsync(originalPath, optimized.Original, cancellationToken);

        // Generate RELATIVE paths (not absolute URLs)
        // The UrlService will convert these to absolute URLs when fetching
        var thumbnailUrl = $"uploads/providers/{providerId}/gallery/{imageId}_thumb.webp";
        var mediumUrl = $"uploads/providers/{providerId}/gallery/{imageId}_medium.webp";
        var originalUrl = $"uploads/providers/{providerId}/gallery/{imageId}_original.webp";

        return new StorageResult(originalUrl, thumbnailUrl, mediumUrl);
    }

    public async Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            string relativePath;

            // Handle both absolute URLs and relative paths
            if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Extract relative path from absolute URL
                var uri = new Uri(imageUrl);
                relativePath = uri.AbsolutePath.TrimStart('/');
            }
            else
            {
                // Already a relative path
                relativePath = imageUrl.TrimStart('/');
            }

            // Get file path - files are stored in ContentRootPath/basePath
            var filePath = Path.Combine(_environment.ContentRootPath, _basePath, relativePath.Replace("uploads/", ""));

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - file deletion is best effort
            Console.WriteLine($"Error deleting image {imageUrl}: {ex.Message}");
            return false;
        }
    }

    public string GetImageUrl(string relativePath)
    {
        // Return the relative path as-is
        // The UrlService will convert it to an absolute URL when needed
        return relativePath;
    }
}
