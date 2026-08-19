using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Application;

/// <summary>
/// Service for communicating with UserManagement API to generate tokens.
///
/// <para>Despite the HTTP call, both contexts run in the same process (single-host modular monolith) — this is
/// a loopback request to <c>Services:UserManagement:BaseUrl</c>, not a call to a separate service.</para>
///
/// <para><b>Why this needs the caller's own token.</b> <c>POST /api/v1/auth/generate-token</c> has no
/// <c>[AllowAnonymous]</c>, so it falls under the host's global <c>FallbackPolicy.RequireAuthenticatedUser()</c>.
/// The <c>UserManagementAPI</c> HTTP client only attaches an <c>X-API-Key</c> header when
/// <c>Services:UserManagement:ApiKey</c> is configured (it is empty by default), and no API-key authentication
/// scheme is registered on the receiving side regardless — only JWT Bearer. An unauthenticated call to this
/// endpoint therefore always 401s. That surfaced as provider registration's final step ("complete") failing
/// with <c>HTTP 500: Token generation failed with status Unauthorized</c> — this method is what the
/// registration-completion flow calls to mint the caller a fresh token carrying their new provider claims.</para>
///
/// <para>The fix is to forward the current request's OWN bearer token: this method only ever runs inside an
/// already-authenticated request (registration completion requires the provider to be signed in), so
/// re-presenting that same token satisfies the fallback policy without a new auth scheme or a stored secret.
/// <c>GenerateToken</c> does not check that the caller matches the target <c>UserId</c> — it accepts any
/// authenticated caller — so this is not narrower than what the endpoint already allows; it is narrower than
/// what an eventual <c>X-API-Key</c> would allow, since it can only ever act as whoever is already calling.</para>
/// </summary>
public class TokenService : ITokenService
{
    // Explicit rather than implicit: makes the deserialization options match the host's actual response
    // casing (Program.cs: JsonNamingPolicy.CamelCase) by declaration, not by relying on whatever
    // ReadFromJsonAsync's parameterless overload happens to default to on a given runtime. The defect that
    // actually caused empty tokens was the response ENVELOPE never being unwrapped (see the class remarks and
    // GenerateTokenEnvelope below) — not casing.
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
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

            // Re-present the caller's own bearer token. The endpoint requires SOME authenticated caller
            // (host-wide FallbackPolicy) but does not check the caller matches the target UserId, so this is
            // sufficient and never grants more than the caller already had.
            var incomingAuthHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization
                .ToString();
            if (!string.IsNullOrEmpty(incomingAuthHeader) &&
                AuthenticationHeaderValue.TryParse(incomingAuthHeader, out var authHeader))
            {
                client.DefaultRequestHeaders.Authorization = authHeader;
            }
            else
            {
                _logger.LogWarning(
                    "No inbound Authorization header available while generating a token for user {UserId}; " +
                    "the internal call to UserManagement will be unauthenticated and is expected to fail.",
                    userId);
            }

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
                "api/v1/auth/generate-token",
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

            // Every controller response on this host is wrapped by ApiResponseMiddleware into
            // { success, statusCode, message, data, metadata } — the actual TokenResponse lives at .data, not
            // at the root. Deserializing straight into TokenResponse (as this used to) matches nothing at the
            // top level, and System.Text.Json silently fills in the record's defaults rather than failing:
            // AccessToken == "", RefreshToken == null. Nothing here threw, so registration completion reported
            // HTTP 200 while quietly handing the provider a blank token — no exception, no null, just wrong data.
            var envelope = await response.Content.ReadFromJsonAsync<GenerateTokenEnvelope>(
                WebJsonOptions,
                cancellationToken: cancellationToken);

            var tokenResponse = envelope?.Data;

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
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

    /// <summary>
    /// Only the piece of the host's standard <c>ApiResponse&lt;T&gt;</c> envelope this call needs. Declared
    /// locally rather than referencing the shared middleware type, so this internal client does not take on a
    /// project reference to <c>Booksy.API</c> for one DTO shape.
    /// </summary>
    private record GenerateTokenEnvelope
    {
        public TokenResponse? Data { get; init; }
    }
}
