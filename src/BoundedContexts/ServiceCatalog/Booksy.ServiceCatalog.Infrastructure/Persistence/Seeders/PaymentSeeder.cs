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
    /// DTO for booking data needed for payment seeding (avoids EF Core tracking issues with owned entities)
    /// </summary>
    internal class BookingPaymentData
    {
        public BookingId BookingId { get; init; } = null!;
        public UserId CustomerId { get; init; } = null!;
        public ProviderId ProviderId { get; init; } = null!;
        public decimal TotalAmount { get; init; }
        public Domain.Enums.BookingStatus Status { get; init; }
    }

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
                // Clear existing payments to avoid tracking conflicts
                var existingPayments = await _context.Payments.ToListAsync(cancellationToken);
                if (existingPayments.Any())
                {
                    _logger.LogInformation("Clearing {Count} existing payments before reseeding", existingPayments.Count);
                    _context.Payments.RemoveRange(existingPayments);
                    await _context.SaveChangesAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                }

                _logger.LogInformation("Starting Iranian payment data seeding...");

                // Get completed and confirmed bookings that need payments - use AsNoTracking and project to DTOs
                // to avoid EF Core tracking owned entities which causes conflicts
                var bookingData = await _context.Bookings
                    .AsNoTracking()
                    .Where(b => b.Status == Domain.Enums.BookingStatus.Completed ||
                               b.Status == Domain.Enums.BookingStatus.Confirmed)
                    .Select(b => new BookingPaymentData
                    {
                        BookingId = b.Id,
                        CustomerId = b.CustomerId,
                        ProviderId = b.ProviderId,
                        TotalAmount = b.TotalPrice.Amount,
                        Status = b.Status
                    })
                    .ToListAsync(cancellationToken);

                if (!bookingData.Any())
                {
                    _logger.LogWarning("No bookings found for payment seeding.");
                    return;
                }

                // Clear change tracker to start fresh
                _context.ChangeTracker.Clear();

                var payments = new List<Payment>();

                foreach (var data in bookingData)
                {
                    // Create payment using the projected data (no entity references)
                    var payment = CreatePaymentFromData(data);
                    if (payment != null)
                    {
                        payments.Add(payment);
                    }
                }

                // Process payments one by one to avoid EF Core owned entity tracking conflicts
                // When multiple Payment entities with owned Money types are added together,
                // EF Core can get confused tracking owned entities with identical values
                foreach (var payment in payments)
                {
                    await _context.Payments.AddAsync(payment, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                }

                _logger.LogInformation("Successfully seeded {Count} Iranian payments", payments.Count);
                LogPaymentStatistics(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian payment data");
                throw;
            }
        }

        private Payment? CreatePaymentFromData(BookingPaymentData data)
        {
            try
            {
                var amount = Money.Create(data.TotalAmount, "IRR");

                // Determine payment method (mostly Iranian methods)
                var paymentMethod = GetRandomIranianPaymentMethod();

                // Mostly ZarinPal for Iranian payments
                var paymentProvider = _random.Next(100) < 80
                    ? PaymentProvider.ZarinPal
                    : GetRandomIranianPaymentProvider();

                var description = $"پرداخت برای رزرو شماره {data.BookingId}";

                var payment = Payment.Create(
                    data.BookingId,
                    data.CustomerId,
                    data.ProviderId,
                    amount,
                    paymentMethod,
                    paymentProvider,
                    description,
                    null);

                // Simulate payment flow based on booking status
                if (data.Status == Domain.Enums.BookingStatus.Completed)
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
                else if (data.Status == Domain.Enums.BookingStatus.Confirmed)
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
                _logger.LogWarning(ex, "Failed to create payment for booking {BookingId}", data.BookingId);
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
