
using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.ServiceCatalog.Application.Exceptions;
using Booksy.ServiceCatalog.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.AddStaffToProvider;

public sealed class AddStaffToProviderCommandHandler
    : ICommandHandler<AddStaffToProviderCommand, AddStaffToProviderResult>
{
    private readonly IProviderWriteRepository _providerRepository;  // ✅ Only Provider repository!
    private readonly ILogger<AddStaffToProviderCommandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AddStaffToProviderCommandHandler(
        IProviderWriteRepository providerRepository,
        ILogger<AddStaffToProviderCommandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AddStaffToProviderResult> Handle(
        AddStaffToProviderCommand request,
        CancellationToken cancellationToken)
    {

        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        _logger.LogInformation(
            "Adding staff member '{FirstName} {LastName}' to provider {ProviderId}",
            request.FirstName,
            request.LastName,
            request.ProviderId);

        // Validate request
        ValidateRequest(request);
        var usreId = UserId.From(userId);
        var provider = await _providerRepository.GetByOwnerIdAsync(usreId, cancellationToken);

        if (provider == null)
            throw new KeyNotFoundException($"Provider {request.ProviderId} not found");

        // Create value objects
        var phone = !string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? PhoneNumber.Create(request.PhoneNumber)
            : null;
        var role = ParseRole(request.Role);

        // ✅ Add staff through Provider aggregate root
        var staff = provider.AddStaff(request.FirstName, request.LastName, role, phone);

        // Update notes if provided
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            provider.UpdateStaffNotes(staff.Id, request.Notes);
        }

        // ✅ Save Provider aggregate (EF Core cascades to staff)
        await _providerRepository.UpdateProviderAsync(provider, cancellationToken);

        _logger.LogInformation(
            "Staff {StaffId} added successfully to provider {ProviderId}",
            staff.Id,
            provider.Id);

        return new AddStaffToProviderResult(
            provider.Id.Value,
            staff.Id,
            staff.FirstName,
            staff.LastName,
            staff.Role.ToString(),
            staff.IsActive,
            staff.HiredAt);
    }

    private void ValidateRequest(AddStaffToProviderCommand request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = new List<string> { "First name is required" };

        if (request.FirstName?.Length > 100)
            errors["firstName"] = new List<string> { "First name cannot exceed 100 characters" };

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = new List<string> { "Last name is required" };

        if (request.LastName?.Length > 100)
            errors["lastName"] = new List<string> { "Last name cannot exceed 100 characters" };

        if (string.IsNullOrWhiteSpace(request.Role))
            errors["role"] = new List<string> { "Role is required" };

        if (errors.Any())
            throw new ValidationException(errors);
    }

    private StaffRole ParseRole(string role)
    {
        if (Enum.TryParse<StaffRole>(role, true, out var staffRole))
            return staffRole;

        return role.ToLowerInvariant() switch
        {
            "owner" => StaffRole.Owner,
            "manager" => StaffRole.Manager,
            "receptionist" => StaffRole.Receptionist,
            "serviceprovider" or "service-provider" or "provider" => StaffRole.ServiceProvider,
            "specialist" => StaffRole.Specialist,
            "assistant" => StaffRole.Assistant,
            "cleaner" => StaffRole.Cleaner,
            "security" => StaffRole.Security,
            "maintenance" => StaffRole.Maintenance,
            _ => StaffRole.ServiceProvider // Default
        };
    }
}
