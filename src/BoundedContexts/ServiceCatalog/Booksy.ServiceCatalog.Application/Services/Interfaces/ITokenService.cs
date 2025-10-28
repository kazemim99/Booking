namespace Booksy.ServiceCatalog.Application.Services.Interfaces;

/// <summary>
/// Service for generating JWT tokens with provider claims
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a new JWT token including provider claims
    /// </summary>
    /// <param name="userId">User ID (OwnerId)</param>
    /// <param name="providerId">Provider ID</param>
    /// <param name="providerStatus">Provider status (e.g., PendingVerification, Active)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token response with access token and refresh token</returns>
    Task<TokenResponse> GenerateTokenWithProviderClaimsAsync(
        Guid userId,
        Guid providerId,
        string providerStatus,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Token response from UserManagement API
/// </summary>
public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = "Bearer";
}
