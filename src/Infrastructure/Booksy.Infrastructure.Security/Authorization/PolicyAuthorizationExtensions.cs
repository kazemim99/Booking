// ========================================
// Authorization/PolicyAuthorizationExtensions.cs
// ========================================
using Booksy.Infrastructure.Security.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Infrastructure.Security.Authorization;

/// <summary>
/// Extension methods for policy-based authorization
/// </summary>
public static class PolicyAuthorizationExtensions
{
    /// <summary>
    /// Adds policy-based authorization
    /// </summary>
    public static IServiceCollection AddPolicyAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Global fallback (C1 harden-resource-authorization): any endpoint that does
            // not carry an explicit [Authorize]/[AllowAnonymous] requires an authenticated
            // user. Genuinely public endpoints are explicitly [AllowAnonymous] (see the C1
            // authorization audit). Infrastructure endpoints (health checks, Swagger) are
            // exempted at their mapping in Program.cs. This closes the "unannotated endpoint
            // is implicitly public" gap.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Admin policies
            // "Admin" is the role production grants an administrator and the one the
            // [Authorize(Roles = "Admin,...")] endpoints check. Without it here, the real admin got
            // 403 on every AdminOnly endpoint (the admin panel's provider pages, 2026-09-19); tests
            // missed it because the test admin carries all three names. The vocabulary itself is
            // FOLLOW-UPS #46. No code path grants any of these three roles: registration gives
            // Customer/Provider only, so widening this opens no self-service route.
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin", "Administrator", "SysAdmin"));

            options.AddPolicy("SysAdminOnly", policy =>
                policy.RequireRole("SysAdmin"));

            // User type policies
            options.AddPolicy("ClientOnly", policy =>
                policy.RequireClaim("user_type", "Client"));

            options.AddPolicy("ProviderOnly", policy =>
                policy.RequireClaim("user_type", "Provider"));

            options.AddPolicy("ClientOrProvider", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "user_type" &&
                        (c.Value == "Client" || c.Value == "Provider"))));   
            
            options.AddPolicy("ProviderOrAdmin", policy => 
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "user_type" &&
                        (c.Value == "Admin" || c.Value == "Provider"))));

            // Feature-based policies
            options.AddPolicy("CanManageUsers", policy =>
                policy.Requirements.Add(new PermissionRequirement("users:manage")));

            options.AddPolicy("CanViewReports", policy =>
                policy.Requirements.Add(new PermissionRequirement("reports:view")));

            options.AddPolicy("CanManageBookings", policy =>
                policy.Requirements.Add(new PermissionRequirement("bookings:manage")));

            // Combined policies
            options.AddPolicy("EmailVerified", policy =>
                policy.RequireClaim("email_verified", "true"));

            options.AddPolicy("ActiveUser", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim("status", "Active") &&
                    context.User.HasClaim("email_verified", "true")));

            // Age-based policy
            options.AddPolicy("MinimumAge18", policy =>
                policy.Requirements.Add(new MinimumAgeRequirement(18)));

            // Time-based access
            options.AddPolicy("BusinessHours", policy =>
                policy.Requirements.Add(new BusinessHoursRequirement()));
        });

        // Register authorization handlers
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, MinimumAgeAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, BusinessHoursAuthorizationHandler>();

        return services;
    }

}
