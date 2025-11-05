using Booksy.Core.Application.Abstractions.CQRS;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 5: Save staff/team members to provider draft
/// Requires Step 3 to be completed (provider draft must exist)
/// </summary>
public sealed record SaveStep5StaffCommand(
    Guid ProviderId,
    List<StaffMemberDto> StaffMembers,
    Guid? IdempotencyKey = null
) : ICommand<SaveStep5StaffResult>;

public sealed record StaffMemberDto(
    string Name,
    string Email,
    string PhoneNumber,
    string Position
);

public sealed record SaveStep5StaffResult(
    Guid ProviderId,
    int RegistrationStep,
    int StaffCount,
    string Message
);
