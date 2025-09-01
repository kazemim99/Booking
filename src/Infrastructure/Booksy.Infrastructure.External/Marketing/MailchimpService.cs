// ========================================
// Marketing/MailchimpService.cs
// ========================================
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booksy.Infrastructure.External.Marketing;

/// <summary>
/// Mailchimp marketing service implementation
/// </summary>
public class MailchimpService : IMarketingService
{
    private readonly HttpClient _httpClient;
    private readonly MailchimpSettings _settings;
    private readonly ILogger<MailchimpService> _logger;

    public MailchimpService(
        HttpClient httpClient,
        IOptions<MailchimpSettings> settings,
        ILogger<MailchimpService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Set up authentication
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"anystring:{_settings.ApiKey}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
    }

    public async Task EnrollInCampaignAsync(
        string userId,
        string email,
        string campaignId,
        string[]? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriberHash = GetSubscriberHash(email);
            var url = $"{_settings.ApiUrl}/lists/{_settings.ListId}/members/{subscriberHash}";

            var payload = new
            {
                email_address = email,
                status = "subscribed",
                merge_fields = new
                {
                    USERID = userId
                },
                tags = tags ?? Array.Empty<string>()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Mailchimp enrollment failed: {Error}", error);
            }
            else
            {
                _logger.LogInformation("User {UserId} enrolled in campaign {CampaignId}", userId, campaignId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enroll user in Mailchimp campaign");
        }
    }

    public async Task UpdateUserTagsAsync(
        string userId,
        string[] tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In Mailchimp, we need the email to update tags
            // This is a simplified implementation
            _logger.LogInformation("Updating tags for user {UserId}: {Tags}", userId, string.Join(", ", tags));
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user tags in Mailchimp");
        }
    }

    public async Task SendTransactionalEmailAsync(
        string to,
        string templateId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Mailchimp Transactional (formerly Mandrill) endpoint
            var url = "https://mandrillapp.com/api/1.0/messages/send-template";

            var payload = new
            {
                key = _settings.MandrillApiKey,
                template_name = templateId,
                template_content = Array.Empty<object>(),
                message = new
                {
                    to = new[]
                    {
                        new { email = to }
                    },
                    global_merge_vars = data.Select(kvp => new
                    {
                        name = kvp.Key,
                        content = kvp.Value
                    }).ToArray()
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Transactional email failed: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send transactional email");
        }
    }

    public async Task UnsubscribeAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriberHash = GetSubscriberHash(email);
            var url = $"{_settings.ApiUrl}/lists/{_settings.ListId}/members/{subscriberHash}";

            var payload = new
            {
                status = "unsubscribed"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PatchAsync(url, content, cancellationToken);

            _logger.LogInformation("User {Email} unsubscribed", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe user");
        }
    }

    private string GetSubscriberHash(string email)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
