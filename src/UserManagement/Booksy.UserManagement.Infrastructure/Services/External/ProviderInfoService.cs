using Booksy.Core.Domain.Infrastructure.Middleware;
using Booksy.UserManagement.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Booksy.UserManagement.Infrastructure.Services.External;

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

    // DTO that matches the ServiceCatalog API response structure
    private record ProviderDetailsDto(
        Guid Id,           // ProviderId
        string Status,
        Guid OwnerId);
}
