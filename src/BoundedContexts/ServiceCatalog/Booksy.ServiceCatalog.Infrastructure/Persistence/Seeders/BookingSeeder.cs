using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds booking data with various scenarios for Iranian providers
    /// </summary>
    public sealed class BookingSeeder : ISeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<BookingSeeder> _logger;

        // Sample customer IDs (will be populated from existing users)
        private List<UserId> _customerIds = new();

        private readonly Random _random = new Random(98765);

        // Persian customer notes
        private readonly string[] _persianCustomerNotes = new[]
        {
            "لطفا دقیق باشید",
            "اولین باره که میام",
            "ممکنه 10 دقیقه دیرتر برسم",
            "محصولات طبیعی استفاده کنید",
            "پوست حساس دارم",
            "عکس مدل دارم نشون میدم",
            "جشن تولده!",
            "سالگرد ازدواجمونه",
            "حتما با همون آرایشگر قبلی",
            null, // Sometimes no note
            null
        };

        private readonly string[] _persianStaffNotes = new[]
        {
            "خدمات با موفقیت انجام شد",
            "مشتری بسیار راضی بود",
            "مشتری خواستار رزرو دوباره شد",
            "تغییرات جزئی طبق درخواست مشتری",
            "نتیجه عالی",
            "مشتری از نتیجه نهایی راضی بود",
            "نوبت بعدی توصیه شد",
            "مشتری محصولات خریداری کرد",
            "قرار ملاقات بدون مشکل",
            "ارتباط خوب در طول خدمات"
        };

        public BookingSeeder(
            ServiceCatalogDbContext context,
            ILogger<BookingSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await _context.Bookings.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Bookings already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting Iranian booking data seeding...");

                // Seed booking policies first
                await SeedBookingPoliciesToServicesAsync(cancellationToken);

                // Generate sample customer IDs (these should ideally come from UserManagement context)
                _customerIds = GenerateSampleCustomerIds(30);

                var providers = await _context.Providers
                    .Include(p => p.Staff)
                    .Include(p => p.Services)
                    .Where(p => p.Status == ProviderStatus.Active || p.Status == ProviderStatus.PendingVerification)
                    .ToListAsync(cancellationToken);

                if (!providers.Any())
                {
                    _logger.LogWarning("No providers found. Skipping booking seeding.");
                    return;
                }

                var allBookings = new List<Booking>();

                foreach (var provider in providers)
                {
                    if (!provider.Staff.Any() || !provider.Services.Any())
                        continue;

                    var providerBookings = await CreateBookingsForProviderAsync(provider, cancellationToken);
                    allBookings.AddRange(providerBookings);
                }

                await _context.Bookings.AddRangeAsync(allBookings, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully seeded {Count} bookings across {ProviderCount} providers",
                    allBookings.Count,
                    providers.Count);

                LogBookingStatistics(allBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding Iranian booking data");
                throw;
            }
        }

        private async Task SeedBookingPoliciesToServicesAsync(CancellationToken cancellationToken)
        {
            var services = await _context.Services.ToListAsync(cancellationToken);

            foreach (var service in services)
            {
                // Assign different policies based on service price (in IRR)
                if (service.BasePrice.Amount < 1000000) // Less than 1M IRR
                {
                    service.SetBookingPolicy(BookingPolicy.Flexible);
                }
                else if (service.BasePrice.Amount > 3000000) // More than 3M IRR
                {
                    service.SetBookingPolicy(BookingPolicy.Strict);
                }
                else
                {
                    service.SetBookingPolicy(BookingPolicy.Default);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Booking policies assigned to {Count} services", services.Count);
        }

        private List<UserId> GenerateSampleCustomerIds(int count)
        {
            var customerIds = new List<UserId>();
            for (int i = 0; i < count; i++)
            {
                customerIds.Add(UserId.From(Guid.NewGuid()));
            }
            return customerIds;
        }

        private async Task<List<Booking>> CreateBookingsForProviderAsync(
            Domain.Aggregates.Provider provider,
            CancellationToken cancellationToken)
        {
            var bookings = new List<Booking>();
            var staff = provider.Staff.Where(s => s.IsActive).ToList();
            var services = await _context.Services
                .Where(s => s.ProviderId == provider.Id)
                .ToListAsync(cancellationToken);

            if (!staff.Any() || !services.Any())
                return bookings;

            // Create 20-35 bookings per provider
            var bookingCount = _random.Next(20, 36);

            for (int i = 0; i < bookingCount; i++)
            {
                var customer = _customerIds[_random.Next(_customerIds.Count)];
                var service = services[_random.Next(services.Count)];
                var staffMember = staff[_random.Next(staff.Count)];

                // Create bookings at different times (past, present, future)
                var daysOffset = _random.Next(-60, 90); // From 60 days ago to 90 days ahead
                var hour = _random.Next(9, 20); // Business hours 9 AM - 8 PM
                var minute = _random.Next(0, 4) * 15; // 0, 15, 30, 45 minutes
                var startTime = DateTime.UtcNow.Date
                    .AddDays(daysOffset)
                    .AddHours(hour)
                    .AddMinutes(minute);

                var booking = CreateBookingWithScenario(
                    customer,
                    provider.Id,
                    service.Id,
                    staffMember.Id,
                    startTime,
                    service.Duration,
                    service.BasePrice,
                    service.BookingPolicy ?? BookingPolicy.Default,
                    daysOffset);

                if (booking != null)
                {
                    bookings.Add(booking);
                }
            }

            return bookings;
        }

        private Booking? CreateBookingWithScenario(
            UserId customerId,
            ProviderId providerId,
            ServiceId serviceId,
            Guid staffId,
            DateTime startTime,
            Duration duration,
            Price price,
            BookingPolicy policy,
            int daysOffset)
        {
            try
            {
                var scenario = DetermineScenario(daysOffset);
                var customerNotes = GetRandomCustomerNote();

                var booking = Booking.CreateBookingRequest(
                    customerId,
                    providerId,
                    serviceId,
                    staffId,
                    startTime,
                    duration,
                    price,
                    policy,
                    customerNotes);

                ApplyScenario(booking, scenario);

                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create booking for time {StartTime}", startTime);
                return null;
            }
        }

        private BookingScenario DetermineScenario(int daysOffset)
        {
            // Past bookings (more than 1 day ago)
            if (daysOffset < -1)
            {
                var pastScenarios = new[]
                {
                    BookingScenario.Completed,
                    BookingScenario.Completed,
                    BookingScenario.Completed,
                    BookingScenario.Completed, // Most past bookings are completed
                    BookingScenario.NoShow,
                    BookingScenario.Cancelled
                };
                return pastScenarios[_random.Next(pastScenarios.Length)];
            }
            // Yesterday
            else if (daysOffset == -1)
            {
                return _random.Next(100) < 85 ? BookingScenario.Completed : BookingScenario.NoShow;
            }
            // Today
            else if (daysOffset == 0)
            {
                var todayScenarios = new[]
                {
                    BookingScenario.Confirmed,
                    BookingScenario.Confirmed,
                    BookingScenario.Requested
                };
                return todayScenarios[_random.Next(todayScenarios.Length)];
            }
            // Near future (1-14 days)
            else if (daysOffset <= 14)
            {
                var nearFutureScenarios = new[]
                {
                    BookingScenario.Confirmed,
                    BookingScenario.Confirmed,
                    BookingScenario.Confirmed,
                    BookingScenario.Requested,
                    BookingScenario.CancelledByCustomer
                };
                return nearFutureScenarios[_random.Next(nearFutureScenarios.Length)];
            }
            // Far future
            else
            {
                var farFutureScenarios = new[]
                {
                    BookingScenario.Confirmed,
                    BookingScenario.Requested,
                    BookingScenario.Requested,
                    BookingScenario.Requested
                };
                return farFutureScenarios[_random.Next(farFutureScenarios.Length)];
            }
        }

        private void ApplyScenario(Booking booking, BookingScenario scenario)
        {
            switch (scenario)
            {
                case BookingScenario.Requested:
                    // Already in requested state
                    break;

                case BookingScenario.Confirmed:
                    if (booking.Policy.RequireDeposit)
                    {
                        booking.ProcessDepositPayment($"pi_iranian_seed_{Guid.NewGuid():N}");
                    }
                    booking.Confirm();
                    break;

                case BookingScenario.Completed:
                    if (booking.Policy.RequireDeposit)
                    {
                        booking.ProcessDepositPayment($"pi_iranian_seed_{Guid.NewGuid():N}");
                    }
                    booking.Confirm();

                    if (!booking.PaymentInfo.IsFullyPaid())
                    {
                        booking.ProcessFullPayment($"pi_full_iranian_seed_{Guid.NewGuid():N}");
                    }

                    booking.Complete(GetRandomStaffNote());
                    break;

                case BookingScenario.NoShow:
                    if (booking.Policy.RequireDeposit)
                    {
                        booking.ProcessDepositPayment($"pi_iranian_seed_{Guid.NewGuid():N}");
                    }
                    booking.Confirm();
                    booking.MarkAsNoShow("مشتری به قرار ملاقات نیامد");
                    break;

                case BookingScenario.Cancelled:
                case BookingScenario.CancelledByCustomer:
                    var cancelReasons = new[]
                    {
                        "درخواست لغو توسط مشتری",
                        "تداخل در برنامه",
                        "اتفاق شخصی",
                        "خدمات دیگری پیدا کرد",
                        "دیگر نیاز نیست"
                    };
                    booking.Cancel(cancelReasons[_random.Next(cancelReasons.Length)]);
                    break;

                case BookingScenario.CancelledByProvider:
                    if (booking.Policy.RequireDeposit && _random.Next(100) < 50)
                    {
                        booking.ProcessDepositPayment($"pi_iranian_seed_{Guid.NewGuid():N}");
                    }
                    booking.Cancel("ارائه‌دهنده مجبور به تغییر زمان شد", byProvider: true);

                    if (booking.PaymentInfo.IsDepositPaid())
                    {
                        var refundAmount = Money.Create(
                            booking.PaymentInfo.PaidAmount.Amount,
                            booking.PaymentInfo.PaidAmount.Currency);
                        booking.ProcessRefund(refundAmount, $"re_iranian_seed_{Guid.NewGuid():N}", "بازپرداخت به دلیل لغو توسط ارائه‌دهنده");
                    }
                    break;
            }
        }

        private string? GetRandomCustomerNote()
        {
            return _persianCustomerNotes[_random.Next(_persianCustomerNotes.Length)];
        }

        private string GetRandomStaffNote()
        {
            return _persianStaffNotes[_random.Next(_persianStaffNotes.Length)];
        }

        private void LogBookingStatistics(List<Booking> bookings)
        {
            var statistics = new
            {
                Total = bookings.Count,
                Requested = bookings.Count(b => b.Status == BookingStatus.Requested),
                Confirmed = bookings.Count(b => b.Status == BookingStatus.Confirmed),
                Completed = bookings.Count(b => b.Status == BookingStatus.Completed),
                Cancelled = bookings.Count(b => b.Status == BookingStatus.Cancelled),
                NoShow = bookings.Count(b => b.Status == BookingStatus.NoShow),
                WithDeposit = bookings.Count(b => b.PaymentInfo.IsDepositPaid()),
                FullyPaid = bookings.Count(b => b.PaymentInfo.IsFullyPaid())
            };

            _logger.LogInformation(
                "Booking Statistics: Total={Total}, Requested={Requested}, Confirmed={Confirmed}, " +
                "Completed={Completed}, Cancelled={Cancelled}, NoShow={NoShow}, " +
                "WithDeposit={WithDeposit}, FullyPaid={FullyPaid}",
                statistics.Total,
                statistics.Requested,
                statistics.Confirmed,
                statistics.Completed,
                statistics.Cancelled,
                statistics.NoShow,
                statistics.WithDeposit,
                statistics.FullyPaid);
        }

        private enum BookingScenario
        {
            Requested,
            Confirmed,
            Completed,
            NoShow,
            Cancelled,
            CancelledByCustomer,
            CancelledByProvider
        }
    }
}
