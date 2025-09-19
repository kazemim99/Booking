// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using Booksy.Core.Domain.ValueObjects;
using System.Security.Claims;

namespace Booksy.UserManagement.Application.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(
            UserId userId,
            Email email,
            string displayName,
            IEnumerable<string> roles,
            int expirationHours = 24);

        ClaimsPrincipal? ValidateToken(string token);
        string? GetUserIdFromToken(string token);
        DateTime? GetTokenExpiration(string token);
        bool IsTokenExpired(string token);
    }
}
