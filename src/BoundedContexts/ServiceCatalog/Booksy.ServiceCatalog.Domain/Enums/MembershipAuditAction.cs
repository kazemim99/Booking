namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// The membership lifecycle events worth an audit record. Values are persisted
    /// by name — append new members, never renumber or rename existing ones.
    /// </summary>
    public enum MembershipAuditAction
    {
        /// <summary>A person was invited to the organization.</summary>
        Invited = 0,

        /// <summary>The invitation was accepted; the membership became active.</summary>
        Accepted = 1,

        /// <summary>An owner membership was created directly (onboarding).</summary>
        OwnerCreated = 2,

        /// <summary>Organization-scoped roles were replaced.</summary>
        RolesChanged = 3,

        /// <summary>The member started providing services (StaffProfile enabled).</summary>
        StaffProfileEnabled = 4,

        /// <summary>The member stopped providing services.</summary>
        StaffProfileDisabled = 5,

        /// <summary>The membership was suspended.</summary>
        Suspended = 6,

        /// <summary>The membership ended (left or was removed).</summary>
        Terminated = 7,

        /// <summary>A suspended/terminated membership was restored.</summary>
        Reinstated = 8,

        /// <summary>A pending invitation was revoked by the organization.</summary>
        InvitationRevoked = 9,

        /// <summary>A pending invitation lapsed without a response.</summary>
        InvitationExpired = 10,

        /// <summary>The organization added a member directly (no invitation round-trip).</summary>
        MemberAdded = 11
    }
}
