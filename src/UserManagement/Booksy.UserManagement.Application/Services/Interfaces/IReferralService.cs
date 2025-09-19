// ========================================
// Event Handlers - Domain Events
// ========================================

// Booksy.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// Booksy.UserManagement.Application/Services/Interfaces/IReferralService.cs
using Booksy.Core.Domain.ValueObjects;

namespace Booksy.UserManagement.Application.Services.Interfaces
{
    public interface IReferralService
    {
        Task<Guid?> GetReferrerIdAsync(string referralCode, CancellationToken cancellationToken = default);
        Task RecordReferralAsync(Guid referrerId, UserId referredUserId, CancellationToken cancellationToken = default);
        Task<string> GenerateReferralCodeAsync(UserId userId, CancellationToken cancellationToken = default);
        Task<int> GetReferralCountAsync(UserId userId, CancellationToken cancellationToken = default);
    }
}

