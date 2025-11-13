// ========================================
// Booksy.UserManagement.Application/EventHandlers/IntegrationEventHandlers/ProviderDraftCreatedEventSubscriber.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Repositories;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.EventHandlers.IntegrationEventHandlers;

/// <summary>
/// Handles ProviderDraftCreatedIntegrationEvent from ServiceCatalog context
/// Updates User.Profile with owner's first name and last name when provider draft is created (Step 3)
/// Uses Unit of Work to ensure transactional consistency
/// </summary>
public sealed class ProviderDraftCreatedEventSubscriber : ICapSubscribe
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProviderDraftCreatedEventSubscriber> _logger;

    public ProviderDraftCreatedEventSubscriber(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProviderDraftCreatedEventSubscriber> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribes to ProviderDraftCreatedIntegrationEvent
    /// Topic: booksy.servicecatalog.providerdraftcreated
    /// Group: booksy.usermanagement (defined in CAP configuration)
    /// </summary>
    [CapSubscribe("booksy.servicecatalog.providerdraftcreated")]
    public async Task HandleAsync(ProviderDraftCreatedIntegrationEvent @event)
    {
        _logger.LogInformation(
            "📨 Received ProviderDraftCreatedIntegrationEvent for User {UserId} ({FirstName} {LastName}), Provider {ProviderId}",
            @event.OwnerId,
            @event.OwnerFirstName,
            @event.OwnerLastName,
            @event.ProviderId);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var userId = UserId.From(@event.OwnerId);
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning(
                        "⚠️ User {UserId} not found for provider draft created event",
                        @event.OwnerId);
                    return;
                }

                // Update user profile with owner's first and last name
                _logger.LogInformation(
                    "📝 Updating User {UserId} profile: FirstName={FirstName}, LastName={LastName}",
                    @event.OwnerId,
                    @event.OwnerFirstName,
                    @event.OwnerLastName);

                user.Profile.UpdatePersonalInfo(@event.OwnerFirstName, @event.OwnerLastName, middleName: null);

                await _userRepository.UpdateAsync(user, CancellationToken.None);
            });

            _logger.LogInformation(
                "✅ Successfully updated User {UserId} profile with owner name: {FirstName} {LastName}",
                @event.OwnerId,
                @event.OwnerFirstName,
                @event.OwnerLastName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to handle ProviderDraftCreatedIntegrationEvent for User {UserId}",
                @event.OwnerId);

            // ExecuteInTransactionAsync automatically rolls back on exception
            // CAP will retry this message based on retry policy
            throw;
        }
    }
}

/// <summary>
/// Integration event DTO from ServiceCatalog context
/// This is a cross-context contract - changes must be coordinated
/// </summary>
public sealed record ProviderDraftCreatedIntegrationEvent(
    Guid ProviderId,
    Guid OwnerId,
    string OwnerFirstName,
    string OwnerLastName,
    string BusinessName,
    DateTime CreatedAt);
