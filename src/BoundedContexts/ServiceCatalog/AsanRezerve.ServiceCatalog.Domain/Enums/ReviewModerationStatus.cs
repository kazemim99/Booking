namespace AsanRezerve.ServiceCatalog.Domain.Enums;

/// <summary>
/// Whether an administrator has cleared a review (or a provider reply) for public display.
/// </summary>
/// <remarks>
/// <para><b>Independent of <c>Review.IsVerified</c>.</b> Verification means "came from a completed booking";
/// this means "an administrator cleared it". A hidden review from a real booking is still verified.</para>
///
/// <para>Persisted by name, like the module's other enums, so the names must stay stable. <see cref="Pending"/>
/// is the database default for the column, so a row written by code that predates moderation lands in the queue
/// rather than going straight to the public.</para>
/// </remarks>
public enum ReviewModerationStatus
{
    /// <summary>Waiting for an administrator. Visible only to its author, the owning provider and admins.</summary>
    Pending = 0,

    /// <summary>Cleared for display. The only state that is public and counts toward a provider's rating.</summary>
    Published = 1,

    /// <summary>Refused before it was ever public. Permanent: a rejected review is never published.</summary>
    Rejected = 2,

    /// <summary>Taken down after publication. Reversible by an administrator.</summary>
    Hidden = 3,
}
