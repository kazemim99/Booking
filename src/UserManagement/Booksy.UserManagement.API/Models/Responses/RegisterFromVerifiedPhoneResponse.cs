// ========================================
// Booksy.UserManagement.API/Models/Responses/RegisterFromVerifiedPhoneResponse.cs
// ========================================

namespace Booksy.UserManagement.API.Models.Responses;

/// <summary>
/// Response from creating user account from verified phone
/// Includes authentication tokens for immediate login
/// </summary>
public sealed class RegisterFromVerifiedPhoneResponse
{
    /// <summary>
    /// Created user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for getting new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (always "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
