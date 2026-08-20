using Booksy.ServiceCatalog.Application.Services.Interfaces;
using Booksy.UserManagement.Application.CQRS.Queries.GetUserById;
using Booksy.UserManagement.Application.Services.Interfaces;
using MediatR;

namespace Booksy.Host.Composition;

/// <summary>
/// In-process implementation of ServiceCatalog's <see cref="ITokenService"/> port. Mints an access
/// token carrying the caller's <c>provider_id</c> / <c>provider_status</c> claims by calling
/// UserManagement's <see cref="IJwtTokenService"/> directly, instead of POSTing to
/// <c>/api/v1/auth/generate-token</c> over loopback HTTP.
///
/// <para><b>Why this lives in the Host.</b> Same reason as
/// <see cref="InProcessProviderInfoService"/>, in the opposite direction: token minting belongs to
/// UserManagement, but only ServiceCatalog knows the provider id and status to stamp into the token.
/// The Host is the one project that references both contexts, so implementing the port here keeps
/// ServiceCatalog free of any compile-time dependency on UserManagement.</para>
///
/// <para><b>What it replaces.</b> Two separate loopback callers, both of which were fragile for the
/// same underlying reason — <c>/api/v1/auth/generate-token</c> carries no <c>[AllowAnonymous]</c> and
/// so falls under the host-wide <c>FallbackPolicy.RequireAuthenticatedUser()</c>:</para>
/// <list type="number">
///   <item><description><c>ServiceCatalog.Infrastructure.Services.Application.TokenService</c> — used by
///   provider-registration completion. It worked only by re-presenting the caller's own bearer token,
///   a documented workaround for the fallback policy. In-process, no token needs forwarding at all.</description></item>
///   <item><description><c>ProvidersController.RefreshProviderToken</c> — built a raw
///   <c>new HttpClient()</c> and POSTed to <c>{Services:UserManagement:BaseUrl}</c>, defaulting to
///   <c>http://localhost:5001</c>: a standalone UserManagement host that no longer exists after the
///   move to a modular monolith, and with no bearer token attached it would have 401'd even if it did.
///   The call could only fail, and the failure became a generic 500 — which is why a freshly-registered
///   provider's dashboard and gallery rendered empty, the client never having obtained a token
///   carrying <c>providerId</c>.</description></item>
/// </list>
///
/// <para><b>Token lifetime.</b> 24 hours, matching every other <c>GenerateAccessToken</c> call site
/// (<c>AuthenticateUser</c>, <c>CompleteCustomerAuthentication</c>, <c>CompleteProviderAuthentication</c>,
/// <c>RefreshToken</c>), and <see cref="TokenResponse.ExpiresIn"/> is reported consistently with what is
/// actually minted. The endpoint being replaced did not do this: it passed <c>15</c> to a parameter named
/// <c>expirationHours</c> while commenting "15 minutes" and reporting <c>ExpiresIn = 900</c>, so it issued
/// a token living 15 hours and told the client it had 15 minutes — a 60x understatement. See
/// <c>openspec/changes/FOLLOW-UPS.md</c> for the same defect still present in
/// <c>AuthController.GenerateToken</c> itself.</para>
/// </summary>
/// <remarks>
/// Public for the same reason as <see cref="InProcessProviderInfoService"/>: so
/// <c>Booksy.Host.CompositionTests</c> can assert the container resolves this exact type.
/// </remarks>
public sealed class InProcessTokenService : ITokenService
{
    /// <summary>
    /// Access-token lifetime, in hours. Matches the convention used by every other call site.
    /// </summary>
    private const int AccessTokenLifetimeHours = 24;

    private readonly ISender _mediator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<InProcessTokenService> _logger;

    public InProcessTokenService(
        ISender mediator,
        IJwtTokenService jwtTokenService,
        ILogger<InProcessTokenService> logger)
    {
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<TokenResponse> GenerateTokenWithProviderClaimsAsync(
        Guid userId,
        Guid providerId,
        string providerStatus,
        CancellationToken cancellationToken = default)
    {
        // GetUserByIdQueryHandler throws InvalidOperationException("User with ID {id} not found")
        // for an unknown user rather than returning null, so this guard is belt-and-braces — it only
        // becomes the live path if that handler is ever changed to return null.
        // TokenServiceCompositionTests pins the current behaviour.
        var user = await _mediator.Send(new GetUserByIdQuery(userId), cancellationToken)
            ?? throw new InvalidOperationException(
                $"Cannot mint a provider token: user {userId} was not found in the identity store.");

        var userType = Enum.TryParse<Booksy.UserManagement.Domain.Enums.UserType>(user.Type, out var parsed)
            ? parsed
            : Booksy.UserManagement.Domain.Enums.UserType.Customer;

        var accessToken = _jwtTokenService.GenerateAccessToken(
            Booksy.Core.Domain.ValueObjects.UserId.From(user.UserId),
            userType,
            Booksy.Core.Domain.ValueObjects.Email.Create(user.Email ?? string.Empty),
            user.DisplayName ?? string.Empty,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            user.Status ?? "Active",
            user.Roles.Select(r => r.Name).ToList(),
            providerId.ToString(),
            providerStatus,
            customerId: null,
            user.PhoneNumber,
            AccessTokenLifetimeHours);

        _logger.LogInformation(
            "Minted provider token in-process for user {UserId} (provider {ProviderId}, status {Status})",
            userId, providerId, providerStatus);

        return new TokenResponse
        {
            AccessToken = accessToken,
            // The rotating refresh token is owned by the auth flows (RefreshTokenCommandHandler);
            // stamping provider claims onto an access token does not rotate it, so none is returned
            // here rather than inventing a value the identity store has never seen. The previous HTTP
            // path returned `Guid.NewGuid().ToString()` — a refresh token that was never persisted and
            // so could never actually be redeemed.
            RefreshToken = null,
            ExpiresIn = (int)TimeSpan.FromHours(AccessTokenLifetimeHours).TotalSeconds,
            TokenType = "Bearer"
        };
    }
}
