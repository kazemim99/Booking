// ========================================
// Middleware/ExceptionHandlingMiddleware.cs
// ========================================
namespace Booksy.API.Middleware;

[AttributeUsage(AttributeTargets.Method)]
public class EnableRateLimitingAttribute : Attribute
{
    public string Policy { get; }

    public EnableRateLimitingAttribute(string policy)
    {
        Policy = policy;
    }
}