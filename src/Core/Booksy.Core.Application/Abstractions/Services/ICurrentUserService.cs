// ========================================
// Booksy.Core.Application/Abstractions/Services/ICurrentUserService.cs
// ========================================
using System.Security.Claims;

namespace Booksy.Core.Application.Abstractions.Services
{
    /// <summary>
    /// Provides information about the current user
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the current user's ID
        /// </summary>
        string? UserId { get; }

        /// <summary>
        /// Gets the current user's email
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Gets the current user's name
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Gets whether the current user is authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the current user's roles
        /// </summary>
        IEnumerable<string> Roles { get; }

        /// <summary>
        /// Gets the current user's claims
        /// </summary>
        IEnumerable<Claim> Claims { get; }

        /// <summary>
        /// Checks if the current user is in a specific role
        /// </summary>
        bool IsInRole(string role);

        /// <summary>
        /// Gets a specific claim value for the current user
        /// </summary>
        string? GetClaimValue(string claimType);


        /// <summary>
        /// Gets the current user's IP address
        /// </summary>
        string? IpAddress { get; }

        /// <summary>
        /// Gets the current user's user agent
        /// </summary>
        string? UserAgent { get; }
    }
}