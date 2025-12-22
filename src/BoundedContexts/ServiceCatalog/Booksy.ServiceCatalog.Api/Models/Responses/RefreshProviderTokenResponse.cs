// ========================================
// Booksy.ServiceCatalog.Api/Models/Responses/RefreshProviderTokenResponse.cs
// ========================================
namespace Booksy.ServiceCatalog.Api.Models.Responses
{
    /// <summary>
    /// Response model for refreshing provider token after registration
    /// </summary>
    public sealed class RefreshProviderTokenResponse
    {
        /// <summary>
        /// New access token with updated provider claims
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// New refresh token
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Token expiration time in seconds
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Token type (Bearer)
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Provider ID included in the new token
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Current provider status included in the new token
        /// </summary>
        public string ProviderStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Internal model for token generation API response
    /// </summary>
    internal sealed class TokenGenerationResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}
