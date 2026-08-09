using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate.Entities;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate
{
    /// <summary>
    /// The link between a <em>person</em> (a UserManagement user) and an
    /// <em>organization</em> (a Provider). This is the single concept that expresses
    /// "works here": a person may hold many memberships (multi-salon), an organization
    /// may have many members, and each membership carries its own organization-scoped
    /// roles, lifecycle, and — when providing services — a <see cref="StaffProfile"/>.
    ///
    /// It replaces the previous "staff = a second Provider linked by ParentProviderId"
    /// mechanism, so changing salons, being owner-and-staff at once, and belonging to
    /// several salons no longer require duplicate accounts or provider rows.
    ///
    /// Organization-wide invariants that a single membership cannot see on its own —
    /// "an organization always keeps at least one Owner" and "at most one live
    /// membership per (person, organization)" — are enforced by the application layer
    /// (command handlers / repository), not inside this aggregate.
    /// </summary>
    public sealed class OrganizationMembership : AggregateRoot<Guid>
    {
        private readonly HashSet<MembershipRole> _roles = new();

        /// <summary>The person this membership belongs to. Null only for a not-yet-claimed staff record migrated from legacy data.</summary>
        public UserId? PersonId { get; private set; }

        /// <summary>The organization the person is a member of.</summary>
        public ProviderId OrganizationId { get; private set; } = default!;

        public MembershipStatus Status { get; private set; }

        /// <summary>Organization-scoped roles held by this membership (never empty for a usable membership).</summary>
        public IReadOnlyCollection<MembershipRole> Roles => _roles;

        /// <summary>Present only while this membership provides services.</summary>
        public StaffProfile? StaffProfile { get; private set; }

        // CreatedAt / CreatedBy / LastModifiedAt come from the auditable Entity base
        // and are stamped by the DbContext; do not redeclare them here.
        public DateTime? InvitedAt { get; private set; }
        public DateTime? JoinedAt { get; private set; }
        public DateTime? LeftAt { get; private set; }
        public string? TerminationReason { get; private set; }

        // Convenience predicates
        public bool IsOwner => _roles.Contains(MembershipRole.Owner);
        public bool ProvidesServices => _roles.Contains(MembershipRole.StaffProvider) && StaffProfile is { ProvidesServices: true };
        public bool IsActive => Status == MembershipStatus.Active;

        // Private constructor for EF Core
        private OrganizationMembership() : base() { }

        /// <summary>
        /// Invite an existing person to join an organization. The membership starts as
        /// <see cref="MembershipStatus.Invited"/> and becomes active only on <see cref="Accept"/>.
        /// Defaults to the StaffProvider role (the usual reason to invite someone).
        /// </summary>
        public static OrganizationMembership InviteExisting(
            UserId personId,
            ProviderId organizationId,
            IEnumerable<MembershipRole>? roles = null)
        {
            ArgumentNullException.ThrowIfNull(personId);
            ArgumentNullException.ThrowIfNull(organizationId);

            var membership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                OrganizationId = organizationId,
                Status = MembershipStatus.Invited,
                InvitedAt = DateTime.UtcNow
            };

            membership.SetRoles(roles ?? new[] { MembershipRole.StaffProvider });

            membership.RaiseDomainEvent(new MembershipInvitedEvent(
                membership.Id, organizationId, personId.Value, membership.InvitedAt.Value));

            return membership;
        }

        /// <summary>
        /// Create the owner's membership during onboarding. If the owner personally
        /// provides services they also get the StaffProvider role and a StaffProfile,
        /// becoming the first active staff member — with no invitation.
        /// </summary>
        public static OrganizationMembership CreateOwner(
            UserId personId,
            ProviderId organizationId,
            bool providesServices)
        {
            ArgumentNullException.ThrowIfNull(personId);
            ArgumentNullException.ThrowIfNull(organizationId);

            var roles = new List<MembershipRole> { MembershipRole.Owner };
            if (providesServices)
                roles.Add(MembershipRole.StaffProvider);

            var membership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                OrganizationId = organizationId,
                Status = MembershipStatus.Active,
                JoinedAt = DateTime.UtcNow
            };

            membership.SetRoles(roles);

            if (providesServices)
                membership.StaffProfile = StaffProfile.Create(providesServices: true);

            membership.RaiseDomainEvent(new MembershipActivatedEvent(
                membership.Id, organizationId, personId.Value, membership.JoinedAt.Value));

            return membership;
        }

        /// <summary>
        /// Adds a staff member the salon manages on the person's behalf — someone who
        /// does not (yet) use the app, so there is no Person to link. They are a real,
        /// bookable member identified by <paramref name="displayName"/>; when they
        /// later accept an invitation on their phone the membership is claimed via
        /// <see cref="ClaimBy"/> and their account takes over the identity.
        /// </summary>
        public static OrganizationMembership CreateUnclaimed(
            ProviderId organizationId,
            string displayName,
            bool providesServices = true)
        {
            ArgumentNullException.ThrowIfNull(organizationId);
            if (string.IsNullOrWhiteSpace(displayName))
                throw new DomainValidationException("A staff member added without an account needs a name.");

            var membership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                PersonId = null,                 // unclaimed — no account behind it yet
                OrganizationId = organizationId,
                Status = MembershipStatus.Active,
                JoinedAt = DateTime.UtcNow
            };

            membership.SetRoles(new[] { MembershipRole.StaffProvider });
            membership.StaffProfile = StaffProfile.Create(
                providesServices: providesServices, displayName: displayName);

            membership.RaiseDomainEvent(new MembershipActivatedEvent(
                membership.Id, organizationId, null, membership.JoinedAt.Value));

            return membership;
        }

        /// <summary>
        /// Attaches a real account to an unclaimed membership, preserving its history
        /// (and any bookings already made against it).
        /// </summary>
        public void ClaimBy(UserId personId)
        {
            ArgumentNullException.ThrowIfNull(personId);
            if (PersonId is not null)
                throw new DomainValidationException("This membership already belongs to a person.");

            EnsureNotTerminated(nameof(ClaimBy));
            PersonId = personId;
        }

        /// <summary>True when nobody has claimed this membership with an account yet.</summary>
        public bool IsUnclaimed => PersonId is null;

        /// <summary>
        /// Accept a pending invitation, activating the membership. Used by both the
        /// existing-user and the register-and-accept flows (the person exists by now).
        /// </summary>
        public void Accept()
        {
            EnsureStatus(MembershipStatus.Invited, nameof(Accept));

            if (PersonId is null)
                throw new DomainValidationException("Cannot accept a membership that has no person linked.");

            Status = MembershipStatus.Active;
            JoinedAt = DateTime.UtcNow;

            RaiseDomainEvent(new MembershipAcceptedEvent(
                Id, OrganizationId, PersonId.Value, JoinedAt.Value));
        }

        /// <summary>Add an organization-scoped role (idempotent).</summary>
        public void AssignRole(MembershipRole role)
        {
            EnsureNotTerminated(nameof(AssignRole));

            if (_roles.Add(role))
                RaiseDomainEvent(new MembershipRoleChangedEvent(Id, OrganizationId, DateTime.UtcNow));
        }

        /// <summary>Remove an organization-scoped role. A membership must always retain at least one role.</summary>
        public void RemoveRole(MembershipRole role)
        {
            EnsureNotTerminated(nameof(RemoveRole));

            if (!_roles.Contains(role))
                return;

            if (_roles.Count == 1)
                throw new DomainValidationException("A membership must retain at least one role; terminate it instead.");

            _roles.Remove(role);

            // Removing the service-providing role retires the StaffProfile too.
            if (role == MembershipRole.StaffProvider)
                StaffProfile = null;

            RaiseDomainEvent(new MembershipRoleChangedEvent(Id, OrganizationId, DateTime.UtcNow));
        }

        /// <summary>Start providing services: grant the StaffProvider role and attach a StaffProfile.</summary>
        public void EnableStaffProfile(string? bio = null)
        {
            EnsureNotTerminated(nameof(EnableStaffProfile));

            _roles.Add(MembershipRole.StaffProvider);
            StaffProfile ??= StaffProfile.Create(providesServices: true, bioOverride: bio);
            StaffProfile.SetProvidesServices(true);
            if (bio is not null)
                StaffProfile.UpdateBio(bio);

            RaiseDomainEvent(new StaffProfileEnabledEvent(Id, OrganizationId, DateTime.UtcNow));
        }

        /// <summary>Stop providing services: drop the StaffProvider role and StaffProfile (owner/manager roles remain).</summary>
        public void DisableStaffProfile()
        {
            EnsureNotTerminated(nameof(DisableStaffProfile));

            if (_roles.Count == 1 && _roles.Contains(MembershipRole.StaffProvider))
                throw new DomainValidationException("Cannot disable the only role of a membership; terminate it instead.");

            _roles.Remove(MembershipRole.StaffProvider);
            StaffProfile = null;

            RaiseDomainEvent(new MembershipRoleChangedEvent(Id, OrganizationId, DateTime.UtcNow));
        }

        /// <summary>
        /// Replace this membership's organization-scoped roles wholesale. Keeps the
        /// StaffProfile in sync with the StaffProvider role and forbids an empty set
        /// (terminate instead). The organization-wide "keep ≥1 owner" rule is enforced
        /// by the application layer, which can see all of the org's memberships.
        /// </summary>
        public void ChangeRoles(IEnumerable<MembershipRole> roles)
        {
            EnsureNotTerminated(nameof(ChangeRoles));

            var newRoles = roles?.ToHashSet() ?? new HashSet<MembershipRole>();
            if (newRoles.Count == 0)
                throw new DomainValidationException("A membership must retain at least one role; terminate it instead.");

            SetRoles(newRoles);

            if (newRoles.Contains(MembershipRole.StaffProvider))
                StaffProfile ??= StaffProfile.Create(providesServices: true);
            else
                StaffProfile = null;

            RaiseDomainEvent(new MembershipRoleChangedEvent(Id, OrganizationId, DateTime.UtcNow));
        }

        /// <summary>Temporarily suspend an active member (retains history; not bookable).</summary>
        public void Suspend()
        {
            EnsureStatus(MembershipStatus.Active, nameof(Suspend));
            Status = MembershipStatus.Suspended;
        }

        /// <summary>Terminate the membership (the person leaves the organization). Retained as history.</summary>
        public void Terminate(string? reason = null)
        {
            if (Status == MembershipStatus.Terminated)
                throw new DomainValidationException("Membership is already terminated.");

            Status = MembershipStatus.Terminated;
            LeftAt = DateTime.UtcNow;
            TerminationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            RaiseDomainEvent(new MembershipTerminatedEvent(
                Id, OrganizationId, PersonId?.Value, TerminationReason, LeftAt.Value));
        }

        /// <summary>Reinstate a suspended or terminated membership to Active.</summary>
        public void Reinstate()
        {
            if (Status is not (MembershipStatus.Suspended or MembershipStatus.Terminated))
                throw new DomainValidationException($"Cannot reinstate a membership with status {Status}.");

            Status = MembershipStatus.Active;
            LeftAt = null;
            TerminationReason = null;
            if (JoinedAt is null)
                JoinedAt = DateTime.UtcNow;

            RaiseDomainEvent(new MembershipReinstatedEvent(Id, OrganizationId, DateTime.UtcNow));
        }

        private void SetRoles(IEnumerable<MembershipRole> roles)
        {
            _roles.Clear();
            foreach (var role in roles)
                _roles.Add(role);

            if (_roles.Count == 0)
                throw new DomainValidationException("A membership must have at least one role.");
        }

        private void EnsureStatus(MembershipStatus expected, string operation)
        {
            if (Status != expected)
                throw new DomainValidationException($"Cannot {operation} a membership with status {Status}; expected {expected}.");
        }

        private void EnsureNotTerminated(string operation)
        {
            if (Status == MembershipStatus.Terminated)
                throw new DomainValidationException($"Cannot {operation} on a terminated membership.");
        }
    }
}
