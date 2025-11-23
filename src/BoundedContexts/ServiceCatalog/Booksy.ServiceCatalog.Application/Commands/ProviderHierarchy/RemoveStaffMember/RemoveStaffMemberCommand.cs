using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.RemoveStaffMember
{
    public sealed record RemoveStaffMemberCommand(
        Guid OrganizationId,
        Guid StaffProviderId,
        string Reason,
        Guid? IdempotencyKey = null) : ICommand<RemoveStaffMemberResult>;

    public sealed record RemoveStaffMemberResult(
        Guid OrganizationId,
        Guid StaffProviderId,
        DateTime RemovedAt);
}
