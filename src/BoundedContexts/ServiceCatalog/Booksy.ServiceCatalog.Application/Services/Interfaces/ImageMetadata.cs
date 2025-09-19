// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetServiceStatistics/GetServiceStatisticsQuery.cs
// ========================================
// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/IImageProcessingService.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    public sealed class ImageMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long SizeBytes { get; set; }
        public string Format { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string? ColorProfile { get; set; }
        public bool HasTransparency { get; set; }
    }
}