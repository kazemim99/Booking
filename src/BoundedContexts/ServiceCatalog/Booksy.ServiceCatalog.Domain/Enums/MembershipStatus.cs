namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// Lifecycle status of an <c>OrganizationMembership</c> — the link between a
    /// person and an organization. Distinct from the platform-wide user status.
    /// </summary>
    public enum MembershipStatus
    {
        /// <summary>Invited but not yet accepted (the person has not joined).</summary>
        Invited = 0,

        /// <summary>Active member of the organization.</summary>
        Active = 1,

        /// <summary>Temporarily suspended; retains history, cannot be booked.</summary>
        Suspended = 2,

        /// <summary>Left the organization. Retained as history; a fresh membership is created on rejoin.</summary>
        Terminated = 3
    }
}
