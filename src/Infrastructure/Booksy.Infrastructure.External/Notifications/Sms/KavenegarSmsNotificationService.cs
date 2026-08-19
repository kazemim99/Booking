// ========================================
// Booksy.Infrastructure.External/Notifications/Sms/KavenegarSmsNotificationService.cs
// ========================================
using Booksy.Core.Application.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Booksy.Infrastructure.External.Notifications.Sms
{
    /// <summary>
    /// Kavenegar SMS gateway (Iranian provider). Selected with <c>Notifications:SMS:Provider = "Kavenegar"</c>.
    /// </summary>
    /// <remarks>
    /// This replaces <c>ServiceCatalog.Infrastructure.ExternalServices.Sms.KavenegarSmsService</c>, which
    /// implemented the now-deleted booking-template SMS interface. Those per-event template methods
    /// (<c>SendBookingConfirmedSmsAsync</c> and friends) had no callers anywhere in the solution — lifecycle
    /// notifications go through the notification dispatcher, which renders the body itself — so only the
    /// generic send survives here.
    /// </remarks>
    public sealed class KavenegarSmsNotificationService : ISmsNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<KavenegarSmsNotificationService> _logger;
        private readonly string _apiKey;
        private readonly string _sender;
        private readonly bool _isEnabled;

        public KavenegarSmsNotificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<KavenegarSmsNotificationService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Single global sandbox switch (Sms:SandboxMode) overrides every provider: when on,
            // no real SMS is sent regardless of the per-provider flag. See also RahyabSmsNotificationService.
            var globalSandbox = configuration.GetValue<bool>("Sms:SandboxMode");
            _isEnabled = configuration.GetValue<bool>("Kavenegar:Enabled", true) && !globalSandbox;
            _sender = configuration["Kavenegar:Sender"] ?? "10004346";

            // Only require an API key when the gateway is actually enabled. A missing key while
            // disabled must NOT throw — otherwise resolving this service (e.g. during a booking
            // notification) would fail DI construction and could break the booking flow.
            var apiKey = configuration["Kavenegar:ApiKey"];
            if (_isEnabled && string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Kavenegar API key not configured");
            _apiKey = apiKey ?? string.Empty;

            _httpClient.BaseAddress = new Uri("https://api.kavenegar.com/v1/");
        }

        public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
            string phoneNumber,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS notifications disabled. Skipping SMS to {PhoneNumber}", phoneNumber);
                return (true, $"disabled-{Guid.NewGuid()}", null);
            }

            try
            {
                var url = $"{_apiKey}/sms/send.json?sender={_sender}&receptor={phoneNumber}&message={Uri.EscapeDataString(message)}";

                var response = await _httpClient.PostAsJsonAsync(url, new { }, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
                    return (true, response.Headers.ETag?.Tag ?? Guid.NewGuid().ToString(), null);
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {StatusCode}, Response: {Response}",
                    phoneNumber, response.StatusCode, errorBody);
                return (false, null, $"Kavenegar error: {response.StatusCode} - {errorBody}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
                return (false, null, ex.Message);
            }
        }

        public async Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
            List<string> phoneNumbers,
            string message,
            Dictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>();

            foreach (var phoneNumber in phoneNumbers)
            {
                var result = await SendSmsAsync(phoneNumber, message, metadata, cancellationToken);
                results.Add((phoneNumber, result.Success, result.MessageId, result.ErrorMessage));
            }

            return results;
        }
    }
}
