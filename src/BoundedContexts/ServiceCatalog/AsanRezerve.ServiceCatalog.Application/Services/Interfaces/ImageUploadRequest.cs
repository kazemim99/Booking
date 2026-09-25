// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
// ========================================
// AsanRezerve.ServiceCatalog.Application/Services/Interfaces/IImageProcessingService.cs
// ========================================
namespace AsanRezerve.ServiceCatalog.Application.Services.Interfaces
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