// ========================================
// Event Handlers - Domain Events
// ========================================

// AsanRezerve.UserManagement.Application/EventHandlers/DomainEventHandlers/UserRegisteredEventHandler.cs
// AsanRezerve.UserManagement.Application/Services/Interfaces/IReferralService.cs
using AsanRezerve.Core.Domain.ValueObjects;

namespace AsanRezerve.UserManagement.Application.Services.Interfaces
{
    public interface IReferralService
    {
        Task<Guid?> GetReferrerIdAsync(string referralCode, CancellationToken cancellationToken = default);
        Task RecordReferralAsync(Guid referrerId, UserId referredUserId, CancellationToken cancellationToken = default);
        Task<string> GenerateReferralCodeAsync(UserId userId, CancellationToken cancellationToken = default);
        Task<int> GetReferralCountAsync(UserId userId, CancellationToken cancellationToken = default);
    }
}

