// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Provider/ProviderDto.cs
// ========================================
using Booksy.ServiceCatalog.Application.Mappings;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class ProviderDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public BusinessProfileDto Profile { get; set; } = new();
        public ProviderStatus Status { get; set; }
        public ProviderType Type { get; set; }
        public ContactInfoDto ContactInfo { get; set; } = new();
        public BusinessAddressDto Address { get; set; } = new();
        public bool RequiresApproval { get; set; }
        public bool AllowOnlineBooking { get; set; }
        public bool OffersMobileServices { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? ActivatedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public List<BusinessHoursDto> BusinessHours { get; set; } = new();
        public List<StaffDto> Staff { get; set; } = new();

        // Provider hierarchy properties
        public ProviderHierarchyType HierarchyType { get; set; }
        public bool IsIndependent { get; set; }
        public Guid? ParentProviderId { get; set; }
    }
}