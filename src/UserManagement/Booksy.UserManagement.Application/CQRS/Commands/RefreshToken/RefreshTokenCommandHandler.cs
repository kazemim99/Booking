
using Booksy.UserManagement.Domain.Exceptions;
using Booksy.UserManagement.Application.Abstractions.Persistence;

namespace Booksy.UserManagement.Application.CQRS.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserManagementUnitOfWork _unitOfWork;
        private readonly ICustomerRepository _customerRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IProviderInfoService _providerInfoService;
        private readonly IMembershipInfoService _membershipInfoService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(
            IUserRepository userWriteRepository,
            IUserManagementUnitOfWork unitOfWork,
            ICustomerRepository customerRepository,
            IJwtTokenService jwtTokenService,
            IProviderInfoService providerInfoService,
            IMembershipInfoService membershipInfoService,
            ILogger<RefreshTokenCommandHandler> logger)
        {
            _userRepository = userWriteRepository;
            _unitOfWork = unitOfWork;
            _customerRepository = customerRepository;
            _jwtTokenService = jwtTokenService;
            _providerInfoService = providerInfoService;
            _membershipInfoService = membershipInfoService;
            _logger = logger;
        }

        public async Task<RefreshTokenResult> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing refresh token request");

            // Find the user by refresh token in the database. This used to load EVERY user
            // (profile, roles and tokens) and search them in memory on each refresh.
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user is not null && user.GetValidRefreshToken(request.RefreshToken) is null)
            {
                user = null;
            }

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

            // Query customer information if user has Customer role
            string? customerId = null;
            if (user.Roles.Any(r => r.Name == "Customer" || r.Name == "Client"))
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

            // Query this person's organization memberships (refactor-identity-and-
            // membership §5.4) so a refreshed token carries the same claims a fresh
            // sign-in would -- otherwise memberships silently vanish on the first refresh.
            IReadOnlyList<MembershipSummary> memberships = Array.Empty<MembershipSummary>();
            try
            {
                memberships = await _membershipInfoService.GetMembershipsForPersonAsync(
                    user.Id.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error querying memberships for UserId: {UserId}, continuing without membership claims",
                    user.Id);
            }
            var activeMembershipId = MembershipClaimResolver.ResolveActiveMembershipId(memberships, providerId);

            // Generate new access token
            var accessToken = _jwtTokenService.GenerateAccessToken(
                user.Id,
                user.Type,
                user.Email,
                user.Profile.GetDisplayName(),
                user.Profile.FirstName,
                user.Profile.LastName,
                user.Status.ToString(),
                user.Roles.Select(r => r.Name).ToList(),
                providerId,
                providerStatus,
                customerId,
                user.PhoneNumber?.Value,
                memberships,
                activeMembershipId,
                24); // 24 hours

            await _userRepository.UpdateAsync(user, cancellationToken);
            // Commit the UserManagement unit of work explicitly. The pipeline's TransactionBehavior
            // commits the ServiceCatalog context (registered last, DI last-wins), so without this the
            // change was tracked and then silently discarded at the end of the request.
            await _unitOfWork.SaveAndPublishEventsAsync(cancellationToken);

            _logger.LogInformation("Refresh token rotated successfully for user: {UserId}", user.Id);

            return new RefreshTokenResult(
                AccessToken: accessToken,
                RefreshToken: newRefreshToken.Token,
                ExpiresIn: 24 * 3600);
        }
    }
}