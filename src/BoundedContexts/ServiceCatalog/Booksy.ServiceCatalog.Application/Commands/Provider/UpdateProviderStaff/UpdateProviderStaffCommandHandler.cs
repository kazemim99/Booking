//// ========================================
//// Application/Commands/Provider/UpdateProviderStaff/UpdateProviderStaffCommandHandler.cs
//// ========================================
//using Booksy.Core.Application.Abstractions.CQRS;
//using Booksy.Core.Domain.ValueObjects;
//using Booksy.ServiceCatalog.Domain.Enums;
//using Booksy.ServiceCatalog.Domain.Repositories;
//using Booksy.ServiceCatalog.Domain.ValueObjects;

//namespace Booksy.ServiceCatalog.Application.Commands.Provider.UpdateProviderStaff
//{
//    /// <summary>
//    /// Handler for UpdateProviderStaffCommand - updates staff through Provider aggregate
//    /// ✅ DDD-Compliant: Uses IProviderWriteRepository only and calls provider.UpdateStaff()
//    /// </summary>
//    internal sealed class UpdateProviderStaffCommandHandler
//        : ICommandHandler<UpdateProviderStaffCommand, UpdateProviderStaffResult>
//    {
//        private readonly IProviderWriteRepository _providerRepository;

//        public UpdateProviderStaffCommandHandler(IProviderWriteRepository providerRepository)
//        {
//            _providerRepository = providerRepository;
//        }

//        public async Task<UpdateProviderStaffResult> Handle(
//            UpdateProviderStaffCommand request,
//            CancellationToken cancellationToken)
//        {
//            // ✅ Load Provider aggregate (not Staff directly)
//            var providerId = ProviderId.From(request.ProviderId);
//            var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

//            if (provider == null)
//                throw new KeyNotFoundException($"Provider {request.ProviderId} not found");

//            // ✅ Hierarchy constraint: Only Organizations can have staff members
//            if (provider.HierarchyType == ProviderHierarchyType.Individual)
//                throw new InvalidOperationException(
//                    "Individual providers cannot have staff members. Convert to Organization first.");

//            // ✅ Update staff through Provider aggregate root (only if core fields are provided)
//            if (!string.IsNullOrWhiteSpace(request.FirstName) &&
//                !string.IsNullOrWhiteSpace(request.LastName) &&
//                !string.IsNullOrWhiteSpace(request.Email) &&
//                !string.IsNullOrWhiteSpace(request.Role))
//            {
//                var email = Email.Create(request.Email);
//                var phone = !string.IsNullOrWhiteSpace(request.PhoneNumber)
//                    ? PhoneNumber.From(request.PhoneNumber)
//                    : null;
//                var role = ParseRole(request.Role);

//                provider.UpdateStaff(
//                    request.StaffId,
//                    request.FirstName,
//                    request.LastName,
//                    email,
//                    phone,
//                    role);
//            }

//            // Update notes if provided
//            if (request.Notes != null)
//                provider.UpdateStaffNotes(request.StaffId, request.Notes);

//            // Update biography if provided
//            if (request.Biography != null)
//                provider.UpdateStaffBiography(request.StaffId, request.Biography);

//            // Update profile photo if provided
//            if (request.ProfilePhotoUrl != null)
//                provider.UpdateStaffProfilePhoto(request.StaffId, request.ProfilePhotoUrl);

//            // ✅ Save Provider aggregate (EF Core cascades to staff)
//            await _providerRepository.UpdateProviderAsync(provider, cancellationToken);

//            // Get updated staff for response
//            var updatedStaff = provider.GetStaffById(request.StaffId);
//            if (updatedStaff == null)
//                throw new InvalidOperationException($"Staff {request.StaffId} not found after update");

//            return new UpdateProviderStaffResult(
//                provider.Id.Value,
//                updatedStaff.Id,
//                updatedStaff.FirstName,
//                updatedStaff.LastName,
//                updatedStaff.Email.Value,
//                updatedStaff.Role.ToString(),
//                updatedStaff.IsActive,
//                DateTime.UtcNow);
//        }

//        private static StaffRole ParseRole(string roleString)
//        {
//            return roleString.ToLowerInvariant() switch
//            {
//                "provider" => StaffRole.Maintenance,
//                "manager" => StaffRole.Manager,
//                "specialist" => StaffRole.Specialist,
//                "assistant" => StaffRole.Assistant,
//                "receptionist" => StaffRole.Receptionist,
//                _ => throw new ArgumentException($"Invalid staff role: {roleString}", nameof(roleString))
//            };
//        }
//    }
//}
