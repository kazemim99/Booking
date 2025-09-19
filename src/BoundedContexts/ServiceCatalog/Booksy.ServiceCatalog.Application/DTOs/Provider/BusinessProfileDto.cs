// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Provider/BusinessProfileDto.cs
// ========================================
namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class BusinessProfileDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public Dictionary<string, string> SocialMedia { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public DateTime LastUpdatedAt { get; set; }
    }
}