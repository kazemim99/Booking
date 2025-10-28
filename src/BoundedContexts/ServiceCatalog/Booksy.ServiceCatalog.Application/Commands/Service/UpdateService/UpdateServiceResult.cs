// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/UpdateService/UpdateServiceResult.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.Commands.Service.UpdateService
{
    public sealed record UpdateServiceResult(
        Guid ServiceId,
        string Name,
        string Description,
        DateTime UpdatedAt);

    public sealed class AddServiceRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationHours { get; set; } = 0;
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
        public string? Category { get; set; }
        public bool IsMobileService { get; set; }
    }
}