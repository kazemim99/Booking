// ========================================
// Marketing/MailchimpService.cs
// ========================================
using System.Net.Http;

namespace Booksy.Infrastructure.External.Marketing;

public class MailchimpSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://us1.api.mailchimp.com/3.0";
    public string ListId { get; set; } = string.Empty;
    public string MandrillApiKey { get; set; } = string.Empty;
}
