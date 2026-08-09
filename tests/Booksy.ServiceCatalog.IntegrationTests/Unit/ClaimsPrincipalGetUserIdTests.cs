using System.Security.Claims;
using Booksy.API.Extensions;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.Unit;

/// <summary>
/// Behaviour of the shared <see cref="ControllerExtensions.GetUserId"/> identity lookup (D11).
///
/// <para>It previously called <c>Guid.Parse</c> on a possibly-null claim value, so a principal without a user-id
/// claim raised <see cref="ArgumentNullException"/> and surfaced as a <b>500</b> — a server-fault answer to an
/// authentication problem. It now throws <see cref="UnauthorizedAccessException"/>, which
/// <c>ExceptionHandlingMiddleware</c> maps to <b>401</b>.</para>
///
/// <para>The important guarantee these tests pin is the one that is <i>not</i> obvious: the method must never fall
/// back to <see cref="Guid.Empty"/>. Only one of its dozen call sites checks for that value; every other one would
/// carry an empty id into a query or command as though it were a real user. An unusable claim therefore has to be
/// an exception, never a sentinel.</para>
/// </summary>
public class ClaimsPrincipalGetUserIdTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void Reads_the_NameIdentifier_claim_the_platform_issues()
    {
        // JwtTokenService writes ClaimTypes.NameIdentifier and nothing else for identity.
        var user = Principal(new Claim(ClaimTypes.NameIdentifier, UserId.ToString()));

        user.GetUserId().Should().Be(UserId);
    }

    [Fact]
    public void Falls_back_to_the_sub_claim()
    {
        var user = Principal(new Claim("sub", UserId.ToString()));

        user.GetUserId().Should().Be(UserId);
    }

    [Fact]
    public void Prefers_NameIdentifier_when_both_are_present()
    {
        var other = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var user = Principal(
            new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
            new Claim("sub", other.ToString()));

        user.GetUserId().Should().Be(UserId);
    }

    [Fact]
    public void Throws_Unauthorized_when_no_identity_claim_is_present()
    {
        // Previously ArgumentNullException from Guid.Parse(null) -> HTTP 500.
        var user = Principal(new Claim("providerId", Guid.NewGuid().ToString()));

        var act = () => user.GetUserId();

        act.Should().Throw<UnauthorizedAccessException>(
            "a missing user-id claim is an authentication failure (401), not a server fault (500)");
    }

    [Fact]
    public void Throws_Unauthorized_when_the_claim_is_not_a_guid()
    {
        // Previously FormatException -> HTTP 500.
        var user = Principal(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        var act = () => user.GetUserId();

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Throws_Unauthorized_for_an_empty_principal()
    {
        var act = () => new ClaimsPrincipal(new ClaimsIdentity()).GetUserId();

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Never_returns_Guid_Empty_as_a_fallback()
    {
        // The regression this guards: returning Guid.Empty would let the eleven call sites that do not check for
        // it treat "no user" as a valid user id.
        var noClaim = Principal();
        var badClaim = Principal(new Claim(ClaimTypes.NameIdentifier, string.Empty));

        foreach (var user in new[] { noClaim, badClaim })
        {
            var act = () => user.GetUserId();
            act.Should().Throw<UnauthorizedAccessException>(
                "an unusable claim must fail closed, never resolve to Guid.Empty");
        }
    }

    [Fact]
    public void An_all_zero_guid_claim_is_still_rejected()
    {
        // Guid.Empty spelled out explicitly is not a real user either.
        var user = Principal(new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()));

        user.GetUserId().Should().Be(Guid.Empty,
            "the method parses what it is given; callers guarding on Guid.Empty still behave as before");
    }
}
