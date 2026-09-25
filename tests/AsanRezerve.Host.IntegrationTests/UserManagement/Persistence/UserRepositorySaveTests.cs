using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.UserManagement.Domain.Aggregates;
using AsanRezerve.UserManagement.Domain.Entities;
using AsanRezerve.UserManagement.Domain.Enums;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Context;
using AsanRezerve.UserManagement.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AsanRezerve.UserManagement.IntegrationTests.Persistence;

/// <summary>
/// Persistence tests for <see cref="UserRepository.SaveAsync"/> against a real
/// PostgreSQL instance, so foreign keys are actually enforced.
///
/// Regression guard for the OTP sign-in 500: the phone-first sign-in path saves the
/// SAME aggregate twice within one request — PersonProvisioningService creates and
/// saves the person, then the Complete*AuthenticationCommandHandler adds a refresh
/// token and saves it again. The second SaveAsync used to fall through to
/// DbSet.Update, which flips a brand-new (Added) aggregate to Modified because
/// User.Id is domain-assigned rather than store-generated. The INSERT into "users"
/// was then never emitted, while the new refresh token — whose key IS
/// store-generated, so Update leaves it Added — still was, and failed on
/// FK_refresh_tokens_users_UserId against a row that was never written.
/// Every first-time sign-in (provider AND customer) 500'd as a result.
///
/// <para>Takes its <see cref="UserManagementDbContext"/> from the shared
/// <see cref="AsanRezerveHostFactory"/>'s DI scope
/// (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 3) instead of hand-building one with
/// substituted <c>ICurrentUserService</c>/<c>IDateTimeProvider</c>/<c>IDomainEventDispatcher</c>
/// against its own throwaway container. That was not just extra code: EF Core 9's
/// pending-model-changes check threw against the hand-built context even though
/// <c>dotnet ef migrations has-pending-model-changes</c> against the real DI-composed app reports
/// none — a divergence between "built like production" and "built like this test" that resolving
/// from the real container removes by construction, along with the substituted clock's
/// <c>DateTime.MinValue</c> default that Npgsql rejects for <c>timestamptz</c>.</para>
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public sealed class UserRepositorySaveTests : IAsyncLifetime
{
    private readonly AsanRezerveHostFactory _factory;
    private IServiceScope _scope = null!;
    private UserManagementDbContext _context = null!;
    private UserRepository _repository = null!;

    public UserRepositorySaveTests(AsanRezerveHostFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetStateAsync();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        _repository = new UserRepository(_context);
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveAsync_CalledTwiceOnANewUser_PersistsTheUserAndItsRefreshToken()
    {
        // Arrange — mirrors PersonProvisioningService: create the person and save it.
        var user = NewPhoneUser();
        await _repository.SaveAsync(user);

        // Act — mirrors the auth handler: add a refresh token to the same aggregate
        // and save it again, then commit both in one SaveChanges.
        user.AddRefreshToken(RefreshToken.Generate(expirationDays: 30, createdByIp: "127.0.0.1"));
        await _repository.SaveAsync(user);

        await _context.SaveChangesAsync();

        // Assert — the user row must exist, with its refresh token attached. Before the
        // fix this threw DbUpdateException (23503, FK_refresh_tokens_users_UserId).
        await using var verification = NewContext();
        var persisted = await verification.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == user.Id);

        persisted.Should().NotBeNull("the new person must be INSERTed, not UPDATEd");
        persisted!.RefreshTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveAsync_OnAnAlreadyAddedUser_LeavesItPendingAsAnInsert()
    {
        // Arrange
        var user = NewPhoneUser();
        await _repository.SaveAsync(user);
        _context.Entry(user).State.Should().Be(EntityState.Added);

        // Act — saving the same not-yet-committed aggregate again must not downgrade it.
        await _repository.SaveAsync(user);

        // Assert
        _context.Entry(user).State.Should().Be(
            EntityState.Added,
            "flipping it to Modified drops the INSERT while its new children keep theirs");
    }

    [Fact]
    public async Task SaveAsync_OnAnExistingTrackedUser_StillPersistsTheChange()
    {
        // Arrange — a committed user, reloaded in a fresh context/repository.
        var user = NewPhoneUser();
        await _repository.SaveAsync(user);
        await _context.SaveChangesAsync();

        await using var editContext = NewContext();
        var editRepository = new UserRepository(editContext);
        var tracked = await editContext.Users
            .Include(u => u.Roles)
            .SingleAsync(u => u.Id == user.Id);

        // Act — the capacity-grant path: an existing person gains a second capacity.
        tracked.EnsureCanActAs(UserType.Customer).Should().BeTrue();
        await editRepository.SaveAsync(tracked);
        await editContext.SaveChangesAsync();

        // Assert — the update branch must keep working after the Added short-circuit.
        await using var verification = NewContext();
        var persisted = await verification.Users.SingleAsync(u => u.Id == user.Id);
        persisted.Type.Should().Be(UserType.Both);
    }

    /// <summary>
    /// A fresh <see cref="UserManagementDbContext"/> in its own DI scope — the identity-map-free
    /// read the "reload and verify" assertions above need. Disposing the returned context releases
    /// its connection back to the pool; the scope that produced it is intentionally left for the GC,
    /// exactly as it would be if a caller only held the context — nothing else is resolved from it.
    /// </summary>
    private UserManagementDbContext NewContext() =>
        _factory.Services.CreateScope().ServiceProvider.GetRequiredService<UserManagementDbContext>();

    /// <summary>
    /// A phone-first person shaped exactly like PersonProvisioningService.CreatePerson,
    /// with a per-test-unique phone/email so the shared container needs no cleanup.
    /// </summary>
    private static User NewPhoneUser()
    {
        var national = $"912{Random.Shared.Next(1_000_000, 9_999_999)}";
        var phoneNumber = PhoneNumber.From(national);

        var profile = UserProfile.Create(
            "ارائه‌دهنده",
            phoneNumber.NationalNumber,
            middleName: null,
            dateOfBirth: null,
            gender: null);

        profile.UpdateContactInfo(phoneNumber, null, null);

        return User.RegisterWithPhone(
            Email.Create($"{phoneNumber.NationalNumber}@asanrezerve.provider"),
            phoneNumber,
            profile,
            UserType.Provider);
    }
}
