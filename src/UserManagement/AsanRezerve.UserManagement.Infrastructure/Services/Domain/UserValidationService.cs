// ========================================
// Security Services
// ========================================

// AsanRezerve.UserManagement.Infrastructure/Services/Security/PasswordHasher.cs
using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Domain.Repositories;
using AsanRezerve.UserManagement.Domain.Services;

namespace AsanRezerve.UserManagement.Infrastructure.Services.Domain
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

