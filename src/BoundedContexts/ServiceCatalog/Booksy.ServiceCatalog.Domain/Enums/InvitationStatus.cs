namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Status of a provider invitation
    /// </summary>
    public enum InvitationStatus
    {
        /// <summary>
        /// Invitation has been sent and is awaiting response
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Invitee has accepted the invitation
        /// </summary>
        Accepted = 1,

        /// <summary>
        /// Invitation has expired (typically after 7 days)
        /// </summary>
        Expired = 2,

        /// <summary>
        /// Invitee has rejected the invitation
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// Organization has cancelled the invitation
        /// </summary>
        Cancelled = 4
    }
}
