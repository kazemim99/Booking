// ========================================
// Booksy.ServiceCatalog.Infrastructure/ExternalServices/Sms/KavenegarSmsService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace Booksy.ServiceCatalog.Infrastructure.ExternalServices.Sms
{
    /// <summary>
    /// Kavenegar SMS service implementation for Iranian SMS gateway
    /// </summary>
    public sealed class KavenegarSmsService : ISmsNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<KavenegarSmsService> _logger;
        private readonly string _apiKey;
        private readonly string _sender;
        private readonly bool _isEnabled;

        public KavenegarSmsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<KavenegarSmsService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _apiKey = configuration["Kavenegar:ApiKey"] ?? throw new InvalidOperationException("Kavenegar API key not configured");
            _sender = configuration["Kavenegar:Sender"] ?? "10004346";
            _isEnabled = configuration.GetValue<bool>("Kavenegar:Enabled", true);

            _httpClient.BaseAddress = new Uri("https://api.kavenegar.com/v1/");
        }

        public async Task SendBookingConfirmedSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string providerName,
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS notifications disabled. Skipping booking confirmed SMS to {PhoneNumber}", phoneNumber);
                return;
            }

            var message = $"سلام {customerName} عزیز،\n" +
                         $"رزرو شما در {providerName} برای خدمت {serviceName} در تاریخ {bookingTime:yyyy/MM/dd} ساعت {bookingTime:HH:mm} تایید شد.\n" +
                         $"منتظر دیدار شما هستیم.";

            await SendSmsAsync(phoneNumber, message, cancellationToken);
        }

        public async Task SendBookingCreatedSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string providerName,
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS notifications disabled. Skipping booking created SMS to {PhoneNumber}", phoneNumber);
                return;
            }

            var message = $"سلام {customerName} عزیز،\n" +
                         $"درخواست رزرو شما در {providerName} برای خدمت {serviceName} در تاریخ {bookingTime:yyyy/MM/dd} ساعت {bookingTime:HH:mm} ثبت شد.\n" +
                         $"پس از تایید، پیامک تایید برای شما ارسال خواهد شد.";

            await SendSmsAsync(phoneNumber, message, cancellationToken);
        }

        public async Task SendBookingCancelledSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS notifications disabled. Skipping booking cancelled SMS to {PhoneNumber}", phoneNumber);
                return;
            }

            var message = $"سلام {customerName} عزیز،\n" +
                         $"رزرو شما برای تاریخ {bookingTime:yyyy/MM/dd} ساعت {bookingTime:HH:mm} لغو شد.\n" +
                         $"دلیل: {reason}\n" +
                         $"امیدواریم در فرصت دیگری شما را ببینیم.";

            await SendSmsAsync(phoneNumber, message, cancellationToken);
        }

        public async Task SendBookingRescheduledSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime oldTime,
            DateTime newTime,
            string providerName,
            CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS notifications disabled. Skipping booking rescheduled SMS to {PhoneNumber}", phoneNumber);
                return;
            }

            var message = $"سلام {customerName} عزیز،\n" +
                         $"رزرو شما در {providerName} از تاریخ {oldTime:yyyy/MM/dd HH:mm} به تاریخ {newTime:yyyy/MM/dd HH:mm} تغییر یافت.\n" +
                         $"منتظر دیدار شما هستیم.";

            await SendSmsAsync(phoneNumber, message, cancellationToken);
        }

        public async Task SendBookingCompletedSmsAsync(
            string phoneNumber,
            string customerName,
            string providerName,
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS notifications disabled. Skipping booking completed SMS to {PhoneNumber}", phoneNumber);
                return;
            }

            var message = $"سلام {customerName} عزیز،\n" +
                         $"از اینکه خدمت {serviceName} را در {providerName} دریافت کردید سپاسگزاریم.\n" +
                         $"لطفاً نظر خود را با ما به اشتراک بگذارید.";

            await SendSmsAsync(phoneNumber, message, cancellationToken);
        }

        public async Task SendBookingReminderSmsAsync(
            string phoneNumber,
            string customerName,
            DateTime bookingTime,
            string providerName,
            string serviceAddress,
            CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("SMS notifications disabled. Skipping booking reminder SMS to {PhoneNumber}", phoneNumber);
                return;
            }

            var message = $"سلام {customerName} عزیز،\n" +
                         $"یادآوری: شما رزرو در {providerName} دارید.\n" +
                         $"تاریخ: {bookingTime:yyyy/MM/dd} ساعت {bookingTime:HH:mm}\n" +
                         $"آدرس: {serviceAddress}";

            await SendSmsAsync(phoneNumber, message, cancellationToken);
        }

        private async Task SendSmsAsync(string receptor, string message, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"{_apiKey}/sms/send.json?sender={_sender}&receptor={receptor}&message={Uri.EscapeDataString(message)}";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                var content = await response.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}", receptor);
                }
                else
                {
                    _logger.LogError("Failed to send SMS to {PhoneNumber}. Status: {StatusCode}, Response: {Response}",
                        receptor, response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", receptor);
            }
        }
    }
}
