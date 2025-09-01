using Booksy.Core.Domain.Infrastructure.Models;

namespace Booksy.Core.Domain.Application.Services
{
    /// <summary>
    /// JWT token service for authentication
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates an access token for the user
        /// </summary>
        string GenerateAccessToken(UserInfoDto user, IReadOnlyList<string> roles);
        string GenerateAccessToken(UserInfoDto user, object roles);

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Validates a token and returns the claims principal
        /// </summary>
        System.Security.Claims.ClaimsPrincipal ValidateToken(string token);
    }
}