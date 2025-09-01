// ========================================
// Authorization/BusinessHoursRequirement.cs
// ========================================
using Microsoft.AspNetCore.Authorization;

namespace Booksy.Infrastructure.Security.Authorization.Requirements;

/// <summary>
/// Business hours access requirement
/// </summary>
public class BusinessHoursRequirement : IAuthorizationRequirement
{
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    public BusinessHoursRequirement()
    {
        StartTime = new TimeOnly(8, 0); // 8:00 AM
        EndTime = new TimeOnly(18, 0);  // 6:00 PM
    }
}
