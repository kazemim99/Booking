// ========================================
// ZarinPalService.cs
// ========================================
using Booksy.Infrastructure.External.Payment.ZarinPal.DTOs;
using Booksy.Infrastructure.External.Payment.ZarinPal.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Booksy.Infrastructure.External.Payment.ZarinPal
{
    /// <summary>
    /// ZarinPal payment gateway service implementation
    /// </summary>
    public class ZarinPalService : IZarinPalService
    {
        private readonly ZarinPalSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ZarinPalService> _logger;
        private const string PAYMENT_REQUEST_ENDPOINT = "/pg/v4/payment/request.json";
        private const string PAYMENT_VERIFY_ENDPOINT = "/pg/v4/payment/verify.json";
        private const string PAYMENT_REFUND_ENDPOINT = "/pg/v4/payment/refund.json";

        public ZarinPalService(
            IOptions<ZarinPalSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<ZarinPalService> logger)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ZarinPalPaymentResult> CreatePaymentRequestAsync(
            decimal amount,
            string description,
            string? mobile = null,
            string? email = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Convert amount to Rials (smallest unit)
                var amountInRials = (long)amount;

                var request = new ZarinPalPaymentRequestDto
                {
                    MerchantId = _settings.MerchantId,
                    Amount = amountInRials,
                    Description = description,
                    CallbackUrl = _settings.CallbackUrl,
                    Metadata = new ZarinPalMetadata
                    {
                        Mobile = mobile,
                        Email = email
                    }
                };

                var httpClient = _httpClientFactory.CreateClient("ZarinPal");
                httpClient.BaseAddress = new Uri(_settings.BaseUrl);

                var response = await httpClient.PostAsJsonAsync(
                    PAYMENT_REQUEST_ENDPOINT,
                    request,
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("ZarinPal payment request response: {Response}", responseContent);

                var result = JsonSerializer.Deserialize<ZarinPalPaymentResponseDto>(responseContent);

                if (result?.Data != null && result.Data.Code == 100)
                {
                    var paymentUrl = $"{_settings.StartPayUrl}{result.Data.Authority}";

                    return new ZarinPalPaymentResult
                    {
                        IsSuccessful = true,
                        Authority = result.Data.Authority,
                        PaymentUrl = paymentUrl,
                        Fee = result.Data.Fee.HasValue ? result.Data.Fee.Value / 10m : null
                    };
                }
                else
                {
                    var error = result?.Errors?.FirstOrDefault();
                    var errorMessage = error?.Message ?? result?.Data?.Message ?? "Unknown error";
                    var errorCode = error?.Code ?? result?.Data?.Code ?? -1;

                    _logger.LogError("ZarinPal payment request failed: {Code} - {Message}", errorCode, errorMessage);

                    return new ZarinPalPaymentResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(errorCode),
                        ErrorCode = errorCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during ZarinPal payment request");
                return new ZarinPalPaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Payment request failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<ZarinPalVerifyResult> VerifyPaymentAsync(
            string authority,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Convert amount to Rials
                var amountInRials = (long)amount;

                var request = new ZarinPalVerifyRequestDto
                {
                    MerchantId = _settings.MerchantId,
                    Amount = amountInRials,
                    Authority = authority
                };

                var httpClient = _httpClientFactory.CreateClient("ZarinPal");
                httpClient.BaseAddress = new Uri(_settings.BaseUrl);

                var response = await httpClient.PostAsJsonAsync(
                    PAYMENT_VERIFY_ENDPOINT,
                    request,
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("ZarinPal verification response: {Response}", responseContent);

                var result = JsonSerializer.Deserialize<ZarinPalVerifyResponseDto>(responseContent);

                if (result?.Data != null && (result.Data.Code == 100 || result.Data.Code == 101))
                {
                    return new ZarinPalVerifyResult
                    {
                        IsSuccessful = true,
                        RefId = result.Data.RefId,
                        CardPan = result.Data.CardPan,
                        CardHash = result.Data.CardHash,
                        Fee = result.Data.Fee.HasValue ? result.Data.Fee.Value / 10m : null
                    };
                }
                else
                {
                    var error = result?.Errors?.FirstOrDefault();
                    var errorMessage = error?.Message ?? result?.Data?.Message ?? "Unknown error";
                    var errorCode = error?.Code ?? result?.Data?.Code ?? -1;

                    _logger.LogError("ZarinPal verification failed: {Code} - {Message}", errorCode, errorMessage);

                    return new ZarinPalVerifyResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(errorCode),
                        ErrorCode = errorCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during ZarinPal verification");
                return new ZarinPalVerifyResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Verification failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<ZarinPalRefundResult> RefundPaymentAsync(
            string authority,
            decimal amount,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Convert amount to Rials
                var amountInRials = (long)amount;

                var request = new ZarinPalRefundRequestDto
                {
                    MerchantId = _settings.MerchantId,
                    Authority = authority,
                    Amount = amountInRials,
                    Description = description
                };

                var httpClient = _httpClientFactory.CreateClient("ZarinPal");
                httpClient.BaseAddress = new Uri(_settings.BaseUrl);

                var response = await httpClient.PostAsJsonAsync(
                    PAYMENT_REFUND_ENDPOINT,
                    request,
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("ZarinPal refund response: {Response}", responseContent);

                var result = JsonSerializer.Deserialize<ZarinPalRefundResponseDto>(responseContent);

                if (result?.Data != null && result.Data.Code == 100)
                {
                    return new ZarinPalRefundResult
                    {
                        IsSuccessful = true
                    };
                }
                else
                {
                    var error = result?.Errors?.FirstOrDefault();
                    var errorMessage = error?.Message ?? result?.Data?.Message ?? "Unknown error";
                    var errorCode = error?.Code ?? result?.Data?.Code ?? -1;

                    _logger.LogError("ZarinPal refund failed: {Code} - {Message}", errorCode, errorMessage);

                    return new ZarinPalRefundResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(errorCode),
                        ErrorCode = errorCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during ZarinPal refund");
                return new ZarinPalRefundResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Refund failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        /// <summary>
        /// Maps ZarinPal error codes to user-friendly messages
        /// </summary>
        private static string GetErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                -9 => "Validation error in submitted data",
                -10 => "Invalid merchant ID or IP/domain not registered",
                -11 => "Invalid merchant credentials",
                -12 => "Not enough attempts remaining",
                -15 => "Payment gateway access suspended",
                -16 => "Invalid credibility level",
                -17 => "Cannot perform operation due to restrictions on merchant",
                -30 => "Invalid merchant terminal code",
                -31 => "Terminal inactive",
                -32 => "Operation not allowed for terminal",
                -33 => "Invalid pay reference code",
                -34 => "Invalid payment amount",
                -35 => "Amount exceeds transaction limit",
                -36 => "Amount below minimum transaction limit",
                -37 => "Amount exceeds daily limit",
                -38 => "Amount exceeds maximum transaction limit",
                -39 => "Amount less than minimum transaction value",
                -40 => "Invalid additional data",
                -41 => "Invalid additional data length",
                -42 => "Invalid pan length",
                -43 => "Invalid currency code",
                -50 => "Amount must be above 1000 Rials",
                -51 => "Transaction already verified",
                -52 => "Transaction not found for verification",
                -53 => "Transaction verification unsuccessful",
                -54 => "Transaction verification time expired",
                100 => "Operation successful",
                101 => "Transaction already verified",
                _ => $"Unknown error (code: {errorCode})"
            };
        }
    }
}
