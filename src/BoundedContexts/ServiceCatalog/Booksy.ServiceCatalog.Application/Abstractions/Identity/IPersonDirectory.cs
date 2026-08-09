namespace Booksy.ServiceCatalog.Application.Abstractions.Identity
{
    /// <summary>
    /// A person resolved from the shared identity store (UserManagement).
    /// <see cref="Status"/> is the UserStatus name (e.g. "Active", "Banned", "Inactive").
    /// </summary>
    public sealed record PersonInfo(
        Guid PersonId,
        string? FirstName,
        string? LastName,
        string? PhoneNumber,
        string Status);

    /// <summary>
    /// Read-only lookup of people (UserManagement users) from ServiceCatalog, so the
    /// invitation flow can reuse an existing account by phone instead of ever creating
    /// a duplicate person. This is the one place ServiceCatalog reads identity; it is
    /// an in-process read over the shared monolith database (see the
    /// <c>ProviderClientsReadService</c> seam), not an HTTP hop.
    /// </summary>
    public interface IPersonDirectory
    {
        /// <summary>
        /// Resolve a person by phone (accepts any form; normalized internally to the
        /// canonical E.164 value and matched against both the stored value and national
        /// number). Excludes soft-deleted accounts. Returns null when no person matches.
        /// </summary>
        Task<PersonInfo?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch-resolve people by id, keyed by person id, so a staff list can show
        /// real names/phones for its memberships in one round-trip. Ids not found (or
        /// soft-deleted) are simply absent from the result.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, PersonInfo>> FindByIdsAsync(
            IReadOnlyCollection<Guid> personIds,
            CancellationToken cancellationToken = default);
    }
}
