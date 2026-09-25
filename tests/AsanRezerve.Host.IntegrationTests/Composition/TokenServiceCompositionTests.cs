using AsanRezerve.Host.Composition;
using AsanRezerve.ServiceCatalog.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.Host.CompositionTests;

/// <summary>
/// Locks down how the Host satisfies ServiceCatalog's <see cref="ITokenService"/> port — the mirror
/// image of <see cref="ProviderInfoCompositionTests"/>, in the opposite direction.
///
/// <para>ServiceCatalog.Infrastructure registers an HTTP adapter for this port
/// (<c>Services.Application.TokenService</c>) that POSTs to
/// <c>/api/v1/auth/generate-token</c> over loopback. That endpoint has no <c>[AllowAnonymous]</c>, so
/// it falls under the host-wide <c>FallbackPolicy.RequireAuthenticatedUser()</c>; the adapter only
/// worked by re-presenting the caller's own bearer token. The Host replaces it with
/// <see cref="InProcessTokenService"/>, which calls UserManagement's JWT service directly.</para>
///
/// <para>As with the provider-info port, the fix depends on <b>registration order</b> in Program.cs:
/// the override must come after AddServiceCatalogInfrastructure or the HTTP adapter silently wins
/// again, and provider-registration completion goes back to depending on a loopback HTTP hop that the
/// host's own auth policy is designed to reject. No other test project builds the composition root,
/// so this is the only place that can catch it.</para>
/// </summary>
[Collection(HostCompositionCollection.Name)]
public sealed class TokenServiceCompositionTests
{
    private readonly HostCompositionFactory _factory;

    public TokenServiceCompositionTests(HostCompositionFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void The_Host_Resolves_The_InProcess_Token_Service_Not_The_Http_One()
    {
        using var scope = _factory.Services.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService<ITokenService>();

        resolved.Should().BeOfType<InProcessTokenService>(
            "ServiceCatalog.Infrastructure also registers an HTTP adapter for this port; if its " +
            "registration ends up last, provider-registration completion and token refresh go back " +
            "to a loopback call that the host's own fallback auth policy rejects");
    }

    /// <summary>
    /// Resolving the adapter proves the registration order, but not that the UserManagement
    /// dependencies it needs (<c>IJwtTokenService</c> and the <c>GetUserByIdQuery</c> handler) are
    /// reachable in the same container. Minting for an unknown user is the cheapest way to prove the
    /// round trip actually executes: a missing dependency or unregistered handler fails inside
    /// MediatR with a resolution error, whereas a fully wired adapter reaches
    /// <c>GetUserByIdQueryHandler</c>, which throws its own "User with ID {id} not found".
    /// Asserting on that specific message therefore distinguishes "wired up, no such user" from
    /// "not wired up at all".
    ///
    /// <para>Note the message is the <b>query handler's</b>, not the adapter's: the handler throws
    /// rather than returning null, so <see cref="InProcessTokenService"/>'s own null guard is
    /// belt-and-braces and never fires on this path. Pinning the handler's wording here means that
    /// if it is ever changed to return null instead, this test fails and points at the adapter's
    /// guard — which would then become the live path.</para>
    /// </summary>
    [Fact]
    public async Task The_InProcess_Token_Service_Can_Reach_Its_UserManagement_Dependencies()
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var unknownUserId = Guid.NewGuid();

        var act = () => tokenService.GenerateTokenWithProviderClaimsAsync(
            unknownUserId, Guid.NewGuid(), "Active", CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage($"*{unknownUserId}*not found*");
    }
}
