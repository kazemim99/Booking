// ========================================
// Booksy.UserManagement.Application/Commands/ActivateUser/ActivateUserCommand.cs
// ========================================
using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Booksy.UserManagement.Application.CQRS.Commands.AuthenticateUser
{
    public sealed class AuthenticateUserCommandHandler : ICommandHandler<AuthenticateUserCommand, AuthenticateUserResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuditUserService _auditService;
        private readonly IProviderInfoService _providerInfoService;
        private readonly ILogger<AuthenticateUserCommandHandler> _logger;

        public AuthenticateUserCommandHandler(
            IUserRepository userWriteRepository,
            IJwtTokenService jwtTokenService,
            IAuditUserService auditService,
            IProviderInfoService providerInfoService,
            ILogger<AuthenticateUserCommandHandler> logger)
        {
            _userRepository = userWriteRepository;
            _jwtTokenService = jwtTokenService;
            _auditService = auditService;
            _providerInfoService = providerInfoService;
            _logger = logger;
        }

        public async Task<AuthenticateUserResult> Handle(
            AuthenticateUserCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Authenticating user: {Email}", request.Email);

            var email = Email.Create(request.Email);

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                await _auditService.LogFailedLoginAsync(
                    request.Email,
                    "User not found",
                    request.IpAddress,
                    request.UserAgent,
                    cancellationToken);

                throw new InvalidCredentialsException();
            }

            try
            {
                var authResult = user.Authenticate(
                    request.Password,
                    request.IpAddress,
                    request.UserAgent);

                await _userRepository.UpdateAsync(user, cancellationToken);

                // Query provider information if user has Provider role
                string? providerId = null;
                string? providerStatus = null;
                if (authResult.Roles.Contains("Provider") || authResult.Roles.Contains("ServiceProvider"))
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

                // Generate JWT token
                var tokenExpirationHours = request.RememberMe ? 168 : 24; // 7 days or 1 day
                var accessToken = _jwtTokenService.GenerateAccessToken(
                    user.Id,
                    user.Type,
                    user.Email,
                    user.Profile.GetDisplayName(),
                    user.Status.ToString(),
                    authResult.Roles,
                    providerId,
                    providerStatus,
                    tokenExpirationHours);

                await _auditService.LogSuccessfulLoginAsync(
                    user.Id,
                    request.IpAddress,
                    request.UserAgent,
                    cancellationToken);

                _logger.LogInformation("User authenticated successfully. UserId: {UserId}", user.Id);

                return new AuthenticateUserResult(
                    UserId: user.Id.Value,
                    Email: user.Email.Value,
                    DisplayName: authResult.DisplayName!,
                    Roles: authResult.Roles.ToList(),
                    AccessToken: accessToken,
                    RefreshToken: authResult.RefreshToken!,
                    ExpiresIn: tokenExpirationHours * 3600);
            }
            catch (InvalidCredentialsException)
            {
                await _auditService.LogFailedLoginAsync(
                    request.Email,
                    "Invalid credentials",
                    request.IpAddress,
                    request.UserAgent,
                    cancellationToken);

                throw;
            }
        }
    }
}
