using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetProviderWithStaff;

/// <summary>
/// Query to get an organization provider with all its staff members (linked individual providers)
/// </summary>
public sealed record GetProviderWithStaffQuery(
    Guid ProviderId) : IQuery<GetProviderWithStaffResult>;

public sealed record GetProviderWithStaffResult(
    Guid ProviderId,
    string BusinessName,
    string? ProfileImageUrl,
    string HierarchyType,
    string Status,
    bool IsIndependent,
    Guid? ParentProviderId,
    OrganizationInfoDto? ParentOrganization,
    IReadOnlyList<StaffProviderDto> StaffMembers,
    int TotalStaffCount);

public sealed record OrganizationInfoDto(
    Guid ProviderId,
    string BusinessName,
    string? ProfileImageUrl,
    string Status);

public sealed record StaffProviderDto(
    Guid ProviderId,
    string BusinessName,
    string? ProfileImageUrl,
    string Status,
    bool IsIndependent,
    DateTime JoinedAt,
    decimal AverageRating,
    int ServiceCount);
