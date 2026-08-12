namespace Booksy.UserManagement.Application.Services.Interfaces;

/// <summary>
/// Service for querying provider information from ServiceCatalog
/// </summary>
public interface IProviderInfoService
{
    /// <summary>
    /// Get provider information by user/owner ID
    /// </summary>
    Task<ProviderInfo?> GetProviderByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// A provider profile as seen from UserManagement, resolved through
/// <see cref="IProviderInfoService"/>.
/// </summary>
/// <param name="Status">
/// ServiceCatalog's provider status name. Carried as a string on purpose: the
/// ProviderStatus enum belongs to ServiceCatalog's domain and UserManagement does not
/// (and should not) reference it. Compare against <see cref="DraftedStatus"/> rather
/// than a literal.
/// </param>
public record ProviderInfo(
    Guid ProviderId,
    string Status)
{
    /// <summary>
    /// The one status meaning business onboarding is still unfinished. Every other
    /// ServiceCatalog status (PendingVerification, Verified, Active, Inactive,
    /// Suspended, Archived) represents a completed registration.
    /// </summary>
    public const string DraftedStatus = "Drafted";

    /// <summary>True while the provider still has onboarding to complete.</summary>
    public bool RequiresOnboarding =>
        string.Equals(Status, DraftedStatus, StringComparison.OrdinalIgnoreCase);
}
