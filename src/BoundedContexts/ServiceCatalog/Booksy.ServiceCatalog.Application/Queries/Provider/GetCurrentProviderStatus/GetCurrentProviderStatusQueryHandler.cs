// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetCurrentProviderStatus/GetCurrentProviderStatusQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetCurrentProviderStatus
{
    /// <summary>
    /// Handles fetching the current user's Provider status
    /// </summary>
    public sealed class GetCurrentProviderStatusQueryHandler
        : IQueryHandler<GetCurrentProviderStatusQuery, ProviderStatusResult?>
    {
        private readonly IProviderReadRepository _providerRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetCurrentProviderStatusQueryHandler(
            IProviderReadRepository providerRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<ProviderStatusResult?> Handle(
            GetCurrentProviderStatusQuery request,
            CancellationToken cancellationToken)
        {
            // Get current user ID from claims
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                throw new UnauthorizedAccessException("User not authenticated");
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
