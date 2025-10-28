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

public record ProviderInfo(
    Guid ProviderId,
    string Status);
