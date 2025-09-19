// ========================================
// Middleware/ExceptionHandlingMiddleware.cs
// ========================================
namespace Booksy.API.Middleware;

public class RateLimitOptions
{
    public int DefaultLimit { get; set; } = 100;
    public int AuthenticationLimit { get; set; } = 5;
    public int RegistrationLimit { get; set; } = 3;
    public int PasswordResetLimit { get; set; } = 3;
}
