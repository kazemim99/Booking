// ========================================
// Authorization/PermissionRequirement.cs
// ========================================
using Microsoft.AspNetCore.Authorization;

namespace Booksy.Infrastructure.Security.Authorization;

/// <summary>
/// Permission-based authorization requirement
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}