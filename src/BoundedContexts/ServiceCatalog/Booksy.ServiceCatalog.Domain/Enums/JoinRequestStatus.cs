namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Status of a join request from an individual to an organization
    /// </summary>
    public enum JoinRequestStatus
    {
        /// <summary>
        /// Request has been submitted and is awaiting organization review
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Organization has approved the request
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Organization has rejected the request
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Requester has withdrawn their request
        /// </summary>
        Withdrawn = 3
    }
}
