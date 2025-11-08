// ========================================
// BehpardakhtService.cs
// ========================================
using Booksy.Infrastructure.External.Payment.Behpardakht.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using System.Text;

namespace Booksy.Infrastructure.External.Payment.Behpardakht
{
    /// <summary>
    /// Behpardakht (Bank Mellat) payment gateway service implementation
    /// </summary>
    public class BehpardakhtService : IBehpardakhtService
    {
        private readonly BehpardakhtSettings _settings;
        private readonly ILogger<BehpardakhtService> _logger;
        private readonly Random _random = new();

        public BehpardakhtService(
            IOptions<BehpardakhtSettings> settings,
            ILogger<BehpardakhtService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<BehpardakhtPaymentResult> CreatePaymentRequestAsync(
            decimal amount,
            string description,
            long payerId = 0,
            string? mobile = null,
            string? email = null,
            string? additionalData = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Convert amount to Rials (smallest unit) - Behpardakht expects amount in Rials
                var amountInRials = (long)amount;

                // Generate unique order ID
                var orderId = GenerateOrderId();

                // Get current date and time in required format
                var localDate = DateTime.Now.ToString("yyyyMMdd");
                var localTime = DateTime.Now.ToString("HHmmss");

                _logger.LogInformation(
                    "Behpardakht payment request: Amount={Amount}, OrderId={OrderId}, Description={Description}",
                    amountInRials, orderId, description);

                // In real implementation, this would make a SOAP call to Behpardakht
                // For now, return a mock successful response
                // TODO: Implement actual SOAP client for bpPayRequest

                var response = await CallBpPayRequestAsync(
                    orderId,
                    amountInRials,
                    localDate,
                    localTime,
                    additionalData ?? "",
                    payerId,
                    cancellationToken);

                if (string.IsNullOrEmpty(response))
                {
                    return new BehpardakhtPaymentResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Empty response from gateway",
                        ErrorCode = -1
                    };
                }

                // Parse response: "ResCode,RefId"
                var parts = response.Split(',');
                var resCode = int.Parse(parts[0]);

                if (resCode == 0 && parts.Length > 1)
                {
                    var refId = parts[1];
                    var paymentUrl = $"{_settings.PaymentPageUrl}";

                    return new BehpardakhtPaymentResult
                    {
                        IsSuccessful = true,
                        RefId = refId,
                        PaymentUrl = paymentUrl
                    };
                }
                else
                {
                    return new BehpardakhtPaymentResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(resCode),
                        ErrorCode = resCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Behpardakht payment request");
                return new BehpardakhtPaymentResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Payment request failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<BehpardakhtVerifyResult> VerifyPaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Behpardakht verify payment: OrderId={OrderId}, SaleOrderId={SaleOrderId}, SaleReferenceId={SaleReferenceId}",
                    orderId, saleOrderId, saleReferenceId);

                // In real implementation, this would make a SOAP call to Behpardakht
                // TODO: Implement actual SOAP client for bpVerifyRequest

                var resCode = await CallBpVerifyRequestAsync(
                    orderId,
                    saleOrderId,
                    saleReferenceId,
                    cancellationToken);

                if (resCode == 0 || resCode == 43) // 0 = Success, 43 = Already verified
                {
                    return new BehpardakhtVerifyResult
                    {
                        IsSuccessful = true,
                        SaleReferenceId = saleReferenceId,
                        CardHolderPan = "6104****1234" // This would come from actual response
                    };
                }
                else
                {
                    return new BehpardakhtVerifyResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(resCode),
                        ErrorCode = resCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Behpardakht verification");
                return new BehpardakhtVerifyResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Verification failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<BehpardakhtSettleResult> SettlePaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Behpardakht settle payment: OrderId={OrderId}, SaleOrderId={SaleOrderId}",
                    orderId, saleOrderId);

                // TODO: Implement actual SOAP client for bpSettleRequest
                var resCode = await CallBpSettleRequestAsync(
                    orderId,
                    saleOrderId,
                    saleReferenceId,
                    cancellationToken);

                if (resCode == 0 || resCode == 45) // 0 = Success, 45 = Already settled
                {
                    return new BehpardakhtSettleResult
                    {
                        IsSuccessful = true
                    };
                }
                else
                {
                    return new BehpardakhtSettleResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(resCode),
                        ErrorCode = resCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Behpardakht settlement");
                return new BehpardakhtSettleResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Settlement failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<BehpardakhtInquiryResult> InquiryPaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Behpardakht inquiry payment: OrderId={OrderId}, SaleOrderId={SaleOrderId}",
                    orderId, saleOrderId);

                // TODO: Implement actual SOAP client for bpInquiryRequest
                var resCode = await CallBpInquiryRequestAsync(
                    orderId,
                    saleOrderId,
                    saleReferenceId,
                    cancellationToken);

                if (resCode == 0)
                {
                    return new BehpardakhtInquiryResult
                    {
                        IsSuccessful = true,
                        Status = "Successful"
                    };
                }
                else
                {
                    return new BehpardakhtInquiryResult
                    {
                        IsSuccessful = false,
                        Status = "Failed",
                        ErrorMessage = GetErrorMessage(resCode),
                        ErrorCode = resCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Behpardakht inquiry");
                return new BehpardakhtInquiryResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Inquiry failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<BehpardakhtReversalResult> ReversePaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Behpardakht reverse payment: OrderId={OrderId}, SaleOrderId={SaleOrderId}",
                    orderId, saleOrderId);

                // TODO: Implement actual SOAP client for bpReversalRequest
                var resCode = await CallBpReversalRequestAsync(
                    orderId,
                    saleOrderId,
                    saleReferenceId,
                    cancellationToken);

                if (resCode == 0 || resCode == 48) // 0 = Success, 48 = Already reversed
                {
                    return new BehpardakhtReversalResult
                    {
                        IsSuccessful = true
                    };
                }
                else
                {
                    return new BehpardakhtReversalResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(resCode),
                        ErrorCode = resCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Behpardakht reversal");
                return new BehpardakhtReversalResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Reversal failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        public async Task<BehpardakhtRefundResult> RefundPaymentAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var amountInRials = (long)amount;

                _logger.LogInformation(
                    "Behpardakht refund payment: OrderId={OrderId}, SaleOrderId={SaleOrderId}, Amount={Amount}",
                    orderId, saleOrderId, amountInRials);

                // TODO: Implement actual SOAP client for bpRefundRequest
                var resCode = await CallBpRefundRequestAsync(
                    orderId,
                    saleOrderId,
                    saleReferenceId,
                    amountInRials,
                    cancellationToken);

                if (resCode == 0)
                {
                    return new BehpardakhtRefundResult
                    {
                        IsSuccessful = true
                    };
                }
                else
                {
                    return new BehpardakhtRefundResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = GetErrorMessage(resCode),
                        ErrorCode = resCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Behpardakht refund");
                return new BehpardakhtRefundResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Refund failed: {ex.Message}",
                    ErrorCode = -1
                };
            }
        }

        #region Private Helper Methods

        private long GenerateOrderId()
        {
            // Generate a unique order ID (timestamp-based)
            return DateTime.UtcNow.Ticks;
        }

        // These methods would contain actual SOAP client implementations
        // For now, they return mock responses for testing

        private async Task<string> CallBpPayRequestAsync(
            long orderId,
            long amount,
            string localDate,
            string localTime,
            string additionalData,
            long payerId,
            CancellationToken cancellationToken)
        {
            // TODO: Implement actual SOAP call to bpPayRequest
            // This is a placeholder that returns a mock success response
            await Task.Delay(100, cancellationToken); // Simulate network delay

            var refId = $"REF{Guid.NewGuid():N}";
            return $"0,{refId}";
        }

        private async Task<int> CallBpVerifyRequestAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken)
        {
            // TODO: Implement actual SOAP call to bpVerifyRequest
            await Task.Delay(100, cancellationToken);
            return 0; // Success
        }

