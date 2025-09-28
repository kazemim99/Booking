using Autofac.Core;
using Booksy.Infrastructure.Core.Persistence.Base;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
using Booksy.ServiceCatalog.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Booksy.ServiceCatalog.Infrastructure.Queries
{
    public class ServiceQueryRepository : QueryRepositoryBase<Service, ServiceId>, IServiceQueryRepository
    {
        private ServiceCatalogDbContext _context;
        private readonly ILogger<ServiceQueryRepository> _logger;
        public ServiceQueryRepository(ServiceCatalogDbContext dbContext, ILogger<ServiceQueryRepository> logger) : base(dbContext)
        {
            _context = dbContext;
            _logger = logger;
        }



        public async Task<BookingStatistics> GetBookingStatisticsAsync(
      Guid serviceId,
      DateTime startDate,
      DateTime endDate,
      CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting booking statistics for service {ServiceId} from {StartDate} to {EndDate}",
                serviceId, startDate, endDate);

            // Note: This assumes there's a related Booking context that we can access
            // In a real implementation, you might need to use integration events or cross-context queries
            // For now, I'll implement a placeholder that could be replaced with actual booking data

            var serviceExists = await _context.Services
                .AsNoTracking()
                .AnyAsync(s => s.Id.Value == serviceId, cancellationToken);

            if (!serviceExists)
            {
                _logger.LogWarning("Service {ServiceId} not found for booking statistics", serviceId);
                return new BookingStatistics
                {
                    TotalBookings = 0,
                    CompletedBookings = 0,
                    CancelledBookings = 0,
                    NoShowBookings = 0
                };
            }

            // TODO: Replace with actual booking data from Booking context
            // This is a placeholder implementation
            var random = new Random(serviceId.GetHashCode());
            var totalBookings = random.Next(10, 100);
            var completedBookings = (int)(totalBookings * 0.8);
            var cancelledBookings = (int)(totalBookings * 0.15);
            var noShowBookings = totalBookings - completedBookings - cancelledBookings;

            var statistics = new BookingStatistics
            {
                TotalBookings = totalBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                NoShowBookings = noShowBookings
            };

            _logger.LogDebug("Booking statistics calculated: {@Statistics}", statistics);

            return statistics;
        }

        public async Task<RevenueStatistics> GetRevenueStatisticsAsync(
            Guid serviceId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting revenue statistics for service {ServiceId} from {StartDate} to {EndDate}",
                serviceId, startDate, endDate);

            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id.Value == serviceId, cancellationToken);

            if (service == null)
            {
                _logger.LogWarning("Service {ServiceId} not found for revenue statistics", serviceId);
                return new RevenueStatistics
                {
                    TotalRevenue = 0,
                    AverageRevenue = 0,
                    TotalTransactions = 0
                };
            }

            // TODO: Replace with actual transaction data from Payment/Booking context
            // This is a placeholder implementation
            var bookingStats = await GetBookingStatisticsAsync(serviceId, startDate, endDate, cancellationToken);
            var basePrice = service.BasePrice.Amount;

            var totalTransactions = bookingStats.CompletedBookings;
            var totalRevenue = totalTransactions * basePrice;
            var averageRevenue = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

            var statistics = new RevenueStatistics
            {
                TotalRevenue = totalRevenue,
                AverageRevenue = averageRevenue,
                TotalTransactions = totalTransactions
            };

            _logger.LogDebug("Revenue statistics calculated: {@Statistics}", statistics);

            return statistics;
        }

        // Additional helper methods for complex queries

        /// <summary>
        /// Gets service performance metrics for dashboard
        /// </summary>
        public async Task<ServicePerformanceMetrics> GetServicePerformanceAsync(
            Guid serviceId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting performance metrics for service {ServiceId}", serviceId);

            var service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id.Value == serviceId, cancellationToken);

            if (service == null)
            {
                throw new InvalidOperationException($"Service with ID {serviceId} not found");
            }

            var bookingStats = await GetBookingStatisticsAsync(serviceId, startDate, endDate, cancellationToken);
            var revenueStats = await GetRevenueStatisticsAsync(serviceId, startDate, endDate, cancellationToken);

            var completionRate = bookingStats.TotalBookings > 0
                ? (double)bookingStats.CompletedBookings / bookingStats.TotalBookings * 100
                : 0;

            var cancellationRate = bookingStats.TotalBookings > 0
                ? (double)bookingStats.CancelledBookings / bookingStats.TotalBookings * 100
                : 0;

            return new ServicePerformanceMetrics
            {
                ServiceId = serviceId,
                ServiceName = service.Name,
                Period = new DateRange(startDate, endDate),
                BookingCompletionRate = Math.Round(completionRate, 2),
                CancellationRate = Math.Round(cancellationRate, 2),
                TotalRevenue = revenueStats.TotalRevenue,
                AverageRevenuePerBooking = revenueStats.AverageRevenue,
                TotalBookings = bookingStats.TotalBookings,
                PopularityScore = CalculatePopularityScore(bookingStats, service)
            };
        }

        /// <summary>
        /// Gets aggregated statistics for multiple services
        /// </summary>
        public async Task<ProviderServicesSummary> GetProviderServicesSummaryAsync(
            Guid providerId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting services summary for provider {ProviderId}", providerId);

            var services = await _context.Services
                .AsNoTracking()
                .Where(s => s.ProviderId.Value == providerId && s.Status == ServiceStatus.Active)
                .Select(s => new { s.Id.Value, s.Name, s.BasePrice.Amount })
                .ToListAsync(cancellationToken);

            var totalServices = services.Count;
            var totalBookings = 0;
            var totalRevenue = 0m;

            // Aggregate statistics for all services
            foreach (var service in services)
            {
                var bookingStats = await GetBookingStatisticsAsync(service.Value, startDate, endDate, cancellationToken);
                var revenueStats = await GetRevenueStatisticsAsync(service.Value, startDate, endDate, cancellationToken);

                totalBookings += bookingStats.TotalBookings;
                totalRevenue += revenueStats.TotalRevenue;
            }

            return new ProviderServicesSummary
            {
                ProviderId = providerId,
                TotalServices = totalServices,
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                AverageRevenuePerService = totalServices > 0 ? totalRevenue / totalServices : 0,
                Period = new DateRange(startDate, endDate)
            };
        }

        private static double CalculatePopularityScore(BookingStatistics bookingStats, Service service)
        {
            // Simple popularity calculation - can be enhanced based on business rules
            var bookingScore = bookingStats.TotalBookings * 0.6;
            var completionScore = bookingStats.TotalBookings > 0
                ? (double)bookingStats.CompletedBookings / bookingStats.TotalBookings * 0.4 * 100
                : 0;

            return Math.Round(bookingScore + completionScore, 2);
        }

    }

    public sealed class ServicePerformanceMetrics
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateRange Period { get; set; } = null!;
        public double BookingCompletionRate { get; set; }
        public double CancellationRate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerBooking { get; set; }
        public int TotalBookings { get; set; }
        public double PopularityScore { get; set; }
    }

    public sealed class ProviderServicesSummary
    {
        public Guid ProviderId { get; set; }
        public int TotalServices { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerService { get; set; }
        public DateRange Period { get; set; } = null!;
    }

    public sealed class DateRange
    {
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public DateRange(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("Start date cannot be greater than end date");

            StartDate = startDate;
            EndDate = endDate;
        }

        public int DurationInDays => (EndDate - StartDate).Days + 1;
    }

}
