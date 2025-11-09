using Booksy.Core.Domain.Enums;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.PaymentAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds payment records for bookings with Iranian payment providers (ZarinPal, etc.)
    /// </summary>
    public sealed class PaymentSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<PaymentSeeder> _logger;
        private readonly Random _random = new Random(11223);

        public PaymentSeeder(
            ServiceCatalogDbContext context,
            ILogger<PaymentSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.Payments.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Payments already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian payment data seeding...");

                // Get completed and confirmed bookings that need payments
                var bookings = await _context.Bookings
                    .Include(b => b.PaymentInfo)
                    .Where(b => b.Status == Domain.Enums.BookingStatus.Completed ||
                               b.Status == Domain.Enums.BookingStatus.Confirmed)
                    .ToListAsync(cancellationToken);

                if (!bookings.Any())
                {
                    _logger.LogWarning("No bookings found for payment seeding.");
                    return;
                }

                var payments = new List<Payment>();

                foreach (var booking in bookings)
                {
                    // Create payment for this booking
                    var payment = CreatePaymentForBooking(booking);
                    if (payment != null)
                    {
                        payments.Add(payment);
                    }
                }

                await _context.Payments.AddRangeAsync(payments, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} Iranian payments", payments.Count);
                LogPaymentStatistics(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian payment data");
                throw;
            }
        }

        private Payment? CreatePaymentForBooking(Domain.Aggregates.BookingAggregate.Booking booking)
        {
            try
            {
                var amount = Money.Create(booking.TotalPrice.Amount, "IRR");

                // Determine payment method (mostly Iranian methods)
                var paymentMethod = GetRandomIranianPaymentMethod();

                // Mostly ZarinPal for Iranian payments
                var paymentProvider = _random.Next(100) < 80
                    ? PaymentProvider.ZarinPal
                    : GetRandomIranianPaymentProvider();

                var description = $"پرداخت برای رزرو شماره {booking.Id}";

                var payment = Payment.Create(
                    booking.Id,
                    booking.CustomerId,
                    booking.ProviderId,
                    amount,
                    paymentMethod,
                    paymentProvider,
                    description,
                    null);

                // Simulate payment flow based on booking status
                if (booking.Status == Domain.Enums.BookingStatus.Completed)
                {
                    // For completed bookings, mark payment as paid
                    if (paymentProvider == PaymentProvider.ZarinPal)
                    {
                        var authority = GenerateZarinPalAuthority();
                        var refNumber = GenerateZarinPalRefNumber();
                        var cardPan = GenerateIranianCardPan();

                        payment.RecordPaymentRequest(authority, GetPaymentUrl(authority));
                        payment.VerifyPayment(refNumber, cardPan, null);
                    }
                    else
                    {
                        var paymentIntentId = $"pi_iranian_{Guid.NewGuid():N}";
                        payment.Authorize(paymentIntentId, null);
                        payment.Capture();
                    }
                }
                else if (booking.Status == Domain.Enums.BookingStatus.Confirmed)
                {
                    // For confirmed bookings, might be paid or just pending
                    if (_random.Next(100) < 70) // 70% paid
                    {
                        if (paymentProvider == PaymentProvider.ZarinPal)
                        {
                            var authority = GenerateZarinPalAuthority();
                            var refNumber = GenerateZarinPalRefNumber();
                            var cardPan = GenerateIranianCardPan();

                            payment.RecordPaymentRequest(authority, GetPaymentUrl(authority));
                            payment.VerifyPayment(refNumber, cardPan, null);
                        }
                        else
                        {
                            var paymentIntentId = $"pi_iranian_{Guid.NewGuid():N}";
                            payment.Authorize(paymentIntentId, null);
                            payment.Capture();
                        }
                    }
                    else // 30% just recorded payment request (pending)
                    {
                        if (paymentProvider == PaymentProvider.ZarinPal)
                        {
                            var authority = GenerateZarinPalAuthority();
                            payment.RecordPaymentRequest(authority, GetPaymentUrl(authority));
                        }
                        else
                        {
                            var paymentIntentId = $"pi_iranian_{Guid.NewGuid():N}";
                            payment.Authorize(paymentIntentId, null);
                        }
                    }
                }

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create payment for booking {BookingId}", booking.Id);
                return null;
            }
        }

        private PaymentMethod GetRandomIranianPaymentMethod()
        {
            // Iranian payment methods distribution
            var methods = new[]
            {
                PaymentMethod.ZarinPal,
                PaymentMethod.ZarinPal,
                PaymentMethod.ZarinPal,
                PaymentMethod.ZarinPal, // 67% online via ZarinPal
                PaymentMethod.CreditCard,
                PaymentMethod.Cash
            };

            return methods[_random.Next(methods.Length)];
        }

        private Core.Domain.Enums.PaymentProvider GetRandomIranianPaymentProvider()
        {
            // Other Iranian payment providers (used when not ZarinPal)
            var providers = new[]
            {
                Core.Domain.Enums.PaymentProvider.IDPay,
                Core.Domain.Enums.PaymentProvider.Parsian,
                Core.Domain.Enums.PaymentProvider.Saman,
                Core.Domain.Enums.PaymentProvider.Behpardakht
            };

            return providers[_random.Next(providers.Length)];
        }

        private string GenerateZarinPalAuthority()
        {
            // ZarinPal authority format: A followed by 35 alphanumeric characters
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var authority = "A" + new string(Enumerable.Repeat(chars, 35)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
            return authority;
        }

        private string GenerateZarinPalRefNumber()
        {
            // ZarinPal reference number is typically a long number
            return _random.Next(100000000, 999999999).ToString();
        }

        private string GenerateIranianCardPan()
        {
            // Iranian card numbers start with specific BIN numbers
            // Common Iranian banks: 603799 (Melli), 627353 (Tejarat), 627961 (Sanat), 639607 (Eghtesad)
            var binPrefixes = new[] { "603799", "627353", "627961", "639607", "621986", "639347" };
            var binPrefix = binPrefixes[_random.Next(binPrefixes.Length)];

            // Generate remaining digits and mask the middle
            var lastFour = _random.Next(1000, 9999).ToString();
            return $"{binPrefix}******{lastFour}";
        }

        private string GetPaymentUrl(string authority)
        {
            return $"https://www.zarinpal.com/pg/StartPay/{authority}";
        }

        private void LogPaymentStatistics(List<Payment> payments)
        {
            var statistics = new
            {
                Total = payments.Count,
                Pending = payments.Count(p => p.Status == PaymentStatus.Pending),
                Paid = payments.Count(p => p.Status == PaymentStatus.Paid),
                PartiallyPaid = payments.Count(p => p.Status == PaymentStatus.PartiallyPaid),
                Failed = payments.Count(p => p.Status == PaymentStatus.Failed),
                Refunded = payments.Count(p => p.Status == PaymentStatus.Refunded),
                ZarinPal = payments.Count(p => p.Provider == Core.Domain.Enums.PaymentProvider.ZarinPal),
                IDPay = payments.Count(p => p.Provider == Core.Domain.Enums.PaymentProvider.IDPay),
                ZarinPalMethod = payments.Count(p => p.Method == PaymentMethod.ZarinPal),
                CreditCard = payments.Count(p => p.Method == PaymentMethod.CreditCard),
                Cash = payments.Count(p => p.Method == PaymentMethod.Cash)
            };

            _logger.LogInformation(
                "Payment Statistics: Total={Total}, Pending={Pending}, Paid={Paid}, " +
                "PartiallyPaid={PartiallyPaid}, Failed={Failed}, Refunded={Refunded}, " +
                "ZarinPal={ZarinPal}, IDPay={IDPay}, ZarinPalMethod={ZarinPalMethod}, " +
                "CreditCard={CreditCard}, Cash={Cash}",
                statistics.Total,
                statistics.Pending,
                statistics.Paid,
                statistics.PartiallyPaid,
                statistics.Failed,
                statistics.Refunded,
                statistics.ZarinPal,
                statistics.IDPay,
                statistics.ZarinPalMethod,
                statistics.CreditCard,
                statistics.Cash);
        }
    }
}
