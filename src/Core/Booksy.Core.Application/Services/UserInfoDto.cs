using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Core.Domain.Infrastructure.Models
{
    // ========================================
    // Booksy.Core.Application/Services/UserInfoDto.cs
    // ========================================
    namespace Booksy.Core.Application.Services
    {
        /// <summary>
        /// Data transfer object for user information
        /// </summary>
        public sealed class UserInfoDto
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? DisplayName { get; set; }
            public string? PhoneNumber { get; set; }
            public string? AvatarUrl { get; set; }
            public List<string> Roles { get; set; } = new();
            public Dictionary<string, string> Claims { get; set; } = new();
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastLoginAt { get; set; }

            public string FullName => $"{FirstName} {LastName}".Trim();

            public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

            public string? GetClaim(string claimType) =>
                Claims.TryGetValue(claimType, out var value) ? value : null;
        }
    }
}
