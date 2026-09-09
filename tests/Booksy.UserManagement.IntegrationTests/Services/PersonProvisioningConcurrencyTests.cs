using Booksy.Core.Application.Abstractions.Services;
using Booksy.Core.Domain.ValueObjects;
using Booksy.Infrastructure.Core.EventBus.Abstractions;
using Booksy.Tests.Common.Fixtures;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Services;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Persistence.Repositories;
using Booksy.UserManagement.Infrastructure.Services.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Booksy.UserManagement.IntegrationTests.Services;

/// <summary>
/// refactor-identity-and-membership §1.9: "no-cross-type-duplicate" -- a customer sign-in and a
/// provider sign-in racing for the same brand-new phone number must resolve to ONE person with
/// UserType.Both, never two independent accounts. Uses two fully independent DbContext/repository/
/// service instances (mirroring two real concurrent HTTP requests, each with its own DI scope),
/// against a real PostgreSQL instance -- same lightweight pattern as
/// <see cref="Persistence.UserRepositorySaveTests"/>, no host boot needed since
/// PersonProvisioningService only depends on IUserRepository.
///
/// <para>This is deliberately NOT assumed to pass. §1.2 (a partial unique index on
/// users(phone_number)) is explicitly not yet done, and GetOrCreateByPhoneAsync's own "defence in
/// depth" comment only guards against a lookup that missed a legacy row shape -- it is not a
/// database-level constraint, so two requests that both observe "no existing row" before either
/// commits are not, on inspection, actually prevented from both inserting. The test reports
/// what genuinely happens.</para>
/// </summary>
public sealed class PersonProvisioningConcurrencyTests : IClassFixture<PostgresTestContainerFixture>, IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _postgres;

    public PersonProvisioningConcurrencyTests(PostgresTestContainerFixture postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        await using var context = NewContext();
        // NOT MigrateAsync(): building UserManagementDbContext with substituted
        // ICurrentUserService/IDomainEventDispatcher (as every test in this project does, see
        // UserRepositorySaveTests) makes EF Core 9's pending-model-changes check throw --
        // "has pending changes. Add a new migration before updating the database" -- even
        // though `dotnet ef migrations has-pending-model-changes` against the real DI-composed
        // app reports none. This is a pre-existing defect in the test project, not this test;
        // recorded in FOLLOW-UPS rather than fixed here (fixing it well means finding why the
        // model hash differs by construction path, which is a separate investigation).
        // EnsureCreatedAsync builds the schema straight from the live model with no migration
        // comparison, which is all a throwaway per-fixture database needs anyway.
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private UserManagementDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<UserManagementDbContext>()
            .UseNpgsql(_postgres.ConnectionString)
            .Options;

        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(_ => DateTime.UtcNow);

        return new UserManagementDbContext(
            options,
            Substitute.For<ICurrentUserService>(),
            clock,
            Substitute.For<IDomainEventDispatcher>());
    }

    /// <summary>Mirrors one complete request scope: its own context, repository, and service.</summary>
    private async Task<(Guid PersonId, bool IsNewPerson)> ProvisionInOwnScopeAsync(
        PhoneNumber phone, UserType capacity)
    {
        await using var context = NewContext();
        var repository = new UserRepository(context);
        var service = new PersonProvisioningService(repository, context, NullLogger<PersonProvisioningService>.Instance);

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
    /// backstop for anything that bypasses <see cref="PersonProvisioningService"/>.
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
