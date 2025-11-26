// ========================================
// Booksy.ServiceCatalog.Application/Commands/Provider/UpdateContactInfo/UpdateContactInfoCommandHandler.cs
// ========================================
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateContactInfo
{
    public sealed class UpdateContactInfoCommandHandler : ICommandHandler<UpdateContactInfoCommand, UpdateContactInfoResult>
    {
        private readonly IProviderWriteRepository _providerWriteRepository;
        private readonly IProviderReadRepository _providerReadRepository;
        private readonly ILogger<UpdateContactInfoCommandHandler> _logger;

        public UpdateContactInfoCommandHandler(
            IProviderWriteRepository providerWriteRepository,
            IProviderReadRepository providerReadRepository,
            ILogger<UpdateContactInfoCommandHandler> logger)
        {
            _providerWriteRepository = providerWriteRepository;
            _providerReadRepository = providerReadRepository;
            _logger = logger;
        }

        public async Task<UpdateContactInfoResult> Handle(
            UpdateContactInfoCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating contact info for provider: {ProviderId}", request.ProviderId);

            var providerId = ProviderId.From(request.ProviderId);
            var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

            if (provider == null)
                throw new InvalidProviderException("Provider not found");

            var email = Email.Create(request.Email);
            var primaryPhone = PhoneNumber.From(request.PrimaryPhone);
            var secondaryPhone = !string.IsNullOrEmpty(request.SecondaryPhone)
                ? PhoneNumber.From(request.SecondaryPhone)
                : null;

            var contactInfo = ContactInfo.Create(
                email,
                primaryPhone,
                secondaryPhone,
                request.Website,
                request.FacebookPage,
                request.InstagramHandle);

            provider.UpdateContactInfo(contactInfo);

            await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);

            _logger.LogInformation("Contact info updated for provider: {ProviderId}", provider.Id);

            return new UpdateContactInfoResult(
                ProviderId: provider.Id.Value,
                Email: email.Value,
                PrimaryPhone: primaryPhone.Value);
        }
    }
}