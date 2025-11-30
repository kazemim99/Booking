using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Services.Interfaces;

/// <summary>
/// Service for cloning provider data (services, hours, gallery) from organization to individual
/// </summary>
public interface IDataCloningService
{
    /// <summary>
    /// Clones all services from source provider to target provider
    /// Returns count of cloned services
    /// </summary>
    Task<int> CloneServicesAsync(
        ProviderId sourceProviderId,
        ProviderId targetProviderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones working hours/business hours from source provider to target provider
    /// Returns count of cloned hour entries
    /// </summary>
    Task<int> CloneWorkingHoursAsync(
        ProviderId sourceProviderId,
        ProviderId targetProviderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones gallery images from source provider to target provider
    /// Images are marked with IsCloned=true and source metadata
    /// Returns count of cloned images
    /// </summary>
    Task<int> CloneGalleryAsync(
        ProviderId sourceProviderId,
        ProviderId targetProviderId,
        bool markAsCloned = true,
        CancellationToken cancellationToken = default);
}
