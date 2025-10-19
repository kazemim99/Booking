// ========================================
// Booksy.UserManagement.Application/EventHandlers/IntegrationEventHandlers/ProviderRegisteredEventSubscriber.cs
// ========================================
using Booksy.Core.Application.Abstractions.Persistence;
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Enums;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace Booksy.UserManagement.Application.EventHandlers.IntegrationEventHandlers;

/// <summary>
/// Handles ProviderRegisteredIntegrationEvent from ServiceCatalog context
/// Updates user role to Provider and status to Pending when a provider registers
/// Uses Unit of Work to ensure transactional consistency
/// </summary>
public sealed class ProviderRegisteredEventSubscriber : ICapSubscribe
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProviderRegisteredEventSubscriber> _logger;

    public ProviderRegisteredEventSubscriber(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProviderRegisteredEventSubscriber> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribes to ProviderRegisteredIntegrationEvent
    /// Topic: booksy.servicecatalog.providerregistered
    /// Group: booksy.usermanagement (defined in CAP configuration)
    /// </summary>
    [CapSubscribe("booksy.servicecatalog.providerregistered")]
    public async Task HandleAsync(ProviderRegisteredIntegrationEvent @event)
    {
        _logger.LogInformation(
            "📨 Received ProviderRegisteredIntegrationEvent for User {UserId}, Provider {ProviderId}",
            @event.OwnerId,
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
                        "⚠️ User {UserId} not found for provider registration event",
                        @event.OwnerId);
                    return; 
                }

                if (!user.HasRole("Provider") && !user.HasRole("ServiceProvider"))
                {
                    _logger.LogInformation(
                        "➕ Adding Provider role to user {UserId}",
                        @event.OwnerId);

                    user.AddRole("Provider");
                }

                if (user.Status == UserStatus.Draft)
                {
                    user.SetStatus(UserStatus.Pending);
                    _logger.LogInformation(
                        "📝 Updated user {UserId} status to {Status}",
                        @event.OwnerId,
                        UserStatus.Pending);
                }

                await _userRepository.UpdateAsync(user, CancellationToken.None);

            });

            _logger.LogInformation(
                "✅ Successfully processed ProviderRegisteredIntegrationEvent for User {UserId}",
                @event.OwnerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Failed to handle ProviderRegisteredIntegrationEvent for User {UserId}",
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
public sealed record ProviderRegisteredIntegrationEvent(
    Guid ProviderId,
    Guid OwnerId,
    string BusinessName,
    string ProviderType, // Changed from enum to string for cross-context compatibility
    DateTime RegisteredAt);
