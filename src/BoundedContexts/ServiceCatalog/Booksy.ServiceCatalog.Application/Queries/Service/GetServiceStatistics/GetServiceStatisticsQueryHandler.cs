using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Abstractions.Queries;
using Booksy.ServiceCatalog.Application.Queries.Service.GetServiceStatistics;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.Extensions.Logging;

public sealed class GetServiceStatisticsQueryHandler : IQueryHandler<GetServiceStatisticsQuery, ServiceStatisticsViewModel>
{
    private readonly IServiceReadRepository _serviceRepository;
    private readonly IServiceQueryRepository _serviceQueryRepository;
    private readonly ILogger<GetServiceStatisticsQueryHandler> _logger;

    public GetServiceStatisticsQueryHandler(
        IServiceReadRepository serviceRepository,
        IServiceQueryRepository serviceQueryRepository,
        ILogger<GetServiceStatisticsQueryHandler> logger)
    {
        _serviceRepository = serviceRepository;
        _serviceQueryRepository = serviceQueryRepository;
        _logger = logger;
    }

    public async Task<ServiceStatisticsViewModel> Handle(GetServiceStatisticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting statistics for service {ServiceId}", request.ServiceId);

        var service = await _serviceRepository.GetByIdAsync(ServiceId.Create(request.ServiceId), cancellationToken);
        if (service == null)
        {
            throw new ServiceNotFoundException(request.ServiceId);
        }

        var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-12);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        // Get statistics using application-specific repository methods
        var bookingStats = await _serviceQueryRepository.GetBookingStatisticsAsync(
            request.ServiceId, startDate, endDate, cancellationToken);

        var revenueStats = await _serviceQueryRepository.GetRevenueStatisticsAsync(
            request.ServiceId, startDate, endDate, cancellationToken);

        return new ServiceStatisticsViewModel
        {
            ServiceId = service.Id.Value,
            ServiceName = service.Name,
            Category = service.Category.ToString(),
            BasePrice = service.BasePrice.Amount,
            Currency = service.BasePrice.Currency,
            Status = service.Status,
            CreatedAt = service.CreatedAt,

            // Booking Statistics
            TotalBookings = bookingStats.TotalBookings,
            CompletedBookings = bookingStats.CompletedBookings,
            CancelledBookings = bookingStats.CancelledBookings,
            NoShowBookings = bookingStats.NoShowBookings,
            BookingCompletionRate = bookingStats.TotalBookings > 0
                ? (double)bookingStats.CompletedBookings / bookingStats.TotalBookings * 100
                : 0,

            // Revenue Statistics
            TotalRevenue = revenueStats.TotalRevenue,
            AverageBookingValue = bookingStats.CompletedBookings > 0
                ? revenueStats.TotalRevenue / bookingStats.CompletedBookings
                : 0,

            // Period and metadata
            StatisticsPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            LastUpdated = DateTime.UtcNow
        };
    }
}
