// ========================================
// Booksy.ServiceCatalog.Application/Queries/Provider/GetProviderClients/GetProviderClientsQueryHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Queries.Provider.GetProviderClients
{
    /// <summary>
    /// Read-only projection: delegates to the clients read service (the
    /// cross-schema identity seam lives in Infrastructure).
    /// </summary>
    public sealed class GetProviderClientsQueryHandler
        : IQueryHandler<GetProviderClientsQuery, GetProviderClientsResult>
    {
        private readonly IProviderClientsReadService _readService;
        private readonly ILogger<GetProviderClientsQueryHandler> _logger;

        public GetProviderClientsQueryHandler(
            IProviderClientsReadService readService,
            ILogger<GetProviderClientsQueryHandler> logger)
        {
            _readService = readService;
            _logger = logger;
        }

        public async Task<GetProviderClientsResult> Handle(
            GetProviderClientsQuery request,
            CancellationToken cancellationToken)
        {
            var clients = await _readService.GetClientsAsync(
                request.ProviderId, cancellationToken);

            _logger.LogInformation(
                "Resolved {Count} clients for provider {ProviderId}",
                clients.Count, request.ProviderId);

            return new GetProviderClientsResult(clients);
        }
    }

    /// <summary>
    /// Derives the provider's client book from bookings, resolving customer
    /// identity read-only across contexts (implemented in Infrastructure).
    /// </summary>
    public interface IProviderClientsReadService
    {
        Task<IReadOnlyList<ProviderClientDto>> GetClientsAsync(
            Guid providerId,
            CancellationToken cancellationToken = default);
    }
}
