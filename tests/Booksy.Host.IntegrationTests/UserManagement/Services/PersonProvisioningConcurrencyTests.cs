using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Booksy.UserManagement.IntegrationTests.Services;

/// <summary>
/// refactor-identity-and-membership §1.9: "no-cross-type-duplicate" -- a customer sign-in and a
/// provider sign-in racing for the same brand-new phone number must resolve to ONE person with
/// UserType.Both, never two independent accounts. Uses two fully independent DI scopes (mirroring
/// two real concurrent HTTP requests, each with its own scoped <c>DbContext</c>/repository/service),
/// against the real composed host's database.
///
/// <para>This is deliberately NOT assumed to pass. §1.2 (a partial unique index on
/// users(phone_number)) is explicitly not yet done, and GetOrCreateByPhoneAsync's own "defence in
/// depth" comment only guards against a lookup that missed a legacy row shape -- it is not a
/// database-level constraint, so two requests that both observe "no existing row" before either
/// commits are not, on inspection, actually prevented from both inserting. The test reports
/// what genuinely happens.</para>
///
/// <para>Takes its scopes from the shared <see cref="BooksyHostFactory"/>
/// (docs/TEST_ARCHITECTURE_AUDIT.md Phase 2 slice 3) instead of a hand-built
/// <see cref="UserManagementDbContext"/> against its own throwaway container. The real host already
/// migrates this schema at startup, so the previous <c>EnsureCreatedAsync</c> workaround — needed
/// only because a hand-built context with substituted services tripped EF's pending-model-changes
/// check — is gone: this now runs against the exact schema (and the exact
/// <see cref="IPersonProvisioningService"/> registration) production uses.</para>
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public sealed class PersonProvisioningConcurrencyTests : IAsyncLifetime
{
    private readonly BooksyHostFactory _factory;

    public PersonProvisioningConcurrencyTests(BooksyHostFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>A fresh context in its own scope, for read-back after both sides have run.</summary>
    private UserManagementDbContext NewContext() =>
        _factory.Services.CreateScope().ServiceProvider.GetRequiredService<UserManagementDbContext>();

    /// <summary>Mirrors one complete request scope: its own context and service, from the real container.</summary>
    private async Task<(Guid PersonId, bool IsNewPerson)> ProvisionInOwnScopeAsync(
        PhoneNumber phone, UserType capacity)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPersonProvisioningService>();

        var result = await service.GetOrCreateByPhoneAsync(
            phone, capacity, "Race", capacity.ToString());
        await context.SaveChangesAsync();

        return (result.Person.Id.Value, result.IsNewPerson);
    }

    /// <summary>
    /// History: CONFIRMED FAILING 3/3 runs on 2026-09-08 with the plain SELECT-then-INSERT
    /// implementation, and left red-but-skipped because the intended fix (§1.2, a partial
    /// unique index on users."PhoneNumber") auto-applies at host startup and cannot ship
    /// until the real environment is confirmed free of duplicates. Un-skipped 2026-09-09:
    /// the create path now serializes per canonical phone with a transaction-scoped
    /// advisory lock (<c>pg_advisory_xact_lock(hashtext(phone))</c>) and re-checks under
    /// the lock, so the loser finds the winner's row. §1.2 remains the defence-in-depth
    /// backstop for anything that bypasses <see cref="IPersonProvisioningService"/>.
    /// </summary>
    [Fact]
    public async Task Concurrent_Customer_And_Provider_Signin_For_A_Brand_New_Phone()
    {
        var phone = PhoneNumber.From($"912{Random.Shared.Next(1_000_000, 9_999_999)}");

        var customerTask = ProvisionInOwnScopeAsync(phone, UserType.Customer);
        var providerTask = ProvisionInOwnScopeAsync(phone, UserType.Provider);

        Exception? raceFailure = null;
        (Guid PersonId, bool IsNewPerson)[] results;
        try
        {
            results = await Task.WhenAll(customerTask, providerTask);
        }
        catch (Exception ex)
        {
            // A unique-constraint violation from the loser is an ACCEPTABLE outcome (it means
            // the database, not application logic, caught the race) -- capture it and inspect
            // what actually got committed rather than failing the test on the exception itself.
            raceFailure = ex;
            results = Array.Empty<(Guid, bool)>();
        }

        await using var verify = NewContext();
        var rows = await verify.Users
            .Where(u => u.PhoneNumber != null &&
                (u.PhoneNumber.Value == phone.Value || u.PhoneNumber.NationalNumber == phone.NationalNumber))
            .ToListAsync();

        if (raceFailure is null)
        {
            results.Select(r => r.PersonId).Distinct().Should().ContainSingle(
                "a customer sign-in and a provider sign-in for the same brand-new phone must " +
                "resolve to one person, not two -- both results returned successfully, so both " +
                "must have agreed on which person that is");
        }

        rows.Should().ContainSingle(
            "regardless of how the race resolved in-process, the database must end up holding " +
            "exactly one row for this phone number -- two independent Provider/Customer accounts " +
            "for the same human is the defect '§1.9 no-cross-type-duplicate' exists to catch. " +
            (raceFailure is not null
                ? $"One side threw ({raceFailure.GetType().Name}: {raceFailure.Message}); if the count below is still 2, the database has no constraint stopping this and §1.2 is not just deferred, it is load-bearing."
                : "Both sides returned successfully with no exception."));

        if (rows.Count == 1)
        {
            rows[0].Type.Should().Be(UserType.Both,
                "the single person must carry both capacities once each side has run");
        }
    }
}
