namespace Booksy.ServiceCatalog.Domain.Enums
{
    /// <summary>
    /// A role a person holds <em>within a single organization</em>. A membership
    /// carries a set of these, so an owner who also provides services holds both
    /// <see cref="Owner"/> and <see cref="StaffProvider"/> simultaneously. These are
    /// organization-scoped and distinct from the global platform roles in UserManagement.
    /// </summary>
    public enum MembershipRole
    {
        /// <summary>Owns/administers the organization.</summary>
        Owner = 0,

        /// <summary>Manages the organization without necessarily owning it.</summary>
        Manager = 1,

        /// <summary>Provides services to customers (has a <c>StaffProfile</c>).</summary>
        StaffProvider = 2,

        /// <summary>Front-desk / scheduling role.</summary>
        Receptionist = 3,

        /// <summary>Organization-defined custom role.</summary>
        Custom = 4
    }
}
