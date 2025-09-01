// ========================================
// Authorization/PermissionAuthorizationHandler.cs
// ========================================
using Microsoft.AspNetCore.Authorization;

namespace Booksy.Infrastructure.Security.Authorization;

/// <summary>
/// Handler for permission-based authorization
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}