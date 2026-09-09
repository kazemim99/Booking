using Booksy.Core.Application.Abstractions.CQRS;
using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Application.Exceptions;
using Booksy.Core.Domain.Exceptions;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Commands.Provider.AddStaffToProvider;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.ServiceCatalog.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.ServiceCatalog.Application.Commands.Provider.Registration;

/// <summary>
/// Step 5: put the team the owner listed during registration into the salon.
///
/// <para>This handler was commented out in its entirety during the move from the provider
/// hierarchy to memberships, and nothing replaced it — so <c>POST /registration/step-5/staff</c>
/// answered 500 ("No service for type IRequestHandler&lt;SaveStep5StaffCommand&gt;") on every call
/// while the wizard, the command, the result type and the endpoint all still existed. The team step
/// of provider registration simply did not work.</para>
///
/// <para>It adds nobody in a new way: each listed person goes through
/// <see cref="AddStaffToProviderCommand"/>, the same path the salon's own "add staff" screen uses,
/// so registration cannot drift from it. That path links the member to an existing person when the
/// phone number identifies one and otherwise creates an unclaimed membership the salon manages on
/// their behalf, records the audit entry, and makes them bookable. It sends nothing to the person —
/// no invitation is dispatched from here.</para>
/// </summary>
public sealed class SaveStep5StaffCommandHandler
    : ICommandHandler<SaveStep5StaffCommand, SaveStep5StaffResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SaveStep5StaffCommandHandler> _logger;

    public SaveStep5StaffCommandHandler(
        IProviderWriteRepository providerRepository,
        ISender sender,
        ICurrentUserService currentUserService,
        ILogger<SaveStep5StaffCommandHandler> logger)
    {
        _providerRepository = providerRepository;
        _sender = sender;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SaveStep5StaffResult> Handle(
        SaveStep5StaffCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated"));

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken)
            ?? throw new NotFoundException("Provider", request.ProviderId);

        if (provider.OwnerId != userId)
            throw new ForbiddenException("You are not authorized to update this provider");

        if (provider.Status != ProviderStatus.Drafted)
            throw new DomainValidationException(
                nameof(provider.Status), "Provider is not in draft status");

        foreach (var member in request.StaffMembers)
        {
            // The wizard collects one display name; the salon's team screen collects two. Split on
            // the first space, exactly as the endpoint that replaced this one does.
            var parts = (member.Name ?? string.Empty).Split(' ', 2, StringSplitOptions.TrimEntries);
            var firstName = parts.Length > 0 && parts[0].Length > 0 ? parts[0] : member.Name;
            var lastName = parts.Length > 1 ? parts[1] : string.Empty;

            await _sender.Send(
                new AddStaffToProviderCommand(
                    ProviderId: request.ProviderId,
                    FirstName: firstName ?? string.Empty,
                    LastName: lastName,
                    PhoneNumber: member.PhoneNumber,
                    CountryCode: null,
                    Role: member.Position),
                cancellationToken);
        }

        provider.UpdateRegistrationStep(5);
        await _providerRepository.UpdateProviderAsync(provider, cancellationToken);

        _logger.LogInformation(
            "Registration step 5: {Count} member(s) added to draft provider {ProviderId}",
            request.StaffMembers.Count, request.ProviderId);

        return new SaveStep5StaffResult(
            provider.Id.Value,
            5,
            request.StaffMembers.Count,
            $"{request.StaffMembers.Count} staff member(s) saved successfully");
    }
}
