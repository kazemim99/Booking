using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.ProviderHierarchy.GetProviderWithStaff;

public sealed class GetProviderWithStaffQueryHandler : IQueryHandler<GetProviderWithStaffQuery, GetProviderWithStaffResult>
{
    private readonly IProviderReadRepository _providerRepository;
    private readonly ILogger<GetProviderWithStaffQueryHandler> _logger;

    public GetProviderWithStaffQueryHandler(
        IProviderReadRepository providerRepository,
        ILogger<GetProviderWithStaffQueryHandler> logger)
    {
        _providerRepository = providerRepository;
        _logger = logger;
    }

    public async Task<GetProviderWithStaffResult> Handle(GetProviderWithStaffQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting provider with staff for provider {ProviderId}", request.ProviderId);

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
            throw new NotFoundException($"Provider with ID {request.ProviderId} not found");

        // Get parent organization if this is a staff member
        OrganizationInfoDto? parentOrganization = null;
        if (provider.ParentProviderId != null)
        {
            var parent = await _providerRepository.GetByIdAsync(provider.ParentProviderId, cancellationToken);
            if (parent != null)
            {
                parentOrganization = new OrganizationInfoDto(
                    ProviderId: parent.Id.Value,
                    BusinessName: parent.Profile.BusinessName,
                    ProfileImageUrl: parent.Profile.ProfileImageUrl,
                    Status: parent.Status.ToString());
            }
        }

        // Get staff members if this is an organization
        var staffMembers = new List<StaffProviderDto>();
        if (provider.HierarchyType == ProviderHierarchyType.Organization)
        {
            var staff = await _providerRepository.GetStaffByOrganizationIdAsync(providerId, cancellationToken);
            staffMembers = staff.Select(s => new StaffProviderDto(
                ProviderId: s.Id.Value,
                BusinessName: s.Profile.BusinessName,
                ProfileImageUrl: s.Profile.ProfileImageUrl,
                Status: s.Status.ToString(),
                IsIndependent: s.IsIndependent,
                JoinedAt: s.CreatedAt,
                AverageRating: s.AverageRating,
                ServiceCount: s.Services.Count)).ToList();
        }

        var totalStaffCount = provider.HierarchyType == ProviderHierarchyType.Organization
            ? await _providerRepository.CountStaffByOrganizationAsync(providerId, cancellationToken)
            : 0;

        return new GetProviderWithStaffResult(
            ProviderId: provider.Id.Value,
            BusinessName: provider.Profile.BusinessName,
            ProfileImageUrl: provider.Profile.ProfileImageUrl,
            HierarchyType: provider.HierarchyType.ToString(),
            Status: provider.Status.ToString(),
            IsIndependent: provider.IsIndependent,
            ParentProviderId: provider.ParentProviderId?.Value,
            ParentOrganization: parentOrganization,
            StaffMembers: staffMembers,
            TotalStaffCount: totalStaffCount);
    }
}
