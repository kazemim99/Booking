namespace AsanRezerve.API.RateLimiting;

/// <summary>
/// Limits for the named policies the controllers already reference through
/// <c>[EnableRateLimiting("...")]</c>. Every value is configuration so an environment can tune it
/// without a deploy, and so a test host can raise them explicitly.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Master switch. Off in test hosts, on everywhere else. When off the policies are still
    /// registered — the attributes must always resolve to something — they simply do not limit.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new();

    /// <summary>
    /// Defaults for every policy name that appears in an <c>[EnableRateLimiting]</c> attribute.
    /// A name missing here would throw at request time, so this table and the attributes must stay
    /// in step; <c>RateLimitingRegistration</c> registers exactly these.
    /// </summary>
    public static IReadOnlyDictionary<string, RateLimitPolicyOptions> Defaults { get; } =
        new Dictionary<string, RateLimitPolicyOptions>(StringComparer.OrdinalIgnoreCase)
        {
            // Costs money and lands on a stranger's phone. The per-phone rules live in the OTP
            // handler; this is the per-caller ceiling on top of them.
            ["phone-verification"] = new(PermitLimit: 5, WindowSeconds: 300),
            ["otp"] = new(PermitLimit: 5, WindowSeconds: 300),
            ["code-verification"] = new(PermitLimit: 10, WindowSeconds: 300),

            // Credential surfaces: enough for a human who mistypes, not enough to enumerate.
            ["authentication"] = new(PermitLimit: 10, WindowSeconds: 60),
            ["password-reset"] = new(PermitLimit: 5, WindowSeconds: 900),

            // Account and business creation.
            ["registration"] = new(PermitLimit: 10, WindowSeconds: 3600),
            ["provider-registration"] = new(PermitLimit: 10, WindowSeconds: 3600),
            ["service-creation"] = new(PermitLimit: 60, WindowSeconds: 3600),

            // Reads and ordinary writes: high enough that normal use never notices.
            ["public-api"] = new(PermitLimit: 120, WindowSeconds: 60),
            ["provider-availability"] = new(PermitLimit: 120, WindowSeconds: 60),
            ["provider-reviews"] = new(PermitLimit: 120, WindowSeconds: 60),
            ["create-review"] = new(PermitLimit: 20, WindowSeconds: 3600),
            ["mark-review-helpful"] = new(PermitLimit: 60, WindowSeconds: 3600),
            // An edit unpublishes a review and forces a full per-provider rating recompute, triggered by an
            // ordinary customer for seven days — hence the tightest ceiling of the review write paths.
            ["edit-review"] = new(PermitLimit: 10, WindowSeconds: 3600),
            ["report-review"] = new(PermitLimit: 20, WindowSeconds: 3600),
            ["reply-review"] = new(PermitLimit: 60, WindowSeconds: 3600),
            // Administrators working the queue: generous, but not unbounded.
            ["moderate-review"] = new(PermitLimit: 600, WindowSeconds: 3600),
        };
}

/// <param name="PermitLimit">Requests allowed per window, per caller.</param>
/// <param name="WindowSeconds">Length of the fixed window.</param>
public sealed record RateLimitPolicyOptions(int PermitLimit = 60, int WindowSeconds = 60);
