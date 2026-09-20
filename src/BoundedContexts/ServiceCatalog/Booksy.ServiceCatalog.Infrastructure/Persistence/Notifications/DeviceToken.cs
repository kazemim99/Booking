using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Notifications
{
    /// <summary>
    /// One device a person can be pushed to.
    /// </summary>
    /// <remarks>
    /// <para>Without this table the push channel has no address: the dispatcher knows a recipient's user id
    /// and nothing else, which is why push previously could not have worked even if the gateway had been
    /// real.</para>
    ///
    /// <para><b>A token belongs to one person at a time.</b> The token identifies the app installation, not
    /// the account — so when somebody signs in on a handset that another person used, the same token arrives
    /// under a new user id. It is reassigned rather than duplicated, because a second row would keep pushing
    /// the previous owner's notifications to a phone that is no longer theirs.</para>
    /// </remarks>
    public sealed class DeviceToken
    {
        public Guid Id { get; set; }

        /// <summary>Who this device currently belongs to.</summary>
        public Guid UserId { get; set; }

        /// <summary>The gateway's address for the installation. Unique across the table.</summary>
        public string Token { get; set; } = default!;

        public DevicePlatform Platform { get; set; }

        public DateTime RegisteredAt { get; set; }

        /// <summary>
        /// Last time the app re-registered this token. Lets stale installations be pruned later without
        /// guessing from send failures alone.
        /// </summary>
        public DateTime LastSeenAt { get; set; }

        /// <summary>
        /// Set when the person signs out, or when the gateway reports the token as permanently invalid.
        /// Revoked rather than deleted so a re-registration can tell a returning device from a new one.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        public bool IsActive => RevokedAt is null;
    }
}
