// ========================================
// Booksy.ServiceCatalog.Application/Queries/Service/GetQualifiedStaff/GetQualifiedStaffResult.cs
// ========================================

namespace Booksy.ServiceCatalog.Application.Queries.Service.GetQualifiedStaff
{
    public sealed record GetQualifiedStaffResult(
        Guid ProviderId,
        Guid ServiceId,
        IReadOnlyList<StaffMemberDto> QualifiedStaff);

    public sealed record StaffMemberDto(
        Guid Id,
        string Name,
        string? PhotoUrl,
        double? Rating,
        int? ReviewCount,
        string? Specialization);
}
