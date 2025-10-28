using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Application;

/// <summary>
/// Service for communicating with UserManagement API to generate tokens
/// </summary>
public class TokenService : ITokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IHttpClientFactory httpClientFactory,
        ILogger<TokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TokenResponse> GenerateTokenWithProviderClaimsAsync(
        Guid userId,
        Guid providerId,
        string providerStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserManagementAPI");

            var request = new GenerateTokenRequest
            {
                UserId = userId.ToString(),
                AdditionalClaims = new Dictionary<string, string>
                {
                    { "provider_id", providerId.ToString() },
                    { "provider_status", providerStatus }
                }
            };

            _logger.LogInformation(
                "Requesting new token for user {UserId} with provider {ProviderId}",
                userId,
                providerId);

            var response = await client.PostAsJsonAsync(
                "/v1/auth/generate-token",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to generate token. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode,
                    errorContent);

                throw new HttpRequestException(
                    $"Token generation failed with status {response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken);

            if (tokenResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize token response");
            }

            _logger.LogInformation(
                "Successfully generated token for user {UserId} with provider {ProviderId}",
                userId,
                providerId);

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating token for user {UserId} with provider {ProviderId}",
                userId,
                providerId);
            throw;
        }
    }

    private record GenerateTokenRequest
    {
        public string UserId { get; init; } = string.Empty;
        public Dictionary<string, string>? AdditionalClaims { get; init; }
    }
}
