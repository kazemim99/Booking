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
        private readonly IUserWriteRepository _userWriteRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuditUserService _auditService;
        private readonly ILogger<AuthenticateUserCommandHandler> _logger;

        public AuthenticateUserCommandHandler(
            IUserWriteRepository userWriteRepository,
            IJwtTokenService jwtTokenService,
            IAuditUserService auditService,
            ILogger<AuthenticateUserCommandHandler> logger)
        {
            _userWriteRepository = userWriteRepository;
            _jwtTokenService = jwtTokenService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<AuthenticateUserResult> Handle(
            AuthenticateUserCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Authenticating user: {Email}", request.Email);

            var email = Email.From(request.Email);

            var user = await _userWriteRepository.GetByEmailAsync(email, cancellationToken);
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

                await _userWriteRepository.UpdateUserAsync(user, cancellationToken);

                // Generate JWT token
                var tokenExpirationHours = request.RememberMe ? 168 : 24; // 7 days or 1 day
                var accessToken = _jwtTokenService.GenerateAccessToken(
                    user.Id,
                    user.Email,
                    user.Profile.GetDisplayName(),
                    authResult.Roles,
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
