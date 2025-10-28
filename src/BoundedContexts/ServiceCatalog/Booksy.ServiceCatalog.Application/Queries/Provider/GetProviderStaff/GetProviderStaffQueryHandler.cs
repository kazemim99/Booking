// ========================================
// Application/Queries/Provider/GetProviderStaff/GetProviderStaffQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderStaff
{
    /// <summary>
    /// Handler for GetProviderStaffQuery - retrieves staff through Provider aggregate
    /// ✅ DDD-Compliant: Uses IProviderReadRepository only and accesses staff through Provider
    /// </summary>
    internal sealed class GetProviderStaffQueryHandler
        : IQueryHandler<GetProviderStaffQuery, GetProviderStaffResult>
    {
        private readonly IProviderReadRepository _providerRepository;

        public GetProviderStaffQueryHandler(IProviderReadRepository providerRepository)
        {
            _providerRepository = providerRepository;
        }

        public async Task<GetProviderStaffResult> Handle(
            GetProviderStaffQuery request,
            CancellationToken cancellationToken)
        {
            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new KeyNotFoundException($"Provider {request.ProviderId} not found");

            var staffList = request.IncludeInactive == true
                ? provider.Staff 
                : provider.GetActiveStaff(); 

            var staffDtos = staffList.Select(staff => new StaffDto(
                staff.Id,
                staff.FirstName,
                staff.LastName,
                staff.FullName,
                staff.Phone?.Value?.ToString(),
                staff.Role!.ToString(),
                staff.IsActive,
                staff.HiredAt,
                staff.TerminatedAt,
                staff.Notes
            )).ToList();

            return new GetProviderStaffResult(
                provider.Id.Value,
                provider.Profile.BusinessName,
                staffDtos);
        }
    }
}
