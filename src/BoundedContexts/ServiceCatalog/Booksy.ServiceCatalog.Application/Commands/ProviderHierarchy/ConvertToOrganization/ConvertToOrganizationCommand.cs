using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.ProviderHierarchy.ConvertToOrganization
{
    public sealed record ConvertToOrganizationCommand(
        Guid ProviderId,
        Guid? IdempotencyKey = null) : ICommand<ConvertToOrganizationResult>;

    public sealed record ConvertToOrganizationResult(
        Guid ProviderId,
        string HierarchyType,
        DateTime ConvertedAt);
}
