using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// Represents an invitation from an organization to an individual to join as staff
    /// </summary>
    public sealed class ProviderInvitation : AggregateRoot<Guid>
    {
        private const int DefaultExpirationDays = 7;

        /// <summary>
        /// The organization sending the invitation
        /// </summary>
        public ProviderId OrganizationId { get; private set; }

        /// <summary>
        /// Phone number of the invitee
        /// </summary>
        public PhoneNumber PhoneNumber { get; private set; }

        /// <summary>
        /// Optional name of the invitee
        /// </summary>
        public string? InviteeName { get; private set; }

        /// <summary>
        /// Optional message from the organization
        /// </summary>
        public string? Message { get; private set; }

        /// <summary>
        /// Current status of the invitation
        /// </summary>
        public InvitationStatus Status { get; private set; }

        /// <summary>
        /// When the invitation was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// When the invitation expires
        /// </summary>
        public DateTime ExpiresAt { get; private set; }

        /// <summary>
        /// When the invitation was accepted/rejected (if applicable)
        /// </summary>
        public DateTime? RespondedAt { get; private set; }

        /// <summary>
        /// The provider ID created when invitation is accepted
        /// </summary>
        public ProviderId? AcceptedByProviderId { get; private set; }

        // Private constructor for EF Core
        private ProviderInvitation() : base() { }

        /// <summary>
        /// Creates a new invitation
        /// </summary>
        public static ProviderInvitation Create(
            ProviderId organizationId,
            PhoneNumber phoneNumber,
            string? inviteeName = null,
            string? message = null,
            int expirationDays = DefaultExpirationDays)
        {
            var invitation = new ProviderInvitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                PhoneNumber = phoneNumber,
                InviteeName = inviteeName,
                Message = message,
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
            };

            invitation.RaiseDomainEvent(new InvitationSentEvent(
                invitation.Id,
                organizationId,
                phoneNumber.Value,
                invitation.CreatedAt));

            return invitation;
        }

        /// <summary>
        /// Accept the invitation and link to a provider
        /// </summary>
        public void Accept(ProviderId individualProviderId)
        {
            if (Status != InvitationStatus.Pending)
                throw new DomainValidationException($"Cannot accept invitation with status {Status}");

            if (DateTime.UtcNow > ExpiresAt)
            {
                Status = InvitationStatus.Expired;
                throw new DomainValidationException("Invitation has expired");
            }

            Status = InvitationStatus.Accepted;
            RespondedAt = DateTime.UtcNow;
            AcceptedByProviderId = individualProviderId;

            RaiseDomainEvent(new InvitationAcceptedEvent(
                Id,
                OrganizationId,
                individualProviderId,
                RespondedAt.Value));
        }

        /// <summary>
        /// Reject the invitation
        /// </summary>
        public void Reject()
        {
            if (Status != InvitationStatus.Pending)
                throw new DomainValidationException($"Cannot reject invitation with status {Status}");

            Status = InvitationStatus.Rejected;
            RespondedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancel the invitation (by organization)
        /// </summary>
        public void Cancel()
        {
            if (Status != InvitationStatus.Pending)
                throw new DomainValidationException($"Cannot cancel invitation with status {Status}");

            Status = InvitationStatus.Cancelled;
            RespondedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Mark invitation as expired (typically done by a background job)
        /// </summary>
        public void MarkAsExpired()
        {
            if (Status == InvitationStatus.Pending && DateTime.UtcNow > ExpiresAt)
            {
                Status = InvitationStatus.Expired;
            }
        }

        /// <summary>
        /// Check if invitation is still valid
        /// </summary>
        public bool IsValid()
        {
            return Status == InvitationStatus.Pending && DateTime.UtcNow <= ExpiresAt;
        }
    }
}
