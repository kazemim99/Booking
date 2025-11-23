using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Events;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// Represents a request from an individual provider to join an organization
    /// </summary>
    public sealed class ProviderJoinRequest : AggregateRoot<Guid>
    {
        /// <summary>
        /// The organization being requested to join
        /// </summary>
        public ProviderId OrganizationId { get; private set; }

        /// <summary>
        /// The individual provider requesting to join
        /// </summary>
        public ProviderId RequesterId { get; private set; }

        /// <summary>
        /// Optional message from the requester
        /// </summary>
        public string? Message { get; private set; }

        /// <summary>
        /// Current status of the request
        /// </summary>
        public JoinRequestStatus Status { get; private set; }

        /// <summary>
        /// When the request was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// When the request was reviewed (approved/rejected)
        /// </summary>
        public DateTime? ReviewedAt { get; private set; }

        /// <summary>
        /// ID of the user who reviewed the request
        /// </summary>
        public Guid? ReviewedBy { get; private set; }

        /// <summary>
        /// Optional note from the reviewer
        /// </summary>
        public string? ReviewNote { get; private set; }

        // Private constructor for EF Core
        private ProviderJoinRequest() : base() { }

        /// <summary>
        /// Creates a new join request
        /// </summary>
        public static ProviderJoinRequest Create(
            ProviderId organizationId,
            ProviderId requesterId,
            string? message = null)
        {
            var request = new ProviderJoinRequest
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RequesterId = requesterId,
                Message = message,
                Status = JoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            return request;
        }

        /// <summary>
        /// Approve the join request
        /// </summary>
        public void Approve(Guid reviewedBy, string? note = null)
        {
            if (Status != JoinRequestStatus.Pending)
                throw new DomainValidationException($"Cannot approve request with status {Status}");

            Status = JoinRequestStatus.Approved;
            ReviewedAt = DateTime.UtcNow;
            ReviewedBy = reviewedBy;
            ReviewNote = note;

            RaiseDomainEvent(new JoinRequestApprovedEvent(
                Id,
                OrganizationId,
                RequesterId,
                ReviewedAt.Value));
        }

        /// <summary>
        /// Reject the join request
        /// </summary>
        public void Reject(Guid reviewedBy, string? note = null)
        {
            if (Status != JoinRequestStatus.Pending)
                throw new DomainValidationException($"Cannot reject request with status {Status}");

            Status = JoinRequestStatus.Rejected;
            ReviewedAt = DateTime.UtcNow;
            ReviewedBy = reviewedBy;
            ReviewNote = note;
        }

        /// <summary>
        /// Withdraw the join request (by requester)
        /// </summary>
        public void Withdraw()
        {
            if (Status != JoinRequestStatus.Pending)
                throw new DomainValidationException($"Cannot withdraw request with status {Status}");

            Status = JoinRequestStatus.Withdrawn;
            ReviewedAt = DateTime.UtcNow;
        }
    }
}
