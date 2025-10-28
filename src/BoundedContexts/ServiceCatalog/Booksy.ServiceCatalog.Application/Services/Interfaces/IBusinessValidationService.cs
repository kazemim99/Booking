// ========================================
// Booksy.ServiceCatalog.Application/Services/Interfaces/IBusinessValidationService.cs
// ========================================
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider;

namespace Booksy.ServiceCatalog.Application.Services.Interfaces
{
    /// <summary>
    /// Application service for business validation rules
    /// </summary>
    public interface IBusinessValidationService
    {
        Task ValidateRegistrationAsync(RegisterProviderCommand command, CancellationToken cancellationToken = default);
        //Task ValidateServiceCreationAsync(CreateServiceCommand command, CancellationToken cancellationToken = default);
        Task<bool> ValidateBusinessHoursAsync(Dictionary<DayOfWeek, (TimeOnly Open, TimeOnly Close)?> businessHours, CancellationToken cancellationToken = default);
        Task<bool> ValidateServicePricingAsync(decimal basePrice, string currency, decimal? depositPercentage = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetValidationErrorsAsync<T>(T command, CancellationToken cancellationToken = default) where T : class;
    }
}