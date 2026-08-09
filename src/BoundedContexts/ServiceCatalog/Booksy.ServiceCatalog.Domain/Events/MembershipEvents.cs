using Booksy.Core.Domain.Base;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Events
{
    /// <summary>Raised when a person is invited to an organization (membership created as Invited).</summary>
    public sealed record MembershipInvitedEvent(
        Guid MembershipId,
        ProviderId OrganizationId,
        Guid? PersonId,
        DateTime InvitedAt) : DomainEvent;

    /// <summary>Raised when an invited person accepts and the membership becomes Active.</summary>
    public sealed record MembershipAcceptedEvent(
        Guid MembershipId,
        ProviderId OrganizationId,
        Guid PersonId,
        DateTime JoinedAt) : DomainEvent;

    /// <summary>Raised when a membership is created already-active (e.g. the owner during onboarding).</summary>
    public sealed record MembershipActivatedEvent(
        Guid MembershipId,
        ProviderId OrganizationId,
        Guid? PersonId,
        DateTime ActivatedAt) : DomainEvent;

    /// <summary>Raised when a membership's organization-scoped roles change.</summary>
    public sealed record MembershipRoleChangedEvent(
        Guid MembershipId,
        ProviderId OrganizationId,
        DateTime ChangedAt) : DomainEvent;

    /// <summary>Raised when a membership starts providing services (StaffProfile enabled).</summary>
    public sealed record StaffProfileEnabledEvent(
        Guid MembershipId,
        ProviderId OrganizationId,
        DateTime EnabledAt) : DomainEvent;

    /// <summary>Raised when a membership is terminated (the person leaves the organization).</summary>
    public sealed record MembershipTerminatedEvent(
        Guid MembershipId,
        ProviderId OrganizationId,
        Guid? PersonId,
        string? Reason,
        DateTime LeftAt) : DomainEvent;

    /// <summary>Raised when a suspended/terminated membership is reinstated to Active.</summary>
    public sealed record MembershipReinstatedEvent(
        Guid MembershipId,
        ProviderId OrganizationId,
        DateTime ReinstatedAt) : DomainEvent;
}
