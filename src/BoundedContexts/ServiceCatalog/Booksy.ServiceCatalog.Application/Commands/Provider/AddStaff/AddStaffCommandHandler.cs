//// ========================================
//// Booksy.ServiceCatalog.Application/Commands/Provider/AddStaff/AddStaffCommandHandler.cs
//// ========================================
//using Booksy.Core.Application.Abstractions.CQRS;
//using Booksy.Core.Application.Abstractions.Persistence;
//using Booksy.Core.Domain.ValueObjects;
//using Booksy.ServiceCatalog.Domain.Exceptions;
//using Booksy.ServiceCatalog.Domain.Repositories;
//using Booksy.ServiceCatalog.Domain.ValueObjects;
//using Microsoft.Extensions.Logging;

//namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaff
//{
//    public sealed class AddStaffCommandHandler : ICommandHandler<AddStaffCommand, AddStaffResult>
//    {
//        private readonly IProviderWriteRepository _providerWriteRepository;
//        private readonly IProviderReadRepository _providerReadRepository;
//        private readonly ILogger<AddStaffCommandHandler> _logger;

//        public AddStaffCommandHandler(
//            IProviderWriteRepository providerWriteRepository,
//            IProviderReadRepository providerReadRepository,
//            ILogger<AddStaffCommandHandler> logger)
//        {
//            _providerWriteRepository = providerWriteRepository;
//            _providerReadRepository = providerReadRepository;
//            _logger = logger;
//        }

//        public async Task<AddStaffResult> Handle(
//            AddStaffCommand request,
//            CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Adding staff to provider: {ProviderId}", request.ProviderId);

//            var providerId = ProviderId.From(request.ProviderId);
//            var provider = await _providerReadRepository.GetByIdAsync(providerId, cancellationToken);

//            if (provider == null)
//                throw new InvalidProviderException("Provider not found");

//            var email = Email.Create(request.Email);
//            var phone = !string.IsNullOrEmpty(request.Phone) ? PhoneNumber.From(request.Phone) : null;


//            await _providerWriteRepository.UpdateProviderAsync(provider, cancellationToken);



//            return new AddStaffResult(
//                ProviderId: provider.Id.Value,
//                StaffId: addedStaff.Id,
//                FullName: addedStaff.FullName,
//                Email: addedStaff.Email.Value,
//                Role: addedStaff.Role,
//                HiredAt: addedStaff.HiredAt);
//        }
//    }
//}