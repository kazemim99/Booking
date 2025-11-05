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

public sealed class SaveStep8FeedbackCommandHandler
    : ICommandHandler<SaveStep8FeedbackCommand, SaveStep8FeedbackResult>
{
    private readonly IProviderWriteRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SaveStep8FeedbackCommandHandler(
        IProviderWriteRepository providerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SaveStep8FeedbackResult> Handle(
        SaveStep8FeedbackCommand request,
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

        // Store feedback (could be saved to a separate feedback table or as provider metadata)
        // For now, we'll just log it and mark the step as complete
        if (!string.IsNullOrWhiteSpace(request.FeedbackText))
        {
            // TODO: Store feedback in a dedicated feedback table or send to analytics
            Console.WriteLine($"Provider {providerId} feedback: {request.FeedbackText}");
        }

        // Update registration step
        provider.UpdateRegistrationStep(8);

        await _unitOfWork.CommitAsync(cancellationToken);

        var message = string.IsNullOrWhiteSpace(request.FeedbackText)
            ? "Feedback step completed (no feedback provided)"
            : "Thank you for your feedback!";

        return new SaveStep8FeedbackResult(
            provider.Id.Value,
            8,
            message);
    }
}
