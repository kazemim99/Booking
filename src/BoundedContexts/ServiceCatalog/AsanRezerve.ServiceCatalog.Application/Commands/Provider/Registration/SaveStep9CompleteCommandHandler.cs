using AsanRezerve.Core.Application.Abstractions.CQRS;
using AsanRezerve.Core.Application.Abstractions.Persistence;
using AsanRezerve.Core.Application.Abstractions.Services;
using AsanRezerve.Core.Application.Exceptions;
using AsanRezerve.Core.Domain.Abstractions;
using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Enums;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AsanRezerve.ServiceCatalog.Application.Commands.Provider.Registration;

public sealed class SaveStep9CompleteCommandHandler
    : ICommandHandler<SaveStep9CompleteCommand, SaveStep9CompleteResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IServiceWriteRepository _serviceRepository;
    private readonly IOrganizationMembershipRepository _membershipRepository;
    private readonly IServiceCatalogUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<SaveStep9CompleteCommandHandler> _logger;

    public SaveStep9CompleteCommandHandler(
        IProviderWriteRepository providerRepository,
        IServiceWriteRepository serviceRepository,
        IOrganizationMembershipRepository membershipRepository,
        IServiceCatalogUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ITokenService tokenService,
        ILogger<SaveStep9CompleteCommandHandler> logger)
    {
        _providerRepository = providerRepository;
        _serviceRepository = serviceRepository;
        _membershipRepository = membershipRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<SaveStep9CompleteResult> Handle(
        SaveStep9CompleteCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(_currentUserService.UserId ??
            throw new UnauthorizedAccessException("User not authenticated"));

        var providerId = ProviderId.From(request.ProviderId);
        var provider = await _providerRepository.GetByIdAsync(providerId, cancellationToken);

        // Every check below answers the caller: the draft is missing something, or is not theirs,
        // or is already complete. They were InvalidOperationExceptions, which
        // ExceptionHandlingMiddleware does not map, so "you still need business hours" reached the
        // registration wizard as a 500 with no field to point at.
        if (provider == null)
            throw new NotFoundException("Provider", request.ProviderId);

        if (provider.OwnerId != userId)
            throw new ForbiddenException("You are not authorized to complete this registration");

        if (provider.Status != ProviderStatus.Drafted)
            throw new DomainValidationException(nameof(provider.Status), "Provider is not in draft status");

        // Validate required data
        if (!provider.BusinessHours.Any())
            throw new DomainValidationException("BusinessHours", "Business hours are required to complete registration");

        // Check services from ServiceRepository (Service is separate aggregate)
        var services = await _serviceRepository.GetServicesByProviderIdAsync(providerId, cancellationToken);
        if (!services.Any())
            throw new DomainValidationException("Services", "At least one service is required to complete registration");

        if (string.IsNullOrWhiteSpace(provider.Profile.BusinessName))
            throw new DomainValidationException("BusinessName", "Business name is required");

        if (provider.Address == null)
            throw new DomainValidationException("Address", "Business address is required");

        if (provider.ContactInfo?.Email == null)
            throw new InvalidOperationException("Contact email is required");

        // Complete registration - transitions to PendingVerification
        provider.CompleteRegistration();
        provider.UpdateRegistrationStep(9);

        // Guarantee the owner has a membership. This is the durable anchor of the
        // membership model: it must not depend on the client's follow-up
        // "do you provide services?" call (which only refines this to +StaffProvider).
        // Idempotent — skip if one already exists.
        var ownerMembership = await _membershipRepository.GetActiveByPersonAndOrganizationAsync(
            userId, provider.Id, cancellationToken);
        if (ownerMembership is null)
        {
            ownerMembership = OrganizationMembership.CreateOwner(userId, provider.Id, providesServices: false);
            await _membershipRepository.SaveAsync(ownerMembership, cancellationToken);
            _logger.LogInformation(
                "Created owner membership {MembershipId} for provider {ProviderId}",
                ownerMembership.Id, provider.Id.Value);
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        // Generate new token with updated status (PendingVerification)
        var tokenResponse = await _tokenService.GenerateTokenWithProviderClaimsAsync(
            userId.Value,
            provider.Id.Value,
            provider.Status.ToString(),
            cancellationToken);

        _logger.LogInformation(
            "Registration completed for provider {ProviderId} with new status {Status}. Generated new authentication tokens.",
            provider.Id.Value,
            provider.Status);

        return new SaveStep9CompleteResult(
            ProviderId: provider.Id.Value,
            Status: provider.Status.ToString(),
            Message: "Registration completed successfully. Your provider profile is now pending admin verification.",
            AccessToken: tokenResponse.AccessToken,
            RefreshToken: tokenResponse.RefreshToken
        );
    }
}
