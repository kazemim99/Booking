// ========================================
// Booksy.UserManagement.Application/Queries/GetUserById/GetUserByIdQuery.cs
// ========================================
using Booksy.UserManagement.Domain.ValueObjects;

namespace Booksy.UserManagement.Application.CQRS.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDetailsViewModel>
    {
        private readonly IUserReadRepository _userReadRepository;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(
            IUserReadRepository userReadRepository,
            ILogger<GetUserByIdQueryHandler> logger)
        {
            _userReadRepository = userReadRepository;
            _logger = logger;
        }

        public async Task<UserDetailsViewModel> Handle(
            GetUserByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Fetching user details for UserId: {UserId}", request.UserId);

            var userId = UserId.From(request.UserId);
            var user = await _userReadRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {request.UserId} not found");
            }

            return new UserDetailsViewModel
            {
                UserId = user.Id.Value,
                Email = user.Email.Value,
                FirstName = user.Profile.FirstName,
                LastName = user.Profile.LastName,
                MiddleName = user.Profile.MiddleName,
                FullName = user.Profile.GetFullName(),
                DisplayName = user.Profile.GetDisplayName(),
                DateOfBirth = user.Profile.DateOfBirth,
                Age = user.Profile.GetAge(),
                Gender = user.Profile.Gender,
                PhoneNumber = user.Profile.PhoneNumber?.Value,
                AlternatePhoneNumber = user.Profile.AlternatePhoneNumber?.Value,
                Address = user.Profile.Address != null ? new AddressViewModel
                {
                    Street = user.Profile.Address.Street,
                    Unit = user.Profile.Address.Unit,
                    City = user.Profile.Address.City,
                    State = user.Profile.Address.State,
                    PostalCode = user.Profile.Address.PostalCode,
                    Country = user.Profile.Address.Country,
                    FullAddress = user.Profile.Address.GetFullAddress()
                } : null,
                AvatarUrl = user.Profile.AvatarUrl,
                Bio = user.Profile.Bio,
                PreferredLanguage = user.Profile.PreferredLanguage,
                TimeZone = user.Profile.TimeZone,
                Status = user.Status.ToString(),
                Type = user.Type.ToString(),
                Roles = user.Roles.Select(r => new RoleViewModel
                {
                    Name = r.Name,
                    AssignedAt = r.AssignedAt,
                    ExpiresAt = r.ExpiresAt,
                    IsExpired = r.IsExpired()
                }).ToList(),
                TwoFactorEnabled = user.TwoFactorEnabled,
                IsLocked = user.IsLocked(),
                LockedUntil = user.LockedUntil,
                RegisteredAt = user.RegisteredAt,
                ActivatedAt = user.ActivatedAt,
                LastLoginAt = user.LastLoginAt,
                LastPasswordChangeAt = user.LastPasswordChangeAt,
                ActiveSessions = user.ActiveSessions.Count(s => s.IsActive),
                Preferences = user.Profile.Preferences
            };
        }
    }
}
