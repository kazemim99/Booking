using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates.MembershipAuditAggregate
{
    /// <summary>
    /// An immutable record of one thing that happened to a membership.
    ///
    /// Membership changes decide who can act for a business and who gets paid for
    /// work, so every transition must be attributable: WHO did it, WHAT changed,
    /// WHEN, and WHY. Entries are append-only — there is no update or delete, and
    /// terminating a membership never erases its history.
    /// </summary>
    public sealed class MembershipAuditEntry : AggregateRoot<Guid>
    {
        /// <summary>The membership, once one exists. Null for invitation-stage events.</summary>
        public Guid? MembershipId { get; private set; }

        /// <summary>The invitation, for events that happened before a membership existed.</summary>
        public Guid? InvitationId { get; private set; }
        public ProviderId OrganizationId { get; private set; } = default!;

        /// <summary>The person the membership belongs to (null for unclaimed members).</summary>
        public UserId? SubjectPersonId { get; private set; }

        public MembershipAuditAction Action { get; private set; }

        /// <summary>Who performed it. Null when the system acted (expiry sweeps, migrations).</summary>
        public UserId? ActorPersonId { get; private set; }

        /// <summary>Roles after the change, as persisted on the membership (CSV of role names).</summary>
        public string? RolesSnapshot { get; private set; }

        /// <summary>Membership status after the change.</summary>
        public MembershipStatus StatusAfter { get; private set; }

        /// <summary>Free-text reason (termination reason, revocation note…).</summary>
        public string? Reason { get; private set; }

        public DateTime OccurredAt { get; private set; }

        // EF Core
        private MembershipAuditEntry() : base() { }

        public static MembershipAuditEntry Record(
            Guid? membershipId,
            ProviderId organizationId,
            MembershipAuditAction action,
            MembershipStatus statusAfter,
            UserId? subjectPersonId = null,
            UserId? actorPersonId = null,
            IEnumerable<MembershipRole>? roles = null,
            string? reason = null,
            Guid? invitationId = null)
        {
            ArgumentNullException.ThrowIfNull(organizationId);

            return new MembershipAuditEntry
            {
                Id = Guid.NewGuid(),
                MembershipId = membershipId,
                InvitationId = invitationId,
                OrganizationId = organizationId,
                SubjectPersonId = subjectPersonId,
                Action = action,
                ActorPersonId = actorPersonId,
                RolesSnapshot = roles is null ? null : string.Join(',', roles.Select(r => r.ToString())),
                StatusAfter = statusAfter,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                OccurredAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Records an invitation-stage event (revoked, expired) — the person has no
        /// membership yet, so the entry is keyed by the invitation instead.
        /// </summary>
        public static MembershipAuditEntry RecordInvitation(
            Guid invitationId,
            ProviderId organizationId,
            MembershipAuditAction action,
            UserId? actorPersonId = null,
            string? reason = null)
        {
            return Record(
                membershipId: null,
                organizationId: organizationId,
                action: action,
                statusAfter: MembershipStatus.Terminated, // no membership resulted
                subjectPersonId: null,
                actorPersonId: actorPersonId,
                roles: null,
                reason: reason,
                invitationId: invitationId);
        }
    }
}
