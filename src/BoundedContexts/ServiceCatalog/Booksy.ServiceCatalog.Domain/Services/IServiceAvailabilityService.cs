// ========================================
// Booksy.ServiceCatalog.Domain/Services/IServiceAvailabilityService.cs
// ========================================
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Services
{
    /// <summary>
    /// Domain service for service availability calculations
    /// </summary>
    public interface IServiceAvailabilityService
    {
        Task<bool> IsServiceAvailableOnDateAsync(ServiceId serviceId, DateOnly date, CancellationToken cancellationToken = default);
        Task<IEnumerable<TimeOnly>> GetAvailableTimeSlotsAsync(ServiceId serviceId, DateOnly date, Guid? preferredStaffId = null, CancellationToken cancellationToken = default);
        Task<bool> CanServiceBeBookedAtTimeAsync(ServiceId serviceId, DateTime appointmentDateTime, Duration duration, CancellationToken cancellationToken = default);
        Task<IEnumerable<Guid>> GetAvailableStaffForServiceAsync(ServiceId serviceId, DateTime appointmentDateTime, Duration duration, CancellationToken cancellationToken = default);
        Task<Duration> CalculateTotalServiceDurationAsync(ServiceId serviceId, IEnumerable<Guid> selectedOptionIds, CancellationToken cancellationToken = default);
        Task<Price> CalculateTotalServicePriceAsync(ServiceId serviceId, IEnumerable<Guid> selectedOptionIds, Guid? selectedTierId = null, CancellationToken cancellationToken = default);
    }
}