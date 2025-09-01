// ========================================
// Authorization/MinimumAgeRequirement.cs
// ========================================
using Microsoft.AspNetCore.Authorization;

namespace Booksy.Infrastructure.Security.Authorization.Requirements;

/// <summary>
/// Minimum age authorization requirement
/// </summary>
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }

    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }
}
