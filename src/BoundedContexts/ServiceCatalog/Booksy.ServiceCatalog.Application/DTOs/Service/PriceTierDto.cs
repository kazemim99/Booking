// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Service/PriceTierDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.DTOs.Service
{
    public sealed class PriceTierDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}