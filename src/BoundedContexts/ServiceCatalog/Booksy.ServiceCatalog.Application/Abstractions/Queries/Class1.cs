using Booksy.Core.Application.DTOs;
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.ServiceCatalog.Domain.Aggregates;
using System.Linq.Expressions;

namespace Booksy.ServiceCatalog.Application.Abstractions.Queries
{
    /// <summary>
    /// Application-specific query repository for services - Following UserManagement pattern
    /// </summary>
    public interface IServiceQueryRepository
    {
        // Paginated queries with projections
        Task<PagedResult<TResult>> GetPagedAsync<TResult>(
            IAdvancedSpecification<Service> specification,
            Expression<Func<Service, TResult>> selector,
            CancellationToken cancellationToken = default);

        // List queries with projections  
        Task<IReadOnlyList<TResult>> GetListAsync<TResult>(
            ISpecification<Service> specification,
            Expression<Func<Service, TResult>> selector,
            CancellationToken cancellationToken = default);

        // Single item query with projection
        Task<TResult?> GetSingleAsync<TResult>(
            ISpecification<Service> specification,
            Expression<Func<Service, TResult>> selector,
            CancellationToken cancellationToken = default);

        // Count query
        Task<int> CountAsync(
            ISpecification<Service> specification,
            CancellationToken cancellationToken = default);

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
