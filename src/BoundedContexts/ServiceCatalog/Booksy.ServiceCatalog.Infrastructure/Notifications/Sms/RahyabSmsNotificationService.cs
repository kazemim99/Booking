// ========================================
// Booksy.ServiceCatalog.Infrastructure/Notifications/Sms/RahyabSmsNotificationService.cs
// ========================================
using Booksy.Infrastructure.External.sms.Rahyab;
using Booksy.ServiceCatalog.Application.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.Notifications.Sms
{
    /// <summary>
    /// Rahyab SMS service implementation
    /// </summary>
    public sealed class RahyabSmsNotificationService : ISmsNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RahyabSmsNotificationService> _logger;
        private readonly string _apiUrl;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _number;
        private readonly string _company;

        public RahyabSmsNotificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<RahyabSmsNotificationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiUrl = configuration["Rahyab:ApiUrl"] ?? "https://api.rahyab.ir/sms/send";
            _userName = configuration["Rahyab:UserName"] ?? "web_negahno";
            _password = configuration["Rahyab:Password"] ?? "B3q71jaY96";
            _number = configuration["Rahyab:Number"] ?? "1000110110001";
            _company = configuration["Rahyab:Company"] ?? "NEGAHNO";
        }

        public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendSmsAsync(
            string phoneNumber,
            string message,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Clean phone number (remove spaces, dashes, etc.)
                phoneNumber = CleanPhoneNumber(phoneNumber);

                // Validate phone number
                if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 10)
                {
                    return (false, null, "Invalid phone number");
                }

                // Limit message length for SMS (1600 chars with concatenation)
                if (message.Length > 1600)
                {
                    message = message.Substring(0, 1597) + "...";
                }

                var request = new RahyabSendSmsReques
                {
                    message = message,
                    destinationAddress = phoneNumber,
                    number = _number,
                    userName = _userName,
                    password = _password,
                    company = _company,
                    messageId = Guid.NewGuid().ToString()
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}. MessageId: {MessageId}",
                        phoneNumber, request.messageId);
                    return (true, request.messageId, null);
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {Status}, Error: {Error}",
                        phoneNumber, response.StatusCode, errorBody);
                    return (false, null, $"Rahyab error: {response.StatusCode} - {errorBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending SMS to {PhoneNumber}", phoneNumber);
                return (false, null, ex.Message);
            }
        }

        public async Task<List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>> SendBulkSmsAsync(
            List<string> phoneNumbers,
            string message,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<(string PhoneNumber, bool Success, string? MessageId, string? ErrorMessage)>();

            // Send SMS to each number individually
            // For production, check if Rahyab supports bulk API
            foreach (var phoneNumber in phoneNumbers)
            {
                var result = await SendSmsAsync(phoneNumber, message, metadata, cancellationToken);
                results.Add((phoneNumber, result.Success, result.MessageId, result.ErrorMessage));

                // Add small delay to avoid rate limiting
                await Task.Delay(100, cancellationToken);
            }

            return results;
        }

        private static string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            // Remove all non-digit characters
            return new string(phoneNumber.Where(char.IsDigit).ToArray());
        }
    }
}
