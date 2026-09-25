namespace AsanRezerve.UserManagement.Application.Configuration;

/// <summary>
/// How hard the anonymous OTP endpoint pushes back. Every sent code costs real money and lands on
/// somebody's phone, so these are abuse limits, not preferences.
///
/// <para>They are configuration for one reason: the previous per-phone cap was wrapped in
/// <c>#if !DEBUG</c>, which meant the only protection on the send path vanished in any Debug build
/// — the protection depended on how the binary was compiled rather than on how it was configured.
/// A test host now raises the limits explicitly and visibly instead.</para>
/// </summary>
public sealed class OtpProtectionOptions
{
    public const string SectionName = "Otp:Protection";

    /// <summary>How many codes one phone number may be sent within <see cref="Window"/>.</summary>
    public int MaxSendsPerWindow { get; set; } = 3;

    /// <summary>The window the send cap is counted over.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The quiet period between two codes for the same phone. Matches
    /// <c>PhoneVerification.CanResend()</c>, which has always modelled 60 seconds but was never
    /// consulted on the anonymous send path.
    /// </summary>
    public TimeSpan ResendCooldown { get; set; } = TimeSpan.FromSeconds(60);
}
