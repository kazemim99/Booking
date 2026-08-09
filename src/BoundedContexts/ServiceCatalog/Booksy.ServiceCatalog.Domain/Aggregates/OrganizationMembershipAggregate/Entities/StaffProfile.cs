namespace Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities
{
    /// <summary>
    /// The service-providing part of a membership. It exists only while a membership
    /// provides services (i.e. holds the StaffProvider role). Because it is owned by
    /// the membership — not by a person or a provider — a person who works at two
    /// salons has two StaffProfiles (one per membership, each with its own schedule)
    /// while remaining a single person identity.
    /// </summary>
    /// <remarks>
    /// Phase 1 keeps this intentionally lean (the flag + an optional per-org bio).
    /// Service assignments and the per-org working schedule (validated within the
    /// organization's hours) are a follow-up task and are not modelled here yet.
    /// </remarks>
    public sealed class StaffProfile
    {
        public bool ProvidesServices { get; private set; }

        /// <summary>Optional bio shown for this person at this organization; overrides the person-level bio.</summary>
        public string? BioOverride { get; private set; }

        /// <summary>
        /// Name to show when the membership has no Person behind it yet — a staff
        /// member the salon added who does not use the app. Once they accept an
        /// invitation on their phone the membership gains a PersonId and the
        /// person's real name takes over.
        /// </summary>
        public string? DisplayName { get; private set; }

        // Private constructor for EF Core
        private StaffProfile() { }

        public static StaffProfile Create(
            bool providesServices = true,
            string? bioOverride = null,
            string? displayName = null)
        {
            return new StaffProfile
            {
                ProvidesServices = providesServices,
                BioOverride = string.IsNullOrWhiteSpace(bioOverride) ? null : bioOverride.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim()
            };
        }

        public void UpdateDisplayName(string? displayName) =>
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();

        public void SetProvidesServices(bool providesServices) => ProvidesServices = providesServices;

        public void UpdateBio(string? bio) =>
            BioOverride = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
    }
}
