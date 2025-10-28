//// ========================================
//// Booksy.ServiceCatalog.Application/Commands/Service/CreateService/CreateServiceCommandHandler.cs
//// ========================================
//using Booksy.Core.Application.Abstractions.CQRS;
//using Booksy.Core.Application.Abstractions.Persistence;
//using Booksy.ServiceCatalog.Application.Services.Interfaces;
//using Booksy.ServiceCatalog.Domain.Aggregates;
//using Booksy.ServiceCatalog.Domain.Repositories;
//using Booksy.ServiceCatalog.Domain.ValueObjects;
//using Microsoft.Extensions.Logging;

//namespace Booksy.ServiceCatalog.Application.Commands.Service.CreateService
//{
//    public sealed class CreateServiceCommandHandler : ICommandHandler<CreateServiceCommand, CreateServiceResult>
//    {
//        private readonly IServiceWriteRepository _serviceWriteRepository;
//        private readonly IProviderReadRepository _providerReadRepository;
//        private readonly IBusinessValidationService _validationService;
//        private readonly ILogger<CreateServiceCommandHandler> _logger;

//        public CreateServiceCommandHandler(
//            IServiceWriteRepository serviceWriteRepository,
//            IProviderReadRepository providerReadRepository,
//            IBusinessValidationService validationService,
//            ILogger<CreateServiceCommandHandler> logger)
//        {
//            _serviceWriteRepository = serviceWriteRepository;
//            _providerReadRepository = providerReadRepository;
//            _validationService = validationService;
//            _logger = logger;
//        }

//        public async Task<CreateServiceResult> Handle(
//            CreateServiceCommand request,
//            CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Creating new service for provider: {ProviderId}", request.ProviderId);

//            // Validate provider exists and is active
//            var providerId = ProviderId.Create(request.ProviderId);
//            var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);
//            if (provider == null)
//            {
//                throw new InvalidOperationException($"Provider {request.ProviderId} not found");
//            }

//            if (!provider.CanAcceptBookings())
//            {
//                throw new InvalidOperationException("Provider must be active to create services");
//            }

//            // Validate business rules
//            await _validationService.ValidateServiceCreationAsync(request, cancellationToken);

//            // Create value objects
//            var category = ServiceCategory.Create(request.CategoryName);
//            var price = Price.Create(request.BasePrice, request.Currency);
//            var duration = Duration.FromMinutes(request.DurationMinutes);
//            var preparationTime = request.PreparationMinutes.HasValue
//                ? Duration.FromMinutes(request.PreparationMinutes.Value)
//                : null;
//            var bufferTime = request.BufferMinutes.HasValue
//                ? Duration.FromMinutes(request.BufferMinutes.Value)
//                : null;

//            // Create service aggregate
//            var service = ServiceCatalog.Domain.Aggregates.Service.Create(
//                providerId,
//                request.Name,
//                request.Description,
//                category,
//                request.ServiceType,
//                price,
//                duration);

//            // Configure service settings
//            service.UpdateDuration(duration, preparationTime, bufferTime);
//            service.UpdateAvailability(request.AvailableAtLocation, request.AvailableAsMobile);
//            service.UpdateBookingRules(
//                request.MaxAdvanceBookingDays,
//                request.MinAdvanceBookingHours,
//                request.MaxConcurrentBookings);

//            if (request.RequiresDeposit)
//            {
//                service.EnableDeposit(request.DepositPercentage);
//            }

//            if (!string.IsNullOrEmpty(request.ImageUrl))
//            {
//                service.SetImage(request.ImageUrl);
//            }

           

//            // Save to repository
//            await _serviceWriteRepository.SaveServiceAsync(service, cancellationToken);

//            _logger.LogInformation("Service created successfully. ServiceId: {ServiceId}", service.Id);

//            return new CreateServiceResult(
//                ServiceId: service.Id.Value,
//                Name: service.Name,
//                Category: service.Category.Name,
//                BasePrice: service.BasePrice.Amount,
//                Currency: service.BasePrice.Currency,
//                Duration: service.Duration.Value,
//                Status: service.Status,
//                CreatedAt: service.CreatedAt);
//        }
//    }
//}