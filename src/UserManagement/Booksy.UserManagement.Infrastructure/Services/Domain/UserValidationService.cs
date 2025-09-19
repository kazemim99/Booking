// ========================================
// Security Services
// ========================================

// Booksy.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.Services;

namespace Booksy.UserManagement.Infrastructure.Services.Domain
{
    public class UserValidationService : IUserValidationService
    {
        private readonly IUserRepository _userRepository;

        public UserValidationService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> IsEmailAvailableAsync(Email email, CancellationToken cancellationToken = default)
        {
            return !await _userRepository.ExistsByEmailAsync(email, cancellationToken);
        }

        public bool CanActivate(User user)
        {
            return user.Status == UserStatus.Pending &&
                   user.ActivationToken != null &&
                   !user.ActivationToken.IsExpired();
        }

        public bool CanSuspend(User user)
        {
            return user.Status == UserStatus.Active;
        }

        public bool CanDelete(User user)
        {
            return user.Status != UserStatus.Deleted;
        }

        public bool IsProfileComplete(User user)
        {
            return user.Profile != null &&
                   !string.IsNullOrEmpty(user.Profile.FirstName) &&
                   !string.IsNullOrEmpty(user.Profile.LastName) &&
                   user.Profile.PhoneNumber != null &&
                   user.Profile.Address != null;
        }

        public bool RequiresEmailVerification(User user)
        {
            return user.Status == UserStatus.Pending;
        }

        public bool RequiresPasswordChange(User user, int maxPasswordAgeDays = 90)
        {
            if (!user.LastPasswordChangeAt.HasValue)
                return true;

            var daysSinceChange = (DateTime.UtcNow - user.LastPasswordChangeAt.Value).Days;
            return daysSinceChange > maxPasswordAgeDays;
        }
    }
}

