namespace Booksy.Core.Application.Exceptions;

/// <summary>
/// The caller is asking too often. Answered as 429, with <see cref="RetryAfter"/> surfaced in the
/// <c>Retry-After</c> header so a client knows when to come back instead of hammering.
///
/// <para>Distinct from <c>DomainValidationException</c> on purpose: "you have asked for three codes
/// in ten minutes" is not a malformed request, and answering it 400 tells the client to fix a
/// payload that is perfectly valid. The OTP send path used to do exactly that.</para>
/// </summary>
public sealed class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How long the caller should wait, when it can be known.</summary>
    public TimeSpan? RetryAfter { get; }
}
