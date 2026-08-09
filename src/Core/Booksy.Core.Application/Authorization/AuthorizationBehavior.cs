using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booksy.Core.Application.Authorization
{
    /// <summary>Tunable for ownership enforcement. Registered from configuration
    /// ("Authorization:EnforceOwnership", default true). When false the behavior
    /// logs would-be denials but does not block (safe production canary).</summary>
    public sealed class AuthorizationOptions
    {
        public bool EnforceOwnership { get; set; } = true;
    }

    /// <summary>
    /// Cross-cutting resource-ownership enforcement. For any command implementing
    /// <see cref="IRequireResourceOwnership"/>, resolves the resource's owners via the
    /// registered <see cref="IResourceOwnershipResolver{TCommand}"/> and allows the
    /// request only if the authenticated caller (from <see cref="ICurrentUserService"/>,
    /// never the request body) is an owner or an Admin. Denies with
    /// <see cref="ForbiddenException"/> (HTTP 403) otherwise. Fails closed: a marked
    /// command with no registered resolver, or a not-found resource, is denied.
    ///
    /// Registered after ValidationBehavior and before TransactionBehavior so denials
    /// never open a database transaction or trigger side effects (e.g. refunds).
    /// </summary>
    public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private const string AdminRole = "Admin";

        private readonly ICurrentUserService _currentUser;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

        public AuthorizationBehavior(
            ICurrentUserService currentUser,
            IServiceProvider serviceProvider,
            ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
        {
            _currentUser = currentUser;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not IRequireResourceOwnership)
                return await next();

            // No authenticated principal → trusted system/background context (e.g. the
            // auto-refund raised by the booking-cancelled integration event). HTTP anonymous
            // access is blocked at the controller ([Authorize] + the global fallback policy),
            // so ownership is enforced among *authenticated* callers only.
            if (!_currentUser.IsAuthenticated ||
                !Guid.TryParse(_currentUser.UserId, out var callerId))
            {
                _logger.LogDebug(
                    "AuthorizationBehavior: no authenticated user for {Command}; treating as trusted system context",
                    typeof(TRequest).Name);
                return await next();
            }

            var options =
                _serviceProvider.GetService(typeof(AuthorizationOptions)) as AuthorizationOptions
                ?? new AuthorizationOptions();

            var resolver =
                _serviceProvider.GetService(typeof(IResourceOwnershipResolver<TRequest>))
                    as IResourceOwnershipResolver<TRequest>;

            if (resolver is null)
            {
                // A command marked for ownership with no resolver is a wiring bug — fail closed.
                _logger.LogError(
                    "No IResourceOwnershipResolver registered for {Command}; failing closed",
                    typeof(TRequest).Name);
                return await DenyOrPassAsync(options, next, "authorization is misconfigured");
            }

            var owners = await resolver.ResolveAsync(request, cancellationToken);

            var isAdmin = _currentUser.IsInRole(AdminRole);
            var isOwner = owners is not null &&
                          (owners.CustomerId == callerId || owners.ProviderOwnerUserId == callerId);

            if (isAdmin || isOwner)
                return await next();

            _logger.LogWarning(
                "Ownership denied: user {UserId} is not an owner of the resource targeted by {Command}",
                callerId, typeof(TRequest).Name);
            return await DenyOrPassAsync(options, next, "you are not authorized to act on this resource");
        }

        private async Task<TResponse> DenyOrPassAsync(
            AuthorizationOptions options,
            RequestHandlerDelegate<TResponse> next,
            string reason)
        {
            if (options.EnforceOwnership)
                throw new ForbiddenException($"Access denied: {reason}.");

            _logger.LogWarning(
                "Authorization enforcement is OFF — would have denied {Command} ({Reason})",
                typeof(TRequest).Name, reason);
            return await next();
        }
    }
}
