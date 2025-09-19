// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/IImageProcessingService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    /// <summary>
    /// Service for handling image processing operations
    /// </summary>
    public interface IImageProcessingService
    {
        // Upload Operations
        Task<string> UploadServiceImageAsync(Stream imageStream, string fileName, Guid serviceId, CancellationToken cancellationToken = default);
        Task<string> UploadProviderImageAsync(Stream imageStream, string fileName, Guid providerId, ImageType imageType = ImageType.Logo, CancellationToken cancellationToken = default);
        Task<string> UploadStaffImageAsync(Stream imageStream, string fileName, Guid providerId, Guid staffId, CancellationToken cancellationToken = default);

        // Image Processing
        Task<string> ResizeImageAsync(string imageUrl, int width, int height, CancellationToken cancellationToken = default);
        Task<byte[]> GenerateThumbnailAsync(Stream imageStream, int size = 150, CancellationToken cancellationToken = default);
        Task<string> OptimizeImageAsync(string imageUrl, int quality = 85, CancellationToken cancellationToken = default);

        // Image Management
        Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default);
        Task<string> GetImageUrlAsync(string imagePath, CancellationToken cancellationToken = default);
        Task<List<string>> GetImagesForServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);
        Task<List<string>> GetImagesForProviderAsync(Guid providerId, CancellationToken cancellationToken = default);

        // Validation
        Task<bool> ValidateImageAsync(Stream imageStream, long maxSizeBytes = 5242880, CancellationToken cancellationToken = default); // 5MB default
        Task<ImageMetadata> GetImageMetadataAsync(Stream imageStream, CancellationToken cancellationToken = default);
        Task<List<string>> GetSupportedImageFormatsAsync();

        // Bulk Operations
        Task<List<string>> UploadMultipleImagesAsync(List<ImageUploadRequest> requests, CancellationToken cancellationToken = default);
        Task<bool> DeleteMultipleImagesAsync(List<string> imageUrls, CancellationToken cancellationToken = default);
    }
}