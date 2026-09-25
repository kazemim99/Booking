// ========================================
// AsanRezerve.ServiceCatalog.Application/Services/Interfaces/IServiceApplicationService.cs
// ========================================
using AsanRezerve.ServiceCatalog.Application.DTOs.Service;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Application.Services.Interfaces
{
    public interface IServiceApplicationService
    {
        Task<ServiceDto?> GetServiceByIdAsync(ServiceId serviceId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceSummaryDto>> GetServicesByProviderAsync(ProviderId providerId, ServiceStatus? status = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceSummaryDto>> GetServicesByCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceSummaryDto>> SearchServicesAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<bool> IsServiceNameUniqueForProviderAsync(ProviderId providerId, string serviceName, ServiceId? excludeServiceId = null, CancellationToken cancellationToken = default);
        Task<ServiceStatisticsDto> GetServiceStatisticsAsync(ServiceId serviceId, CancellationToken cancellationToken = default);
        Task<bool> CanServiceBeActivatedAsync(ServiceId serviceId, CancellationToken cancellationToken = default);
        Task<Price> CalculateServicePriceAsync(ServiceId serviceId, IEnumerable<Guid> selectedOptionIds, Guid? selectedTierId = null, CancellationToken cancellationToken = default);
    }
}