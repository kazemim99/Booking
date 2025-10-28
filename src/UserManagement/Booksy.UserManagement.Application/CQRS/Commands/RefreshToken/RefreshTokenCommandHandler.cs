
using Booksy.UserManagement.Domain.Exceptions;

namespace Booksy.UserManagement.Application.CQRS.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IProviderInfoService _providerInfoService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IUserRepository userWriteRepository,
            IJwtTokenService jwtTokenService,
            IProviderInfoService providerInfoService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _userRepository = userWriteRepository;

            _jwtTokenService = jwtTokenService;
            _providerInfoService = providerInfoService;
            _logger = logger;
        }

        public async Task<RefreshTokenResult> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing refresh token request");

            // Find user by refresh token
            var users = await _userRepository.GetAllAsync(cancellationToken);
            var user = users.FirstOrDefault(u => u.GetValidRefreshToken(request.RefreshToken) != null);

            if (user == null)
            {
                throw new InvalidCredentialsException("Invalid refresh token");
            }

            var oldToken = user.GetValidRefreshToken(request.RefreshToken);
            if (oldToken == null || !oldToken.IsActive)
            {
                throw new InvalidCredentialsException("Invalid or expired refresh token");
            }

            // Rotate refresh token
            var newRefreshToken = oldToken.Rotate(request.IpAddress);
            user.AddRefreshToken(newRefreshToken);

            // Query provider information if user has Provider role
            string? providerId = null;
            string? providerStatus = null;
            if (user.Roles.Any(r => r.Name == "Provider" || r.Name == "ServiceProvider"))
            {
                _logger.LogInformation("User has Provider role, querying provider info for UserId: {UserId}", user.Id);
                var providerInfo = await _providerInfoService.GetProviderByOwnerIdAsync(
                    user.Id.Value,
                    cancellationToken);

                if (providerInfo != null)
                {
                    providerId = providerInfo.ProviderId.ToString();
                    providerStatus = providerInfo.Status;
                    _logger.LogInformation("Provider found: ProviderId={ProviderId}, Status={Status}",
                        providerId, providerInfo.Status);
                }
                else
                {
                    _logger.LogInformation("No provider found for UserId: {UserId}", user.Id);
                }
            }

            // Generate new access token
            var accessToken = _jwtTokenService.GenerateAccessToken(
                user.Id,
                user.Type,
                user.Email,
                user.Profile.GetDisplayName(),
                user.Status.ToString(),
                user.Roles.Select(r => r.Name).ToList(),
                providerId,
                providerStatus,
                24); // 24 hours

            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Refresh token rotated successfully for user: {UserId}", user.Id);

            return new RefreshTokenResult(
                AccessToken: accessToken,
                RefreshToken: newRefreshToken.Token,
                ExpiresIn: 24 * 3600);
        }
    }
}