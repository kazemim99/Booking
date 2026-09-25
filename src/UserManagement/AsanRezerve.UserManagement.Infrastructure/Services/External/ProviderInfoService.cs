using AsanRezerve.Core.Domain.Infrastructure.Middleware;
using AsanRezerve.UserManagement.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AsanRezerve.UserManagement.Infrastructure.Services.External;

public class ProviderInfoService : IProviderInfoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProviderInfoService> _logger;

    public ProviderInfoService(
        IHttpClientFactory httpClientFactory,
        ILogger<ProviderInfoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ProviderInfo?> GetProviderByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ServiceCatalogAPI");

            var response = await client.GetFromJsonAsync<ApiResponse<ProviderDetailsDto>>(
                $"/api/v1/Providers/by-owner/{ownerId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No provider found for owner {OwnerId}", ownerId);
                return null;
            }

            if (response?.Data == null)
            {
                return null;
            }

            return new ProviderInfo(
                response.Data.Id,  // API returns "Id" not "ProviderId"
                response.Data.Status);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Failed to query provider info for owner {OwnerId}. Service may be unavailable.",
                ownerId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error querying provider info for owner {OwnerId}",
                ownerId);
            return null;
        }
    }

    /// <summary>
    /// The standalone UserManagement host has no way to read salons in one call (ServiceCatalog exposes no batched
    /// public endpoint), so it returns none; the customer's lists then come back empty rather than failing. The
    /// composed AsanRezerve.Host — the one that runs in production — uses InProcessProviderInfoService instead.
    /// </summary>
    public Task<IReadOnlyDictionary<Guid, SalonCard>> GetActiveSalonCardsAsync(
        IReadOnlyCollection<Guid> providerIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, SalonCard>>(new Dictionary<Guid, SalonCard>());

    // DTO that matches the ServiceCatalog API response structure
    private record ProviderDetailsDto(
        Guid Id,           // ProviderId
        string Status,
        Guid OwnerId);
}
