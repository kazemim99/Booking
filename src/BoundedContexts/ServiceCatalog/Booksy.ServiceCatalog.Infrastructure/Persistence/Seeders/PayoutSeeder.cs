using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.PayoutAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds provider payout records with Iranian bank information
    /// </summary>
    public sealed class PayoutSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<PayoutSeeder> _logger;
        private readonly Random _random = new Random(33445);

        // Iranian bank names
        private readonly string[] _iranianBanks = new[]
        {
            "بانک ملی ایران",
            "بانک تجارت",
            "بانک صنعت و معدن",
            "بانک اقتصاد نوین",
            "بانک پاسارگاد",
            "بانک کشاورزی",
            "بانک ملت",
            "بانک سپه",
            "بانک صادرات",
            "بانک رفاه",
            "بانک سامان",
            "بانک سرمایه",
            "بانک شهر",
            "بانک دی",
            "بانک آینده"
        };

        public PayoutSeeder(
            ServiceCatalogDbContext context,
            ILogger<PayoutSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.Payouts.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Payouts already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian payout data seeding...");

                // Get providers who have captured payments
                var providers = await _context.Providers
                    .Where(p => p.Status == Domain.Enums.ProviderStatus.Active)
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogWarning("No active providers found for payout seeding.");
                    return;
                }

                // Get paid payments grouped by provider
                var paidPayments = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Paid)
                    .ToListAsync(cancellationToken);

                if (!paidPayments.Any())
                {
                    _logger.LogWarning("No paid payments found for payout seeding.");
                    return;
                }

                var payouts = new List<Payout>();

                // Create payouts for each provider for the last 3 months
                var now = DateTime.UtcNow;

                foreach (var provider in providers)
                {
                    var providerPayments = paidPayments
                        .Where(p => p.ProviderId == provider.Id)
                        .ToList();

                    if (!providerPayments.Any())
                        continue;

                    // Create monthly payouts for the last 3 months
                    for (int i = 1; i <= 3; i++)
                    {
                        var periodEnd = now.AddMonths(-i).Date.AddDays(-now.AddMonths(-i).Day + 1).AddMonths(1).AddDays(-1);
                        var periodStart = periodEnd.AddDays(-periodEnd.Day + 1);

                        // Get payments for this period
                        var periodPayments = providerPayments
                            .Where(p => p.CapturedAt.HasValue &&
                                       p.CapturedAt.Value >= periodStart &&
                                       p.CapturedAt.Value <= periodEnd)
                            .ToList();

                        if (periodPayments.Any())
                        {
                            var payout = CreatePayoutForProvider(
                                provider.Id,
                                periodPayments,
                                periodStart,
                                periodEnd);

                            if (payout != null)
                            {
                                payouts.Add(payout);
                            }
                        }
                    }
                }

                await _context.Payouts.AddRangeAsync(payouts, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully seeded {Count} Iranian payouts for {ProviderCount} providers",
                    payouts.Count,
                    providers.Count);

                LogPayoutStatistics(payouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian payout data");
                throw;
            }
        }

        private Payout? CreatePayoutForProvider(
            ProviderId providerId,
            List<Domain.Aggregates.PaymentAggregate.Payment> payments,
            DateTime periodStart,
            DateTime periodEnd)
        {
            try
            {
                if (!payments.Any())
                    return null;

                // Calculate gross amount (total of all payments)
                var grossAmount = Money.Create(
                    payments.Sum(p => p.Amount.Amount),
                    "IRR");

                // Calculate commission (10-15% based on provider tier)
                var commissionRate = 0.10m + (_random.Next(0, 6) * 0.01m); // 10-15%
                var commissionAmount = Money.Create(
                    grossAmount.Amount * commissionRate,
                    "IRR");

                var paymentIds = payments.Select(p => p.Id).ToList();

                var notes = $"تسویه حساب دوره {periodStart:yyyy/MM/dd} - {periodEnd:yyyy/MM/dd}";

                var payout = Payout.Create(
                    providerId,
                    grossAmount,
                    commissionAmount,
                    periodStart,
                    periodEnd,
                    paymentIds,
                    notes,
                    null);

                // Prepare Iranian bank details for when payout is marked as paid
                var bankName = GetRandomIranianBank();
                var accountLast4 = _random.Next(1000, 9999).ToString();

                // Simulate payout status based on period
                var daysSincePeriodEnd = (DateTime.UtcNow - periodEnd).Days;

                if (daysSincePeriodEnd > 45) // More than 45 days ago
                {
                    // Old payouts are mostly paid
                    if (_random.Next(100) < 90) // 90% paid
                    {
                        payout.Schedule(periodEnd.AddDays(7));
                        payout.MarkAsProcessing($"iranian_payout_{Guid.NewGuid():N}", null);
                        payout.MarkAsPaid(accountLast4, bankName);
                    }
                    else if (_random.Next(100) < 50) // Some failed
                    {
                        payout.Schedule(periodEnd.AddDays(7));
                        payout.MarkAsProcessing($"iranian_payout_{Guid.NewGuid():N}", null);
                        payout.MarkAsFailed("خطا در انتقال وجه به حساب بانکی");
                    }
                }
                else if (daysSincePeriodEnd > 15) // 15-45 days ago
                {
                    if (_random.Next(100) < 70) // 70% paid
                    {
                        payout.Schedule(periodEnd.AddDays(7));
                        payout.MarkAsProcessing($"iranian_payout_{Guid.NewGuid():N}", null);
                        payout.MarkAsPaid(accountLast4, bankName);
                    }
                    else if (_random.Next(100) < 20) // 20% processing
                    {
                        payout.Schedule(periodEnd.AddDays(7));
                        payout.MarkAsProcessing($"iranian_payout_{Guid.NewGuid():N}", null);
                    }
                    else // 10% scheduled (pending)
                    {
                        payout.Schedule(periodEnd.AddDays(7));
                    }
                }
                else // Recent period
                {
                    if (_random.Next(100) < 40) // 40% scheduled (pending)
                    {
                        payout.Schedule(periodEnd.AddDays(7));
                    }
                    // Else remains pending
                }

                return payout;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create payout for provider {ProviderId}", providerId);
                return null;
            }
        }

        private string GetRandomIranianBank()
        {
            return _iranianBanks[_random.Next(_iranianBanks.Length)];
        }

        private void LogPayoutStatistics(List<Payout> payouts)
        {
            var statistics = new
            {
                Total = payouts.Count,
                Pending = payouts.Count(p => p.Status == PayoutStatus.Pending),
                Processing = payouts.Count(p => p.Status == PayoutStatus.Processing),
                Paid = payouts.Count(p => p.Status == PayoutStatus.Paid),
                Failed = payouts.Count(p => p.Status == PayoutStatus.Failed),
                Cancelled = payouts.Count(p => p.Status == PayoutStatus.Cancelled),
                OnHold = payouts.Count(p => p.Status == PayoutStatus.OnHold),
                TotalGrossAmount = payouts.Sum(p => p.GrossAmount.Amount),
                TotalCommissionAmount = payouts.Sum(p => p.CommissionAmount.Amount),
                TotalNetAmount = payouts.Sum(p => p.NetAmount.Amount)
            };

            _logger.LogInformation(
                "Payout Statistics: Total={Total}, Pending={Pending}, Processing={Processing}, " +
                "Paid={Paid}, Failed={Failed}, Cancelled={Cancelled}, OnHold={OnHold}, " +
                "TotalGross={TotalGrossAmount:N0} IRR, TotalCommission={TotalCommissionAmount:N0} IRR, " +
                "TotalNet={TotalNetAmount:N0} IRR",
                statistics.Total,
                statistics.Pending,
                statistics.Processing,
                statistics.Paid,
                statistics.Failed,
                statistics.Cancelled,
                statistics.OnHold,
                statistics.TotalGrossAmount,
                statistics.TotalCommissionAmount,
                statistics.TotalNetAmount);
        }
    }
}
