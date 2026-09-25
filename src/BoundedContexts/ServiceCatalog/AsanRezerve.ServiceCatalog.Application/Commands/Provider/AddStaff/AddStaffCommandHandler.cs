//// ========================================
//// AsanRezerve.ServiceCatalog.Application/Commands/Provider/AddStaff/AddStaffCommandHandler.cs
//// ========================================
//using AsanRezerve.Core.Application.Abstractions.CQRS;
//using AsanRezerve.Core.Application.Abstractions.Persistence;
//using AsanRezerve.Core.Domain.ValueObjects;
//using AsanRezerve.ServiceCatalog.Domain.Exceptions;
//using AsanRezerve.ServiceCatalog.Domain.Repositories;
//using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
//using Microsoft.Extensions.Logging;

//namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.AddStaff
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