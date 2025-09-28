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
            // Admin policies
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Administrator", "SysAdmin"));

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
