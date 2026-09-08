using Booksy.Core.Domain.Exceptions;

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
    /// Carries the member's per-salon presentation (display name, bio, photo), their
    /// working week, and which of the salon's services they perform. Both of the latter
    /// default to empty and mean "the salon's hours" and "all of the salon's services" —
    /// so a salon that does not distinguish between its staff never has to fill them in.
    /// </remarks>
    public sealed class StaffProfile
    {
        private readonly List<StaffWorkingDay> _workingDays = new();
        private readonly List<Guid> _serviceIds = new();

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

        /// <summary>
        /// Photo shown for this member at this organization. Chiefly for a member with no
        /// account, who has no Person avatar of their own; for a claimed member it acts as
        /// a per-salon override of their personal avatar.
        /// </summary>
        public string? PhotoUrl { get; private set; }

        // Private constructor for EF Core
        private StaffProfile() { }

        public static StaffProfile Create(
            bool providesServices = true,
            string? bioOverride = null,
            string? displayName = null,
            string? photoUrl = null)
        {
            return new StaffProfile
            {
                ProvidesServices = providesServices,
                BioOverride = string.IsNullOrWhiteSpace(bioOverride) ? null : bioOverride.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim()
            };
        }

        public void UpdateDisplayName(string? displayName) =>
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();

        public void SetProvidesServices(bool providesServices) => ProvidesServices = providesServices;

        public void UpdateBio(string? bio) =>
            BioOverride = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

        public void UpdatePhotoUrl(string? photoUrl) =>
            PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();

        /// <summary>
        /// This member's working week at this salon. Empty means "the salon's own hours",
        /// which is the default and the common case.
        /// </summary>
        public IReadOnlyList<StaffWorkingDay> WorkingDays => _workingDays.AsReadOnly();

        /// <summary>
        /// The services this member performs. Empty means "everything the salon offers",
        /// so a salon that does not care about per-member specialisation never has to say so.
        /// </summary>
        public IReadOnlyList<Guid> ServiceIds => _serviceIds.AsReadOnly();

        /// <summary>True when this member keeps hours of their own rather than the salon's.</summary>
        public bool HasOwnSchedule => _workingDays.Count > 0;

        /// <summary>
        /// Replace the working week wholesale. At most one entry per day: a split shift is
        /// not modelled, and silently keeping two rows for one day would make availability
        /// generation ambiguous.
        /// </summary>
        public void SetWorkingDays(IEnumerable<StaffWorkingDay> days)
        {
            var replacement = (days ?? Enumerable.Empty<StaffWorkingDay>()).ToList();

            var duplicated = replacement
                .GroupBy(d => d.DayOfWeek)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicated is not null)
                throw new DomainValidationException(
                    nameof(WorkingDays),
                    $"A member can have only one working period per day; {duplicated.Key} was given more than once.");

            _workingDays.Clear();
            _workingDays.AddRange(replacement);
        }

        /// <summary>Replace the assigned services. Empty restores "performs everything".</summary>
        public void SetServiceIds(IEnumerable<Guid> serviceIds)
        {
            _serviceIds.Clear();
            _serviceIds.AddRange((serviceIds ?? Enumerable.Empty<Guid>()).Distinct());
        }

        /// <summary>
        /// Whether this member performs the given service. An empty assignment list means
        /// they perform all of the salon's services.
        /// </summary>
        public bool PerformsService(Guid serviceId) =>
            _serviceIds.Count == 0 || _serviceIds.Contains(serviceId);
    }
}
