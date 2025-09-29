// ========================================
// Booksy.ServiceCatalog.Application/Services/Implementations/BusinessValidationService.cs
// ========================================
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Application.Commands.Provider.RegisterProvider;
using Booksy.ServiceCatalog.Application.Commands.Service.CreateService;
using Booksy.ServiceCatalog.Application.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Services.Implementations
{
    public sealed class BusinessValidationService : IBusinessValidationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BusinessValidationService> _logger;

        public BusinessValidationService(
            IServiceProvider serviceProvider,
            ILogger<BusinessValidationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ValidateRegistrationAsync(RegisterProviderCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating provider registration for: {BusinessName}", command.BusinessName);

            var validator = _serviceProvider.GetService<IValidator<RegisterProviderCommand>>();
            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());
                    throw new DomainValidationException($"Registration validation failed", errors);
                }
            }
        }

        public async Task ValidateServiceCreationAsync(CreateServiceCommand command, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating service creation for: {ServiceName}", command.Name);

            var validator = _serviceProvider.GetService<IValidator<CreateServiceCommand>>();
            if (validator != null)
            {
                var validationResult = await validator.ValidateAsync(command, cancellationToken);
                if (!validationResult.IsValid)
                {
 var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());       
                    
                    throw new DomainValidationException($"Registration validation failed", errors);
                }
            }

            // Additional service-specific validation
            await ValidateServiceBusinessRulesAsync(command);
        }

        public async Task<bool> ValidateBusinessHoursAsync(
            Dictionary<DayOfWeek, (TimeOnly Open, TimeOnly Close)?> businessHours,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            // Validate business hours make sense
            foreach (var hours in businessHours.Where(h => h.Value.HasValue))
            {
                if (hours.Value!.Value.Open >= hours.Value.Value.Close)
                {
                    return false;
                }

                // Business rule: Must be open at least 4 hours per day
                var duration = hours.Value.Value.Close - hours.Value.Value.Open;
                if (duration.TotalHours < 4)
                {
                    return false;
                }
            }

            // Business rule: Must be open at least 3 days per week
            var openDays = businessHours.Count(h => h.Value.HasValue);
            return openDays >= 3;
        }

        public async Task<bool> ValidateServicePricingAsync(
            decimal basePrice, string currency, decimal? depositPercentage = null,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            // Business rules for pricing
            if (basePrice <= 0) return false;
            if (basePrice > 10000) return false; // Max service price

            if (depositPercentage.HasValue)
            {
                if (depositPercentage.Value <= 0 || depositPercentage.Value > 100) return false;
            }

            var validCurrencies = new[] { "USD", "EUR", "GBP", "CAD", "AUD" };
            return validCurrencies.Contains(currency.ToUpperInvariant());
        }

        public async Task<IEnumerable<string>> GetValidationErrorsAsync<T>(T command, CancellationToken cancellationToken = default) where T : class
        {
            var validator = _serviceProvider.GetService<IValidator<T>>();
            if (validator == null) return Enumerable.Empty<string>();

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            return validationResult.Errors.Select(e => e.ErrorMessage);
        }

       

        private async Task ValidateServiceBusinessRulesAsync(CreateServiceCommand command)
        {
            await Task.CompletedTask;

            // Rule: Premium services must have higher minimum price
            if (command.ServiceType == ServiceType.Premium && command.BasePrice < 100)
            {
                throw new DomainValidationException("Premium services must have a base price of at least $100");
            }

            // Rule: Mobile services have duration limits
            if (command.AvailableAsMobile && command.DurationMinutes > 240) // 4 hours
            {
                throw new DomainValidationException("Mobile services cannot exceed 4 hours duration");
            }
        }
    }
}