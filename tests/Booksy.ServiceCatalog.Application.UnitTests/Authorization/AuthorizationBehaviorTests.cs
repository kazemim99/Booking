using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Application.Authorization;
using Booksy.Core.Application.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booksy.ServiceCatalog.Application.UnitTests.Authorization;

/// <summary>
/// Unit tests for the cross-cutting ownership enforcement (C1). These prove the
/// IDOR fix at the behavior level: a non-owner is denied before the handler runs,
/// while owners, admins, and trusted system contexts proceed.
/// </summary>
public class AuthorizationBehaviorTests
{
    private sealed record TestBookingCommand(Guid BookingId, Guid ActingUserId) : IRequireBookingOwnership;

    private sealed class StubResolver : IResourceOwnershipResolver<TestBookingCommand>
    {
        private readonly ResourceOwners? _owners;
        public StubResolver(ResourceOwners? owners) => _owners = owners;
        public Task<ResourceOwners?> ResolveAsync(TestBookingCommand command, CancellationToken ct)
            => Task.FromResult(_owners);
    }

    private static ICurrentUserService User(Guid? id, bool admin = false)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.IsAuthenticated.Returns(id.HasValue);
        u.UserId.Returns(id?.ToString());
        u.IsInRole("Admin").Returns(admin);
        return u;
    }

    private static (AuthorizationBehavior<TestBookingCommand, string> behavior,
                    RequestHandlerDelegate<string> next,
                    Func<bool> nextCalled)
        Build(ICurrentUserService currentUser, ResourceOwners? owners, bool enforce = true, bool registerResolver = true)
    {
        var sp = Substitute.For<IServiceProvider>();
        if (registerResolver)
            sp.GetService(typeof(IResourceOwnershipResolver<TestBookingCommand>)).Returns(new StubResolver(owners));
        sp.GetService(typeof(AuthorizationOptions)).Returns(new AuthorizationOptions { EnforceOwnership = enforce });

        var behavior = new AuthorizationBehavior<TestBookingCommand, string>(
            currentUser, sp, NullLogger<AuthorizationBehavior<TestBookingCommand, string>>.Instance);

        var called = false;
        RequestHandlerDelegate<string> next = (CancellationToken _) => { called = true; return Task.FromResult("ok"); };
        return (behavior, next, () => called);
    }

    private static TestBookingCommand AnyCommand() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Owning_customer_is_allowed()
    {
        var caller = Guid.NewGuid();
        var (behavior, next, called) = Build(User(caller), new ResourceOwners(caller, Guid.NewGuid()));

        var result = await behavior.Handle(new TestBookingCommand(Guid.NewGuid(), caller), next, CancellationToken.None);

        result.Should().Be("ok");
        called().Should().BeTrue();
    }

    [Fact]
    public async Task Owning_provider_is_allowed()
    {
        var caller = Guid.NewGuid();
        var (behavior, next, called) = Build(User(caller), new ResourceOwners(Guid.NewGuid(), caller));

        await behavior.Handle(AnyCommand(), next, CancellationToken.None);

        called().Should().BeTrue();
    }

    [Fact]
    public async Task Non_owner_is_denied_with_Forbidden_and_handler_never_runs()
    {
        var (behavior, next, called) = Build(User(Guid.NewGuid()), new ResourceOwners(Guid.NewGuid(), Guid.NewGuid()));

        var act = () => behavior.Handle(AnyCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        called().Should().BeFalse("the handler (and any refund side effect) must not run for a non-owner");
    }

    [Fact]
    public async Task Admin_is_allowed_even_when_not_an_owner()
    {
        var (behavior, next, called) = Build(User(Guid.NewGuid(), admin: true), new ResourceOwners(Guid.NewGuid(), Guid.NewGuid()));

        await behavior.Handle(AnyCommand(), next, CancellationToken.None);

        called().Should().BeTrue();
    }

    [Fact]
    public async Task Missing_resource_fails_closed()
    {
        var (behavior, next, called) = Build(User(Guid.NewGuid()), owners: null); // resolver returns null (not found)

        var act = () => behavior.Handle(AnyCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        called().Should().BeFalse();
    }

    [Fact]
    public async Task System_context_without_authenticated_user_passes_through()
    {
        var (behavior, next, called) = Build(User(null), owners: null, registerResolver: false);

        await behavior.Handle(new TestBookingCommand(Guid.NewGuid(), Guid.Empty), next, CancellationToken.None);

        called().Should().BeTrue("a background/system command has no HTTP user and must not be blocked");
    }

    [Fact]
    public async Task Enforcement_disabled_does_not_block_a_non_owner()
    {
        var (behavior, next, called) = Build(User(Guid.NewGuid()), new ResourceOwners(Guid.NewGuid(), Guid.NewGuid()), enforce: false);

        await behavior.Handle(AnyCommand(), next, CancellationToken.None);

        called().Should().BeTrue("the canary flag lets denials pass while logging");
    }
}
