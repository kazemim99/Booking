using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
// AddRateLimiter lives in Microsoft.AspNetCore.Builder, not the DependencyInjection namespace its
// siblings use.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.API.RateLimiting;

/// <summary>
/// Registers the named policies that <c>[EnableRateLimiting("...")]</c> attributes across the API
/// have always referred to.
///
/// <para><b>Why this exists.</b> Thirteen policy names appear on endpoints — OTP send and verify,
/// authentication, password reset, registration, reviews, public reads. None of them existed:
/// ASP.NET Core's rate limiter was never added, so every one of those attributes was decoration.
/// The only limiter running was AspNetCoreRateLimit's blanket client rule, which cannot express
/// "five OTPs per five minutes" and which bucketed every anonymous caller together.</para>
///
/// <para><b>Partitioning.</b> By authenticated user when there is one, otherwise by remote IP —
/// never by a shared constant, which is what made the old anonymous bucket a shared-fate limit.
/// Per-phone protection is deliberately NOT here: a limiter cannot read the phone number without
/// buffering the request body, and the OTP handler already owns that rule.</para>
/// </summary>
public static class RateLimitingRegistration
{
    public static IServiceCollection AddAsanRezerveRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RateLimitingOptions();
        configuration.GetSection(RateLimitingOptions.SectionName).Bind(options);

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Tell the caller when to come back. Without it a client can only guess, and guessing
            // means retrying immediately.
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Too many requests. Please try again later.\",\"errorCode\":\"TOO_MANY_REQUESTS\"}",
                    cancellationToken);
            };

            foreach (var (name, defaults) in RateLimitingOptions.Defaults)
            {
                var policy = options.Policies.TryGetValue(name, out var configured) ? configured : defaults;

                limiter.AddPolicy(name, httpContext => options.Enabled
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        PartitionKey(httpContext, name),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = policy.PermitLimit,
                            Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        })
                    // Registered but not limiting: the attribute must still resolve, or every
                    // decorated endpoint throws at request time.
                    : RateLimitPartition.GetNoLimiter(name));
            }
        });

        return services;
    }

    /// <summary>
    /// One bucket per caller per policy. The authenticated user id when present — so a signed-in
    /// user behind a shared NAT is not throttled by their neighbours — otherwise the remote IP.
    /// </summary>
    private static string PartitionKey(HttpContext httpContext, string policyName)
    {
        var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        var caller = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return $"{policyName}|{caller}";
    }
}
