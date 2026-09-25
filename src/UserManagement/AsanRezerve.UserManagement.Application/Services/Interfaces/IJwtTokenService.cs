// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Enums;
using System.Security.Claims;

namespace AsanRezerve.UserManagement.Application.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(
            UserId userId,
            UserType userType,
            Email email,
            string displayName,
            string firstName,
            string lastName,
            string status,
            IEnumerable<string> roles,
            string? providerId = null,
            string? providerStatus = null,
            string? customerId = null,
            string? phoneNumber = null,
            IEnumerable<MembershipSummary>? memberships = null,
            string? activeMembershipId = null,
            int expirationHours = 24);

        ClaimsPrincipal? ValidateToken(string token);
        string? GetUserIdFromToken(string token);
        DateTime? GetTokenExpiration(string token);
        bool IsTokenExpired(string token);
    }
}
