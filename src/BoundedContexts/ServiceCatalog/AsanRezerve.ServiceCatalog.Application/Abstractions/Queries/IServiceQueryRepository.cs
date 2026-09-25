using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.DTOs;
using AsanRezerve.Core.Domain.Abstractions.Entities;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using System.Linq.Expressions;

namespace AsanRezerve.ServiceCatalog.Application.Abstractions.Queries
{
    /// <summary>
    /// Application-specific query repository for services - Following UserManagement pattern
    /// </summary>
    public interface IServiceQueryRepository : IQueryRepositoryBase<Service, ServiceId>
    {
        // Statistics queries
        Task<BookingStatistics> GetBookingStatisticsAsync(
            Guid serviceId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        Task<RevenueStatistics> GetRevenueStatisticsAsync(
            Guid serviceId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
    }

    // Statistics DTOs
    public sealed class BookingStatistics
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int NoShowBookings { get; set; }
    }

    public sealed class RevenueStatistics
    {
        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenue { get; set; }
        public int TotalTransactions { get; set; }
    }
}