        private async Task<int> CallBpSettleRequestAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken)
        {
            // TODO: Implement actual SOAP call to bpSettleRequest
            await Task.Delay(100, cancellationToken);
            return 0; // Success
        }

        private async Task<int> CallBpInquiryRequestAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken)
        {
            // TODO: Implement actual SOAP call to bpInquiryRequest
            await Task.Delay(100, cancellationToken);
            return 0; // Success
        }

        private async Task<int> CallBpReversalRequestAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            CancellationToken cancellationToken)
        {
            // TODO: Implement actual SOAP call to bpReversalRequest
            await Task.Delay(100, cancellationToken);
            return 0; // Success
        }

        private async Task<int> CallBpRefundRequestAsync(
            long orderId,
            long saleOrderId,
            long saleReferenceId,
            long amount,
            CancellationToken cancellationToken)
        {
            // TODO: Implement actual SOAP call to bpRefundRequest
            await Task.Delay(100, cancellationToken);
            return 0; // Success
        }

        /// <summary>
        /// Maps Behpardakht error codes to user-friendly messages (from PDF documentation)
        /// </summary>
        private static string GetErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                0 => "Transaction successful",
                11 => "Card number is invalid",
                12 => "Insufficient balance",
                13 => "Incorrect PIN",
                14 => "Exceeded PIN entry attempts",
                15 => "Card is invalid",
                16 => "Exceeded withdrawal limit",
                17 => "Customer cancelled the transaction",
                18 => "Card expiration date has passed",
                19 => "Exceeded withdrawal amount",
                21 => "Merchant is invalid",
                23 => "Security error",
                24 => "Merchant credentials are invalid",
                25 => "Amount is invalid",
                31 => "Response is invalid",
                32 => "Data format is incorrect",
                33 => "Account is invalid",
                34 => "System error",
                35 => "Date is invalid",
                41 => "Duplicate order ID",
                42 => "Sale transaction not found",
                43 => "Verification already done",
                44 => "Verification request not found",
                45 => "Transaction already settled",
                46 => "Transaction not settled",
                47 => "Settle transaction not found",
                48 => "Transaction already reversed",
                51 => "Duplicate transaction",
                54 => "Original transaction not found",
                55 => "Invalid transaction",
                61 => "Settlement error",
                62 => "Callback URL domain mismatch",
                98 => "Static password usage limit reached",
                111 => "Card issuer is invalid",
                112 => "Card issuer switch error",
                113 => "No response from card issuer",
                114 => "Card holder not authorized for this transaction",
                412 => "Bill ID is incorrect",
                413 => "Payment ID is incorrect",
                414 => "Bill issuing organization is invalid",
                415 => "Session timeout",
                416 => "Error in data submission",
                417 => "Payer ID is invalid",
                418 => "Error in customer information definition",
                419 => "Exceeded allowed data entry attempts",
                421 => "IP is invalid",
                995 => "Card ownership could not be verified",
                _ => $"Unknown error (code: {errorCode})"
            };
        }

        #endregion
    }
}
