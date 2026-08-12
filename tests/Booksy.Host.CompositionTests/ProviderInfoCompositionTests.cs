using Booksy.Host.Composition;
using Booksy.UserManagement.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.Host.CompositionTests;

/// <summary>
/// Locks down how the Host satisfies UserManagement's <see cref="IProviderInfoService"/> port.
///
/// <para>UserManagement.Infrastructure registers an HTTP adapter for this port that calls
/// ServiceCatalog's <c>GET /api/v1/Providers/by-owner/{id}</c>. That endpoint has no
/// <c>[AllowAnonymous]</c> and so falls under the host-wide
/// <c>FallbackPolicy.RequireAuthenticatedUser()</c>, while its only caller — OTP sign-in
/// completion — runs before the caller has a token. The call therefore always 401'd, and
/// because the adapter catches <see cref="HttpRequestException"/> and logs a warning, the
/// failure was invisible: every provider sign-in reported "no provider profile", issuing a
/// token with no provider claims and sending registered providers back into onboarding.</para>
///
/// <para>The Host replaces that adapter with <see cref="InProcessProviderInfoService"/>, which
/// dispatches the ServiceCatalog query directly. Two things make that fix fragile, and both are
/// pinned here: it depends on <b>registration order</b> in Program.cs (the override must come
/// after AddUserManagementInfrastructure, or the HTTP adapter silently wins again), and it
/// depends on the ServiceCatalog query handler being <b>resolvable in the same container</b>.
/// Neither is visible to the other test projects, which boot per-context Startup classes and
/// never build the composition root.</para>
/// </summary>
public sealed class ProviderInfoCompositionTests : IClassFixture<HostCompositionFactory>
{
    private readonly HostCompositionFactory _factory;

    public ProviderInfoCompositionTests(HostCompositionFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void The_Host_Resolves_The_InProcess_Adapter_Not_The_Http_One()
    {
        using var scope = _factory.Services.CreateScope();

        var resolved = scope.ServiceProvider.GetRequiredService<IProviderInfoService>();

        resolved.Should().BeOfType<InProcessProviderInfoService>(
            "UserManagement.Infrastructure also registers an HTTP adapter for this port; if its " +
            "registration ends up last, provider claims silently vanish from every sign-in");
    }

    /// <summary>
    /// The adapter is only useful if the ServiceCatalog query it sends is actually handled in
    /// the same container. An unknown owner is the cheapest way to prove the round trip: a
    /// missing handler throws (MediatR cannot resolve it), whereas a working one runs the query
    /// and legitimately returns null. Asserting "returns null without throwing" therefore
    /// distinguishes "wired up, no such provider" from "not wired up at all".
    /// </summary>
    [Fact]
    public async Task The_InProcess_Adapter_Reaches_ServiceCatalogs_Query_Handler()
    {
        using var scope = _factory.Services.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IProviderInfoService>();

        var result = await sut.GetProviderByOwnerIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull("no provider owns a random GUID — but the query had to execute to find that out");
    }

    /// <summary>
    /// The port is consumed by a scoped command handler, so resolving it per scope must work and
    /// must not hand back a singleton that captures a scoped DbContext.
    /// </summary>
    [Fact]
    public void The_Adapter_Is_Scoped()
    {
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();

        var a = first.ServiceProvider.GetRequiredService<IProviderInfoService>();
        var b = second.ServiceProvider.GetRequiredService<IProviderInfoService>();

        a.Should().NotBeSameAs(b, "a captured instance would outlive the DbContext its query depends on");
    }
}

/// <summary>
/// Guards the onboarding rule that the in-process lookup made reachable. Kept next to the
/// composition tests because the two only combine into a bug together: while the lookup always
/// failed, <c>RequiresOnboarding</c> was never evaluated against a real status.
/// </summary>
public sealed class ProviderInfoOnboardingRuleTests
{
    [Theory]
    [InlineData("Drafted", true)]
    [InlineData("drafted", true)]  // status arrives as a string; casing must not decide routing
    [InlineData("PendingVerification", false)]
    [InlineData("Verified", false)]
    [InlineData("Active", false)]
    [InlineData("Inactive", false)]
    [InlineData("Suspended", false)]
    [InlineData("Archived", false)]
    public void Only_A_Drafted_Provider_Still_Owes_Onboarding(string status, bool expected)
    {
        new ProviderInfo(Guid.NewGuid(), status).RequiresOnboarding.Should().Be(expected);
    }
}
