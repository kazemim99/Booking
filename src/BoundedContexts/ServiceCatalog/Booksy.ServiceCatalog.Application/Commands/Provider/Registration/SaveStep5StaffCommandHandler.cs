using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.Abstractions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

public sealed class SaveStep5StaffCommandHandler
    : ICommandHandler<SaveStep5StaffCommand, SaveStep5StaffResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveStep5StaffCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SaveStep5StaffResult> Handle(
        SaveStep5StaffCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        if (provider == null)
            throw new InvalidOperationException("Provider not found");

        if (provider.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to update this provider");

        if (provider.Status != ProviderStatus.Drafted)
            throw new InvalidOperationException("Provider is not in draft status");

        // Staff is optional during registration
        // Remove existing staff members (deactivate them)
        var existingStaff = provider.Staff.ToList();
        foreach (var staff in existingStaff)
        {
            provider.RemoveStaff(staff.Id, "Updating staff during registration");
        }

        // Add new staff members
        foreach (var staffDto in request.StaffMembers)
        {
            // Split name into first and last
            var nameParts = staffDto.Name.Split(' ', 2, StringSplitOptions.TrimEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : staffDto.Name;
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var staffPhone = PhoneNumber.Create(staffDto.PhoneNumber);

            // Map position string to StaffRole enum (default to ServiceProvider)
            var role = MapPositionToRole(staffDto.Position);

            provider.AddStaff(firstName, lastName, role, staffPhone);
        }

        // Update registration step
        provider.UpdateRegistrationStep(5);

        await _unitOfWork.CommitAsync(cancellationToken);

        return new SaveStep5StaffResult(
            provider.Id.Value,
            5,
            request.StaffMembers.Count,
            $"{request.StaffMembers.Count} staff member(s) saved successfully");
    }

    private static StaffRole MapPositionToRole(string position)
    {
        return position.ToLower() switch
        {
            "owner" => StaffRole.Owner,
            "manager" => StaffRole.Manager,
            "receptionist" => StaffRole.Receptionist,
            "service provider" or "provider" or "stylist" or "therapist" => StaffRole.ServiceProvider,
            "specialist" => StaffRole.Specialist,
            "assistant" => StaffRole.Assistant,
            "cleaner" => StaffRole.Cleaner,
            "security" => StaffRole.Security,
            "maintenance" => StaffRole.Maintenance,
            _ => StaffRole.ServiceProvider // Default
        };
    }
}
