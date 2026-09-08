using Booksy.Core.Application.Abstractions.CQRS;
using DomainDayOfWeek = Booksy.ServiceCatalog.Domain.Enums.DayOfWeek;

namespace Booksy.ServiceCatalog.Application.Commands.Membership.UpdateMembership;

/// <summary>
/// Edit a member's organization-scoped details: the salon's display name for them
/// (unclaimed members only), their per-salon bio, whether they currently provide
/// services to customers, the days they work here, and which services they perform.
/// </summary>
/// <remarks>
/// Roles are deliberately NOT part of this command — <c>ChangeMembershipRolesCommand</c>
/// owns them, because changing roles carries the "keep ≥1 owner" invariant that needs to
/// see every membership in the organization.
///
/// Person-level fields (first/last name, email, phone) are not editable here at all: they
/// belong to the Person in UserManagement, not to the salon. See
/// <c>OrganizationMembership.UpdateStaffDetails</c>.
///
/// Schedule and service assignments are per-MEMBERSHIP, which is what lets one person
/// work Tue–Thu at one salon and Fri–Sat at another without a second identity.
/// </remarks>
/// <param name="WorkingDays">
/// The member's working week AT THIS SALON. Null leaves it unchanged; an EMPTY list puts
/// them back on the salon's own opening hours. Changing it regenerates their unbooked
/// future availability.
/// </param>
/// <param name="ServiceIds">
/// The services this member performs. Null leaves it unchanged; an EMPTY list means they
/// perform everything the salon offers.
/// </param>
public sealed record UpdateMembershipCommand(
    Guid MembershipId,
    string? DisplayName = null,
    string? BioOverride = null,
    bool? ProvidesServices = null,
    string? PhotoUrl = null,
    IReadOnlyList<MembershipWorkingDayInput>? WorkingDays = null,
    IReadOnlyList<Guid>? ServiceIds = null) : ICommand<UpdateMembershipResult>
{
    public Guid? IdempotencyKey { get; init; }
}

/// <summary>One day of a member's working week at a salon.</summary>
public sealed record MembershipWorkingDayInput(
    DomainDayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record UpdateMembershipResult(
    Guid MembershipId,
    Guid OrganizationId,
    Guid? PersonId,
    string? DisplayName,
    string? BioOverride,
    string? PhotoUrl,
    bool ProvidesServices,
    IReadOnlyCollection<string> Roles,
    IReadOnlyList<MembershipWorkingDayInput> WorkingDays,
    IReadOnlyList<Guid> ServiceIds);
