namespace Booksy.Core.Application.Authorization
{
    /// <summary>
    /// The set of user identities that are considered "owners" of a resource for
    /// authorization purposes. A caller matching any populated id is authorized.
    /// </summary>
    /// <param name="CustomerId">The owning customer's user id, if any.</param>
    /// <param name="ProviderOwnerUserId">The owning provider's owner user id, if any.</param>
    public sealed record ResourceOwners(Guid? CustomerId, Guid? ProviderOwnerUserId);

    /// <summary>
    /// Resolves the owning identities for the resource targeted by a command.
    /// Implemented per bounded context (e.g. booking → CustomerId/ProviderOwner,
    /// payment → CustomerId/ProviderOwner). Returning <c>null</c> means the resource
    /// was not found, which the behavior treats as a denial (fail closed).
    /// </summary>
    public interface IResourceOwnershipResolver<in TCommand>
    {
        Task<ResourceOwners?> ResolveAsync(TCommand command, CancellationToken cancellationToken);
    }
}
