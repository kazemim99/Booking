// ========================================
// Storage/IBlobStorage.cs
// ========================================
namespace Booksy.Infrastructure.External.Marketing;

public interface IMarketingService
{
    /// <summary>
    /// Enrolls a user in a marketing campaign
    /// </summary>
    Task EnrollInCampaignAsync(string userId, string email, string campaignId, string[]? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user tags
    /// </summary>
    Task UpdateUserTagsAsync(string userId, string[] tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a transactional email
    /// </summary>
    Task SendTransactionalEmailAsync(string to, string templateId, Dictionary<string, object> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes a user
    /// </summary>
    Task UnsubscribeAsync(string email, CancellationToken cancellationToken = default);
}

