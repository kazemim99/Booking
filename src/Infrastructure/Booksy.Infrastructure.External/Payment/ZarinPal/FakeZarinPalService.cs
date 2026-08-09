using System.Security.Cryptography;
using System.Text;
using Booksy.Infrastructure.External.Payment.ZarinPal.Models;
using Microsoft.Extensions.Logging;

namespace Booksy.Infrastructure.External.Payment.ZarinPal
{
    /// <summary>
    /// Deterministic stand-in for <see cref="ZarinPalService"/> used **only** for automated end-to-end verification
    /// (tier T2). It speaks the exact same contract as the real gateway — create returns an authority + a StartPay-shaped
    /// URL, verify returns a RefId/CardPan/fee or a gateway error code — so the application, callback and verification
    /// paths under test are byte-for-byte the production ones. Nothing about payment or booking business rules changes:
    /// the deposit gate, the ledger, idempotency and reconciliation all run exactly as in production.
    ///
    /// <para><b>Safety.</b> This type is only reachable when <c>Payments:UseFakeZarinPal=true</c> AND the host is not
    /// running in Production — <see cref="FakeZarinPalGuard"/> throws at startup if that combination is ever attempted,
    /// so a production configuration cannot silently activate it. Enabling it also emits a loud startup banner.</para>
    ///
    /// <para><b>Determinism.</b> Authority and RefId are derived from a SHA-256 of the amount + description, so the same
    /// logical payment always yields the same values and a test can assert them. Authorities are 36 characters with the
    /// <c>A</c>-prefixed shape ZarinPal uses.</para>
    ///
    /// <para><b>Scenario control.</b> The outcome is driven by the payment <i>description</i> (the only field a caller
    /// can influence end-to-end without touching production code paths):
    /// <list type="bullet">
    ///   <item><description><c>FAKE_REFUSE</c> — create fails with a gateway error (nothing is charged).</description></item>
    ///   <item><description><c>FAKE_UNVERIFIED</c> — create succeeds, verify reports "not paid" (models a customer
    ///   abandoning the bank page, and the NOK/cancel path).</description></item>
    ///   <item><description>anything else — create succeeds and verify succeeds.</description></item>
    /// </list>
    /// The scenario is encoded into the authority at create time so verify stays stateless and consistent.</para>
    /// </summary>
    public sealed class FakeZarinPalService : IZarinPalService
    {
        /// <summary>Marker embedded in a fake authority so verify can honour the requested scenario statelessly.</summary>
        private const string UnverifiedMarker = "U";
        private const string PaidMarker = "P";

        public const string RefuseTrigger = "FAKE_REFUSE";
        public const string UnverifiedTrigger = "FAKE_UNVERIFIED";

        private readonly ILogger<FakeZarinPalService> _logger;

        public FakeZarinPalService(ILogger<FakeZarinPalService> logger) => _logger = logger;

        public Task<ZarinPalPaymentResult> CreatePaymentRequestAsync(
            decimal amount,
            string description,
            string? mobile = null,
            string? email = null,
            CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                // Mirror the real gateway's rejection of a non-positive amount.
                return Task.FromResult(new ZarinPalPaymentResult
                {
                    IsSuccessful = false,
                    ErrorCode = -11,
                    ErrorMessage = "FAKE: amount must be positive",
                });
            }

            if (description.Contains(RefuseTrigger, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("FAKE ZarinPal: refusing create (scenario {Scenario})", RefuseTrigger);
                return Task.FromResult(new ZarinPalPaymentResult
                {
                    IsSuccessful = false,
                    // -51 is ZarinPal's "operation failed" family; any non-zero code exercises the refusal path.
                    ErrorCode = -51,
                    ErrorMessage = "FAKE: gateway refused the payment request",
                });
            }

            var unverified = description.Contains(UnverifiedTrigger, StringComparison.OrdinalIgnoreCase);
            var authority = BuildAuthority(amount, description, unverified ? UnverifiedMarker : PaidMarker);

            _logger.LogWarning(
                "FAKE ZarinPal: created authority {Authority} for amount {Amount} (verify will {Outcome})",
                authority, amount, unverified ? "FAIL" : "SUCCEED");

            return Task.FromResult(new ZarinPalPaymentResult
            {
                IsSuccessful = true,
                Authority = authority,
                // Same shape as the real sandbox StartPay URL so the client's redirect handling is unchanged.
                PaymentUrl = $"https://sandbox.zarinpal.com/pg/StartPay/{authority}",
                Fee = 0,
            });
        }

        public Task<ZarinPalVerifyResult> VerifyPaymentAsync(
            string authority,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                return Task.FromResult(new ZarinPalVerifyResult
                {
                    IsSuccessful = false,
                    ErrorCode = -53,
                    ErrorMessage = "FAKE: authority is required",
                });
            }

            // Scenario marker is the character right after the 'A' prefix (see BuildAuthority).
            var unverified = authority.Length > 1 &&
                             string.Equals(authority[1].ToString(), UnverifiedMarker, StringComparison.Ordinal);

            if (unverified)
            {
                _logger.LogWarning("FAKE ZarinPal: reporting NOT VERIFIED for authority {Authority}", authority);
                return Task.FromResult(new ZarinPalVerifyResult
                {
                    IsSuccessful = false,
                    // -51: the payment was not completed. This is what the real gateway reports for an abandoned
                    // or cancelled payment, which is exactly the NOK path the client must handle.
                    ErrorCode = -51,
                    ErrorMessage = "FAKE: payment was not completed",
                });
            }

            var refId = DeterministicRefId(authority);
            _logger.LogWarning(
                "FAKE ZarinPal: verified authority {Authority} -> RefId {RefId}", authority, refId);

            return Task.FromResult(new ZarinPalVerifyResult
            {
                IsSuccessful = true,
                RefId = refId,
                CardPan = "6274********1234",
                CardHash = "FAKECARDHASH",
                Fee = 0,
            });
        }

        public Task<ZarinPalRefundResult> RefundPaymentAsync(
            string authority,
            decimal amount,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("FAKE ZarinPal: refunding {Amount} for authority {Authority}", amount, authority);
            return Task.FromResult(new ZarinPalRefundResult { IsSuccessful = true });
        }

        /// <summary>
        /// Builds a stable, ZarinPal-shaped authority: 'A' + scenario marker + 34 hex chars derived from the payment,
        /// giving the 36-character length the real gateway uses.
        /// </summary>
        private static string BuildAuthority(decimal amount, string description, string marker)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{amount}|{description}")));
            return $"A{marker}{hash[..34]}";
        }

        private static long DeterministicRefId(string authority)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(authority));
            // Positive 9-digit reference, like a real RefId.
            var value = BitConverter.ToUInt32(hash, 0) % 900_000_000L;
            return 100_000_000L + value;
        }
    }

}
