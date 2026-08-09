using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.SetOwnerProvidesServices;

/// <summary>
/// Onboarding "Do you personally provide services?" answer. Creates (or updates) the
/// owner's membership: Yes ⇒ roles {Owner, StaffProvider} + StaffProfile (the owner
/// becomes the first active staff member, no invitation); No ⇒ {Owner} only.
/// </summary>
public sealed record SetOwnerProvidesServicesCommand(bool ProvidesServices)
    : ICommand<SetOwnerProvidesServicesResult>
{
    public Guid? IdempotencyKey { get; init; }
}

public sealed record SetOwnerProvidesServicesResult(
    Guid OrganizationId,
    Guid MembershipId,
    bool ProvidesServices,
    IReadOnlyCollection<string> Roles);
