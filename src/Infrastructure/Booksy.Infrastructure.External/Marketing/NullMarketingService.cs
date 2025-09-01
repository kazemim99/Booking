
// ========================================
// Storage/IBlobStorage.cs
// ========================================
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Marketing;


public class NullMarketingService : IMarketingService
{
    private readonly ILogger<NullMarketingService> _logger;

    public NullMarketingService(ILogger<NullMarketingService> logger)
    {
        _logger = logger;
    }

    public Task EnrollInCampaignAsync(string userId, string email, string campaignId, string[]? tags = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marketing: Enrolling {Email} in campaign {CampaignId}", email, campaignId);
        return Task.CompletedTask;
    }

    public Task UpdateUserTagsAsync(string userId, string[] tags, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marketing: Updating tags for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task SendTransactionalEmailAsync(string to, string templateId, Dictionary<string, object> data, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marketing: Sending transactional email to {To} with template {TemplateId}", to, templateId);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marketing: Unsubscribing {Email}", email);
        return Task.CompletedTask;
    }
}

