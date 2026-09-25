// ========================================
// AsanRezerve.ServiceCatalog.Application/Queries/Provider/GetOwnedProviderStatus/GetOwnedProviderStatusQueryHandler.cs
// ========================================
using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AsanRezerve.ServiceCatalog.Application.Queries.Provider.GetOwnedProviderStatus
{
    /// <summary>
    /// Resolves the salon owned by the current user (by <c>OwnerId</c>) and returns its status;
    /// null when the user owns no salon, which is the normal answer for an employee.
    /// </summary>
    public sealed class GetOwnedProviderStatusQueryHandler
        : IQueryHandler<GetOwnedProviderStatusQuery, ProviderStatusResult?>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetOwnedProviderStatusQueryHandler(
            IProviderReadRepository providerRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<ProviderStatusResult?> Handle(
            GetOwnedProviderStatusQuery request,
            CancellationToken cancellationToken)
        {
            // Get current user ID from claims
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Return null if user not authenticated or invalid - controller will handle with 401/404
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return null;
            }

            var ownerId = UserId.From(userGuid);

            // Fetch provider by owner ID
            var provider = await _providerRepository.GetByOwnerIdAsync(ownerId, cancellationToken);

            // Return null if no provider record exists (404 will be returned by controller)
            if (provider == null)
            {
                return null;
            }

            // Return minimal status information
            return new ProviderStatusResult(
                ProviderId: provider.Id.Value,
                Status: provider.Status,
                UserId: userGuid);
        }
    }
}
