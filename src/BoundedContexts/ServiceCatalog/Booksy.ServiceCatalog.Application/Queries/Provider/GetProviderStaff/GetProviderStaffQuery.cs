// ========================================
// Application/Queries/Provider/GetProviderStaff/GetProviderStaffQuery.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStaff
{
    /// <summary>
    /// Query to get all staff members for a provider through Provider aggregate
    /// Follows DDD principles by querying through Provider aggregate root
    /// </summary>
    public sealed record GetProviderStaffQuery(
        Guid ProviderId,
        bool? IncludeInactive = false) : IQuery<GetProviderStaffResult>;

    /// <summary>
    /// Result containing the list of staff members
    /// </summary>
    public sealed record GetProviderStaffResult(
        Guid ProviderId,
        string ProviderName,
        List<StaffDto> Staff);

    /// <summary>
    /// DTO for staff member information
    /// </summary>
    public sealed record StaffDto(
        Guid Id,
        string FirstName,
        string LastName,
        string FullName,
        string? PhoneNumber,
        string Role,
        bool IsActive,
        DateTime HiredAt,
        DateTime? TerminatedAt,
        string? Notes);
}
