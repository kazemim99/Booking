using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.BookingAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Infrastructure.Persistence.Seeders
{
    /// <summary>
    /// Seeds sample booking data for testing and demonstration
    /// </summary>
    public sealed class BookingDataSeeder
    {
        private readonly ServiceCatalogDbContext _context;
        private readonly ILogger<BookingDataSeeder> _logger;

        // Sample customer IDs (in production, these would come from UserManagement)
        private readonly List<UserId> _sampleCustomers;

        public BookingDataSeeder(
            ServiceCatalogDbContext context,
            ILogger<BookingDataSeeder> logger)
        {
            _context = context;
            _logger = logger;

            // Create sample customer IDs
            _sampleCustomers = new List<UserId>
            {
                UserId.From(Guid.NewGuid()), // Customer 1
                UserId.From(Guid.NewGuid()), // Customer 2
                UserId.From(Guid.NewGuid()), // Customer 3
                UserId.From(Guid.NewGuid()), // Customer 4
                UserId.From(Guid.NewGuid())  // Customer 5
            };
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if bookings already exist
                if (await _context.Bookings.AnyAsync(cancellationToken))
                {
                    _logger.LogInformation("Bookings already seeded. Skipping...");
                    return;
                }

                _logger.LogInformation("Starting booking data seeding...");

                // Seed booking policies to services first
                await SeedBookingPoliciesToServicesAsync(cancellationToken);

                // Get existing data
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

                var bookings = new List<Booking>();

                // Seed different types of bookings for each provider
                foreach (var provider in providers)
                {
                    if (!provider.Staff.Any() || !provider.Services.Any())
                        continue;

                    var providerBookings = await CreateBookingsForProviderAsync(provider, cancellationToken);
                    bookings.AddRange(providerBookings);
                }

                await _context.Bookings.AddRangeAsync(bookings, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully seeded {Count} bookings across {ProviderCount} providers",
                    bookings.Count,
                    providers.Count);

                // Log booking statistics
                LogBookingStatistics(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding booking data");
                throw;
            }
        }

        private async Task SeedBookingPoliciesToServicesAsync(CancellationToken cancellationToken)
        {
            var services = await _context.Services.ToListAsync(cancellationToken);

            foreach (var service in services)
            {
                // Assign different policies based on service price
                if (service.BasePrice.Amount < 50)
                {
                    service.SetBookingPolicy(BookingPolicy.Flexible);
                }
                else if (service.BasePrice.Amount > 100)
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

            var random = new Random(provider.Id.Value.GetHashCode()); // Deterministic seed

            // Create 15-25 bookings per provider with various states
            var bookingCount = random.Next(15, 26);

            for (int i = 0; i < bookingCount; i++)
            {
                var customer = _sampleCustomers[random.Next(_sampleCustomers.Count)];
                var service = services[random.Next(services.Count)];
                var staffMember = staff[random.Next(staff.Count)];

                // Create bookings at different times (past, present, future)
                var daysOffset = random.Next(-30, 60); // From 30 days ago to 60 days ahead
                var hour = random.Next(9, 18); // Business hours 9 AM - 6 PM
                var startTime = DateTime.UtcNow.Date
                    .AddDays(daysOffset)
                    .AddHours(hour);

                var booking = CreateBookingWithScenario(
                    customer,
                    provider.Id,
                    service.Id,
                    staffMember.Id,
                    startTime,
                    service.Duration,
                    service.BasePrice,
                    service.BookingPolicy ?? BookingPolicy.Default,
                    daysOffset,
                    random);

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
            int daysOffset,
            Random random)
        {
            try
            {
                // Determine booking state based on time and randomization
                var scenario = DetermineScenario(daysOffset, random);

                var customerNotes = GetRandomCustomerNote(random);
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

                // Apply scenario-specific state transitions
                ApplyScenario(booking, scenario, random);

                return booking;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create booking for time {StartTime}", startTime);
                return null;
            }
        }

        private BookingScenario DetermineScenario(int daysOffset, Random random)
        {
            // Past bookings
            if (daysOffset < -1)
            {
                var pastScenarios = new[]
                {
                    BookingScenario.Completed,
                    BookingScenario.Completed,
                    BookingScenario.Completed, // Most likely
                    BookingScenario.NoShow,
                    BookingScenario.Cancelled
                };
                return pastScenarios[random.Next(pastScenarios.Length)];
            }
            // Recent past (yesterday)
            else if (daysOffset == -1)
            {
                return random.Next(100) < 80 ? BookingScenario.Completed : BookingScenario.NoShow;
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
                return todayScenarios[random.Next(todayScenarios.Length)];
            }
            // Near future (1-7 days)
            else if (daysOffset <= 7)
            {
                var nearFutureScenarios = new[]
                {
                    BookingScenario.Confirmed,
                    BookingScenario.Confirmed,
                    BookingScenario.Confirmed,
                    BookingScenario.Requested,
                    BookingScenario.CancelledByCustomer
                };
                return nearFutureScenarios[random.Next(nearFutureScenarios.Length)];
            }
            // Far future
            else
            {
                var farFutureScenarios = new[]
                {
                    BookingScenario.Confirmed,
                    BookingScenario.Requested,
                    BookingScenario.Requested
                };
                return farFutureScenarios[random.Next(farFutureScenarios.Length)];
            }
        }

        private void ApplyScenario(Booking booking, BookingScenario scenario, Random random)
        {
            switch (scenario)
            {
                case BookingScenario.Requested:
                    // Already in requested state, do nothing
                    break;

                case BookingScenario.Confirmed:
                    // Pay deposit if required
                    if (booking.Policy.RequireDeposit)
                    {
                        booking.ProcessDepositPayment($"pi_seed_{Guid.NewGuid():N}");
                    }
                    booking.Confirm();
                    break;

                case BookingScenario.Completed:
                    // Pay deposit, confirm, then complete
                    if (booking.Policy.RequireDeposit)
                    {
                        booking.ProcessDepositPayment($"pi_seed_{Guid.NewGuid():N}");
                    }
                    booking.Confirm();

                    // Pay remaining amount
                    if (!booking.PaymentInfo.IsFullyPaid())
                    {
                        booking.ProcessFullPayment($"pi_full_seed_{Guid.NewGuid():N}");
                    }

                    booking.Complete(GetRandomStaffNote(random));
                    break;

                case BookingScenario.NoShow:
                    // Confirm but customer doesn't show
                    if (booking.Policy.RequireDeposit)
                    {
                        booking.ProcessDepositPayment($"pi_seed_{Guid.NewGuid():N}");
                    }
                    booking.Confirm();
                    booking.MarkAsNoShow("Customer did not arrive for appointment");
                    break;

                case BookingScenario.Cancelled:
                case BookingScenario.CancelledByCustomer:
                    var cancelReasons = new[]
                    {
                        "Customer requested cancellation",
                        "Schedule conflict",
                        "Personal emergency",
                        "Found another service",
                        "No longer needed"
                    };
                    booking.Cancel(cancelReasons[random.Next(cancelReasons.Length)]);
                    break;

                case BookingScenario.CancelledByProvider:
                    if (booking.Policy.RequireDeposit && random.Next(100) < 50)
                    {
                        booking.ProcessDepositPayment($"pi_seed_{Guid.NewGuid():N}");
                    }
                    booking.Cancel("Provider had to reschedule", byProvider: true);

                    // Refund if payment was made
                    if (booking.PaymentInfo.IsDepositPaid())
                    {
                        var refundAmount = Money.Create(
                            booking.PaymentInfo.PaidAmount.Amount,
                            booking.PaymentInfo.PaidAmount.Currency);
                        booking.ProcessRefund(refundAmount, $"re_seed_{Guid.NewGuid():N}", "Provider cancellation refund");
                    }
                    break;
            }
        }

        private string? GetRandomCustomerNote(Random random)
        {
            var notes = new[]
            {
                null, // No notes sometimes
                null,
                "Please be gentle, sensitive skin",
                "First time customer",
                "Running 5 minutes late",
                "Prefer natural products",
                "Allergic to certain ingredients",
                "Looking for a specific style",
                "Have photos for reference",
                "Birthday treat!",
                "Anniversary surprise"
            };

            return notes[random.Next(notes.Length)];
        }

        private string GetRandomStaffNote(Random random)
        {
            var notes = new[]
            {
                "Service completed successfully",
                "Customer very satisfied",
                "Customer requested same stylist next time",
                "Minor adjustments made per customer request",
                "Excellent results achieved",
                "Customer loved the final look",
                "Recommended follow-up appointment",
                "Customer purchased retail products",
                "Smooth appointment, no issues",
                "Great communication throughout service"
            };

            return notes[random.Next(notes.Length)];
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
                Rescheduled = bookings.Count(b => b.Status == BookingStatus.Rescheduled),
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
