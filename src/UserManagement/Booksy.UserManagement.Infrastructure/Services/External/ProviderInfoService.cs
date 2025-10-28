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

            var response = await client.GetAsync(
                $"/api/v1/Providers/by-owner/{ownerId}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No provider found for owner {OwnerId}", ownerId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ProviderStatusResponse>(
                cancellationToken: cancellationToken);

            if (result?.Data == null)
            {
                return null;
            }

            return new ProviderInfo(
                Guid.Parse(result.Data.ProviderId),
                result.Data.Status);
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

    private record ProviderStatusResponse(
        bool Success,
        int StatusCode,
        string Message,
        ProviderStatusData? Data);

    private record ProviderStatusData(
        string ProviderId,
        string Status,
        string UserId);
}
