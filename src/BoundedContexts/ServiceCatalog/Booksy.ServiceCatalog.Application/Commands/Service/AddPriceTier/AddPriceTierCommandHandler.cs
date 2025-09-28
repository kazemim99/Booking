// ========================================
// Booksy.ServiceCatalog.Application/Commands/Service/AddPriceTier/AddPriceTierCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Service.AddPriceTier
{
    public sealed class AddPriceTierCommandHandler : ICommandHandler<AddPriceTierCommand, AddPriceTierResult>
    {
        private readonly IServiceWriteRepository _serviceWriteRepository;
        private readonly IServiceReadRepository _serviceReadRepository;
        private readonly ILogger<AddPriceTierCommandHandler> _logger;

        public AddPriceTierCommandHandler(
            IServiceWriteRepository serviceWriteRepository,
            IServiceReadRepository serviceReadRepository,
            ILogger<AddPriceTierCommandHandler> logger)
        {
            _serviceWriteRepository = serviceWriteRepository;
            _serviceReadRepository = serviceReadRepository;
            _logger = logger;
        }

        public async Task<AddPriceTierResult> Handle(
            AddPriceTierCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adding price tier to service: {ServiceId}", request.ServiceId);

            var serviceId = ServiceId.Create(request.ServiceId);
            var service = await _serviceReadRepository.GetByIdAsync(serviceId, cancellationToken);

            if (service == null)
                throw new InvalidServiceException("Service not found");

            var price = Price.Create(request.Price, request.Currency);
            service.AddPriceTier(request.Name, price, request.Description);

            var addedTier = service.PriceTiers.OrderByDescending(t => t.CreatedAt).First();

            // Set as default if requested and no default exists
            if (request.IsDefault || !service.PriceTiers.Any(t => t.IsDefault))
            {
                // Clear other defaults first
                foreach (var tier in service.PriceTiers.Where(t => t.IsDefault && t.Id != addedTier.Id))
                {
                    tier.UnsetAsDefault();
                }
                addedTier.SetAsDefault();
            }

            // Add attributes if provided
            if (request.Attributes?.Any() == true)
            {
                foreach (var attribute in request.Attributes)
                {
                    addedTier.SetAttribute(attribute.Key, attribute.Value);
                }
            }

            await _serviceWriteRepository.UpdateServiceAsync(service, cancellationToken);

            _logger.LogInformation("Price tier added: {ServiceId}, TierId: {TierId}", service.Id, addedTier.Id);

            return new AddPriceTierResult(
                ServiceId: service.Id.Value,
                TierId: addedTier.Id,
                Name: addedTier.Name,
                Price: addedTier.Price.Amount,
                Currency: addedTier.Price.Currency,
                IsDefault: addedTier.IsDefault,
                CreatedAt: addedTier.CreatedAt);
        }
    }
}