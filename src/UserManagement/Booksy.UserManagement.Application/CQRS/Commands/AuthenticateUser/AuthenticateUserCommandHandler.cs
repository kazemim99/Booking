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
        private readonly ICustomerRepository _customerRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuditUserService _auditService;
        private readonly IProviderInfoService _providerInfoService;
        private readonly ILogger<AuthenticateUserCommandHandler> _logger;

        public AuthenticateUserCommandHandler(
            IUserRepository userWriteRepository,
            ICustomerRepository customerRepository,
            IJwtTokenService jwtTokenService,
            IAuditUserService auditService,
            IProviderInfoService providerInfoService,
            ILogger<AuthenticateUserCommandHandler> logger)
        {
            _userRepository = userWriteRepository;
            _customerRepository = customerRepository;
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

                // Query customer information if user has Customer role
                string? customerId = null;
                if (authResult.Roles.Contains("Customer") || authResult.Roles.Contains("Client"))
                {
                    _logger.LogInformation("User has Customer role, querying customer info for UserId: {UserId}", user.Id);
                    var customer = await _customerRepository.GetByUserIdAsync(user.Id, cancellationToken);

                    if (customer != null)
                    {
                        customerId = customer.Id.Value.ToString();
                        _logger.LogInformation("Customer found: CustomerId={CustomerId}", customerId);
                    }
                    else
                    {
                        _logger.LogInformation("No customer found for UserId: {UserId}", user.Id);
                    }
                }

                // Generate JWT token
                var tokenExpirationHours = request.RememberMe ? 168 : 24; // 7 days or 1 day
                var accessToken = _jwtTokenService.GenerateAccessToken(
                    user.Id,
                    user.Type,
                    user.Email,
                    user.Profile.GetDisplayName(),
                    user.Profile.FirstName,
                    user.Profile.LastName,
                    user.Status.ToString(),
                    authResult.Roles,
                    providerId,
                    providerStatus,
                    customerId,
                    user.PhoneNumber?.Value,
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
