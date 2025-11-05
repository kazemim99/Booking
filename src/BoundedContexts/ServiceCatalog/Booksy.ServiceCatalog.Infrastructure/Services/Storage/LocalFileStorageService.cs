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
    private readonly string _baseUrl;

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IImageOptimizationService imageOptimizationService)
    {
        _environment = environment;
        _configuration = configuration;
        _imageOptimizationService = imageOptimizationService;

        _basePath = configuration["FileStorage:BasePath"] ?? "wwwroot/uploads";
        var applicationBaseUrl = configuration["Application:BaseUrl"] ?? "https://localhost:7002";
        _baseUrl = $"{applicationBaseUrl}/uploads";
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

        // Generate URLs
        var thumbnailUrl = $"{_baseUrl}/providers/{providerId}/gallery/{imageId}_thumb.webp";
        var mediumUrl = $"{_baseUrl}/providers/{providerId}/gallery/{imageId}_medium.webp";
        var originalUrl = $"{_baseUrl}/providers/{providerId}/gallery/{imageId}_original.webp";

        return new StorageResult(originalUrl, thumbnailUrl, mediumUrl);
    }

    public async Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract relative path from URL
            var uri = new Uri(imageUrl);
            var relativePath = uri.AbsolutePath.TrimStart('/');

            // Get file path
            var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", relativePath);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception)
        {
            // Log error but don't throw - file deletion is best effort
            return false;
        }
    }

    public string GetImageUrl(string relativePath)
    {
        return $"{_baseUrl}/{relativePath}";
    }
}
