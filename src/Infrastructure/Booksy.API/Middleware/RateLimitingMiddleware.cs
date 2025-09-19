// ========================================
// Middleware/ExceptionHandlingMiddleware.cs
// ========================================


namespace Booksy.API.Middleware;


public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = configuration.GetSection("RateLimit").Get<RateLimitOptions>() ?? new RateLimitOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var rateLimitAttribute = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();

        if (rateLimitAttribute != null)
        {
            var key = GenerateClientKey(context);
            var rateLimitKey = $"rate_limit_{rateLimitAttribute.Policy}_{key}";

            var requestCount = await _cache.GetOrCreateAsync(rateLimitKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (requestCount >= GetLimitForPolicy(rateLimitAttribute.Policy))
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
                return;
            }

            _cache.Set(rateLimitKey, requestCount + 1, TimeSpan.FromMinutes(1));
        }

        await _next(context);
    }

    private string GenerateClientKey(HttpContext context)
    {
        var userId = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return userId;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private int GetLimitForPolicy(string policy)
    {
        return policy switch
        {
            "authentication" => _options.AuthenticationLimit,
            "registration" => _options.RegistrationLimit,
            "password-reset" => _options.PasswordResetLimit,
            _ => _options.DefaultLimit
        };
    }
}
