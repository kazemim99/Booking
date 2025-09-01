// ========================================
// Filters/AuthorizationFilter.cs
// ========================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Booksy.UserManagement.API.Filters;

public class AuthorizationFilter : IAuthorizationFilter
{
    private readonly string _permission;

    public AuthorizationFilter(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check for specific permission claim
        if (!user.HasClaim("permission", _permission))
        {
            context.Result = new ForbidResult();
        }
    }
}