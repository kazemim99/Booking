// ========================================
// IDPayService.cs
// ========================================
using Booksy.Infrastructure.External.Payment.IDPay.DTOs;
using Booksy.Infrastructure.External.Payment.IDPay.Exceptions;
using Booksy.Infrastructure.External.Payment.IDPay.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Booksy.Infrastructure.External.Payment.IDPay
{
    /// <summary>
    /// Implementation of IDPay payment gateway service
    /// </summary>
    public class IDPayService : IIDPayService
    {
        private readonly HttpClient _httpClient;
        private readonly IDPaySettings _settings;
        private readonly ILogger<IDPayService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public IDPayService(
            HttpClient httpClient,
            IOptions<IDPaySettings> settings,
            ILogger<IDPayService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<IDPayPaymentResult> CreatePaymentRequestAsync(
            decimal amount,
            string orderId,
            string description,
            string? name = null,
            string? phone = null,
            string? email = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating IDPay payment request for amount: {Amount}, orderId: {OrderId}", amount, orderId);

                // Convert amount to Rials (assuming amount is in Tomans, multiply by 10)
                var amountInRials = (long)(amount * 10);

                var requestDto = new IDPayPaymentRequestDto
                {
                    OrderId = orderId,
                    Amount = amountInRials,
                    Description = description,
                    CallbackUrl = _settings.CallbackUrl,
                    Name = name,
                    Phone = phone,
                    Email = email
                };

                var jsonContent = JsonSerializer.Serialize(requestDto, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/payment", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseDto = JsonSerializer.Deserialize<IDPayPaymentResponseDto>(responseContent, _jsonOptions);

                    _logger.LogInformation("IDPay payment request created successfully. PaymentId: {PaymentId}", responseDto?.Id);

                    return new IDPayPaymentResult
                    {
                        IsSuccessful = true,
                        PaymentId = responseDto?.Id,
                        PaymentUrl = responseDto?.Link,
                        ErrorMessage = null,
                        ErrorCode = 0
                    };
                }
                else
                {
                    _logger.LogError("IDPay payment request failed. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);

                    // Parse error response
                    var errorCode = (int)response.StatusCode;
                    var errorMessage = $"IDPay payment request failed with status {response.StatusCode}";

                    return new IDPayPaymentResult
                    {
                        IsSuccessful = false,
                        PaymentId = null,
                        PaymentUrl = null,
                        ErrorMessage = errorMessage,
                        ErrorCode = errorCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating IDPay payment request");
                throw new IDPayException($"Failed to create payment request: {ex.Message}", -1, ex);
            }
        }

        public async Task<IDPayVerifyResult> VerifyPaymentAsync(
            string paymentId,
            string orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Verifying IDPay payment. PaymentId: {PaymentId}, OrderId: {OrderId}", paymentId, orderId);

                var requestDto = new IDPayVerifyRequestDto
                {
                    Id = paymentId,
                    OrderId = orderId
                };

                var jsonContent = JsonSerializer.Serialize(requestDto, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/payment/verify", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseDto = JsonSerializer.Deserialize<IDPayVerifyResponseDto>(responseContent, _jsonOptions);

                    _logger.LogInformation("IDPay payment verified successfully. TrackId: {TrackId}", responseDto?.TrackId);

                    return new IDPayVerifyResult
                    {
                        IsSuccessful = true,
                        TrackId = responseDto?.TrackId ?? 0,
                        PaymentId = responseDto?.Id,
                        Amount = responseDto?.Amount ?? 0,
                        CardNumber = responseDto?.CardNo,
                        HashedCardNumber = responseDto?.HashedCardNo,
                        StatusCode = responseDto?.Status ?? 0,
                        ErrorMessage = null,
                        ErrorCode = 0
                    };
                }
                else
                {
                    _logger.LogError("IDPay payment verification failed. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);

                    var errorCode = (int)response.StatusCode;
                    var errorMessage = $"IDPay payment verification failed with status {response.StatusCode}";

                    throw new IDPayVerificationException(errorMessage, errorCode);
                }
            }
            catch (IDPayVerificationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while verifying IDPay payment");
                throw new IDPayVerificationException($"Failed to verify payment: {ex.Message}", -1, ex);
            }
        }
    }
}
