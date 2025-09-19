// ========================================
// Booksy.ServiceCatalog.Application/DTOs/Provider/StaffDto.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.DTOs.Provider
{
    public sealed class StaffDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public StaffRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime HiredAt { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public string? TerminationReason { get; set; }
        public string? Notes { get; set; }
    }
}