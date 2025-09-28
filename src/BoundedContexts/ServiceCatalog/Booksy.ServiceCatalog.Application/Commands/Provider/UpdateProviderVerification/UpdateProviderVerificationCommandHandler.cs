////===========================================
//// Provider Query Handlers and Specifications
////===========================================

////===========================================
//// Queries/Provider/SearchProviders/SearchProvidersQueryHandler.cs
////===========================================
//using Booksy.Core.Application.Abstractions.CQRS;
//using Booksy.ServiceCatalog.Domain.Exceptions;
//using Booksy.ServiceCatalog.Domain.Repositories;
//using Microsoft.Extensions.Logging;

//namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateProviderVerification
//{
//    public sealed class UpdateProviderVerificationCommandHandler : ICommandHandler<UpdateProviderVerificationCommand, UpdateProviderVerificationResult>
//    {
//        private readonly IProviderWriteRepository _providerWriteRepository;
//        private readonly IProviderReadRepository _providerReadRepository;
//        private readonly ILogger<UpdateProviderVerificationCommandHandler> _logger;

//        public UpdateProviderVerificationCommandHandler(
//            IProviderWriteRepository providerWriteRepository,
//            IProviderReadRepository providerReadRepository,
//            ILogger<UpdateProviderVerificationCommandHandler> logger)
//        {
//            _providerWriteRepository = providerWriteRepository;
//            _providerReadRepository = providerReadRepository;
//            _logger = logger;
//        }

//        public async Task<UpdateProviderVerificationResult> Handle(
//            UpdateProviderVerificationCommand request,
//            CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Updating verification status for provider {ProviderId} to {IsVerified}",
//                request.ProviderId, request.IsVerified);

//            var providerId = ProviderId.From(request.ProviderId);
//            var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

//            if (provider == null)
//                throw new InvalidProviderException("Provider not found");

//            if (request.IsVerified)
//            {
//                provider.Verify(request.VerificationNotes);
//            }
//            else
//            {
//                provider.Unverify(request.VerificationNotes);
//            }

//            await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);

//            _logger.LogInformation("Provider {ProviderId} verification status updated to {IsVerified}",
//                provider.Id, request.IsVerified);

//            return new UpdateProviderVerificationResult(
//                ProviderId: provider.Id.Value,
//                IsVerified: provider.IsVerified,
//                UpdatedAt: DateTime.UtcNow);
//        }
//    }
//}