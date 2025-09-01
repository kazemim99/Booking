using Booksy.Infrastructure.Security.Authorization.Requirements;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Handler for business hours authorization
/// </summary>
public class BusinessHoursAuthorizationHandler : AuthorizationHandler<BusinessHoursRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BusinessHoursRequirement requirement)
    {
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);

        if (currentTime >= requirement.StartTime && currentTime <= requirement.EndTime)
        {
            context.Succeed(requirement);
        }

        // Admins can access outside business hours
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}