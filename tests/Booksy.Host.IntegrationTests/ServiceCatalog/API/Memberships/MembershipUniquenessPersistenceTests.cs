using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Memberships;

/// <summary>
/// refactor-identity-and-membership §3.5/§4.8/§16.3: the "at most one live membership per
/// (person, organization)" and "keep the record when a person leaves and rejoins" invariants are
/// explicitly NOT enforced inside the <see cref="OrganizationMembership"/> aggregate itself (see its
/// own class doc) — only in the application layer's pre-checks (<c>HasActiveMembershipAsync</c>) and,
/// as the actual backstop against a lost race between two concurrent accepts, the partial unique index
/// <c>ux_membership_person_org_active</c> (migration 20260721212656, on (person_id, organization_id)
/// WHERE status &lt;&gt; 'Terminated' AND person_id IS NOT NULL). Nothing in the suite exercised that
/// index directly before this — these tests do, by racing the DB itself rather than the application
/// pre-check, which a concurrent request can always lose.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class MembershipUniquenessPersistenceTests : ServiceCatalogIntegrationTestBase
{
    public MembershipUniquenessPersistenceTests(BooksyHostFactory factory)
        : base(factory) { }

    private async Task SaveAsync(OrganizationMembership membership)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        await repo.SaveAsync(membership);
        await uow.CommitAsync();
    }

    [Fact]
    public async Task A_second_active_membership_for_the_same_person_and_org_is_rejected_by_the_database()
    {
        // Distinct emails per test: all [Fact]s in this class share one database (one fixture per
        // class), and both User.Email and Provider.ContactInfo.Email are uniquely indexed.
        var personId = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-uniq-{Guid.NewGuid():N}@test.com"));
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"membership-uniq-org-{Guid.NewGuid():N}@test.com");
        var organizationId = provider.Id;

        // First accept wins the application-layer pre-check and persists cleanly.
        var first = OrganizationMembership.CreateOwner(personId, organizationId, providesServices: false);
        await SaveAsync(first);

        // A second, independent membership for the exact same (person, organization) — modelling a
        // concurrent accept that raced past the application's own HasActiveMembershipAsync guard
        // before the first one committed. The aggregate has no way to see this on its own; only the
        // database can still refuse it.
        var second = OrganizationMembership.CreateOwner(personId, organizationId, providesServices: false);

        var act = async () => await SaveAsync(second);

        await act.Should().ThrowAsync<DbUpdateException>(
            "the partial unique index ux_membership_person_org_active must reject a second live " +
            "membership for the same person in the same organization, even when application-layer " +
            "checks are bypassed or lost a race");

        using var verifyScope = Factory.Services.CreateScope();
        var repo = verifyScope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var all = await repo.GetByOrganizationAsync(organizationId);
        all.Should().ContainSingle(m => m.PersonId == personId, "only the first membership must exist");
    }

    [Fact]
    public async Task Terminating_a_membership_lets_the_same_person_rejoin_the_same_org()
    {
        var personId = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-rejoin-{Guid.NewGuid():N}@test.com"));
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"membership-rejoin-org-{Guid.NewGuid():N}@test.com");
        var organizationId = provider.Id;

        var original = OrganizationMembership.CreateOwner(personId, organizationId, providesServices: false);
        await SaveAsync(original);

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var toTerminate = await repo.GetByIdAsync(original.Id);
            toTerminate!.Terminate("left the organization");
            await repo.UpdateAsync(toTerminate);
            await uow.CommitAsync();
        }

        // The partial index's "status <> 'Terminated'" clause must exclude the now-terminated row, so
        // rejoining does not collide with their own history.
        var rejoined = OrganizationMembership.CreateOwner(personId, organizationId, providesServices: false);
        var act = async () => await SaveAsync(rejoined);

        await act.Should().NotThrowAsync(
            "a terminated membership must not block the same person from rejoining the same organization later");

        using var verifyScope = Factory.Services.CreateScope();
        var verifyRepo = verifyScope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var all = await verifyRepo.GetByOrganizationAsync(organizationId);
        all.Should().HaveCount(2, "the terminated history row and the new active membership both exist");
        all.Should().ContainSingle(m => m.PersonId == personId && m.Status == MembershipStatus.Active);
        all.Should().ContainSingle(m => m.PersonId == personId && m.Status == MembershipStatus.Terminated);
    }

    [Fact]
    public async Task Two_unclaimed_staff_members_in_the_same_org_do_not_collide()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"membership-unclaimed-org-{Guid.NewGuid():N}@test.com");
        var organizationId = provider.Id;

        // Both carry PersonId = null (no account yet) — the index's "person_id IS NOT NULL" clause
        // must exclude unclaimed rows entirely, so the salon can add any number of accountless staff.
        var first = OrganizationMembership.CreateUnclaimed(organizationId, "Staff One");
        var second = OrganizationMembership.CreateUnclaimed(organizationId, "Staff Two");

        await SaveAsync(first);
        var act = async () => await SaveAsync(second);

        await act.Should().NotThrowAsync(
            "unclaimed memberships (PersonId null) must never collide with each other or with claimed ones");

        using var verifyScope = Factory.Services.CreateScope();
        var repo = verifyScope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var all = await repo.GetByOrganizationAsync(organizationId);
        all.Should().HaveCount(2);
        all.Should().OnlyContain(m => m.IsUnclaimed);
    }
}
