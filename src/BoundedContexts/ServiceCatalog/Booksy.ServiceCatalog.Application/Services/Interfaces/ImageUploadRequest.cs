// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/IImageProcessingService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    public sealed class ImageUploadRequest
    {
        public Stream ImageStream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public ImageType ImageType { get; set; }
        public string? Description { get; set; }
    }
}