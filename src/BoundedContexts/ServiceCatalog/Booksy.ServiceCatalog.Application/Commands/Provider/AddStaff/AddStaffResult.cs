// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/AddStaff/AddStaffResult.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaff
{
    public sealed record AddStaffResult(
        Guid ProviderId,
        Guid StaffId,
        string FullName,
        string Email,
        StaffRole Role,
        DateTime HiredAt);
}