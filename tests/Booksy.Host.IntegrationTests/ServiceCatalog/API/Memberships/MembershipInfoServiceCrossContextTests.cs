using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Application.Abstractions.Persistence;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Repositories;
using Booksy.UserManagement.Application.Services.Interfaces;
using Booksy.UserManagement.Infrastructure.Persistence.Context;
using Booksy.UserManagement.Infrastructure.Services.External;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Memberships;

/// <summary>
/// refactor-identity-and-membership §5.4: the JWT that carries a person's memberships
/// resolves them through <see cref="MembershipInfoService"/>, the reverse direction of the
/// same cross-context seam as ServiceCatalog's PersonDirectoryReadService -- raw SQL over the
/// shared connection, reading ServiceCatalog's "organization_memberships" table from a
/// UserManagement-owned service. These tests prove the seam actually reads real rows written
/// by ServiceCatalog's own repository, using the same host boot (both schemas present) that
/// the rest of this project already relies on.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class MembershipInfoServiceCrossContextTests : ServiceCatalogIntegrationTestBase
{
    public MembershipInfoServiceCrossContextTests(BooksyHostFactory factory)
        : base(factory) { }

    private async Task SaveMembershipAsync(OrganizationMembership membership)
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        await repo.SaveAsync(membership);
        await uow.CommitAsync();
    }

    private async Task<IReadOnlyList<MembershipSummary>> QueryMembershipsAsync(Guid personId)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var service = new MembershipInfoService(dbContext);
        return await service.GetMembershipsForPersonAsync(personId);
    }

    [Fact]
    public async Task Finds_an_active_membership_written_by_ServiceCatalogs_own_repository()
    {
        var personId = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-info-{Guid.NewGuid():N}@test.com"));
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"membership-info-org-{Guid.NewGuid():N}@test.com");

        var membership = OrganizationMembership.CreateOwner(personId, provider.Id, providesServices: false);
        await SaveMembershipAsync(membership);

        var result = await QueryMembershipsAsync(personId.Value);

        result.Should().ContainSingle();
        var found = result[0];
        found.MembershipId.Should().Be(membership.Id);
        found.OrganizationId.Should().Be(provider.Id.Value);
        found.Roles.Should().Be("Owner");
        found.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Excludes_a_terminated_membership()
    {
        var personId = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-info-term-{Guid.NewGuid():N}@test.com"));
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"membership-info-term-org-{Guid.NewGuid():N}@test.com");

        var membership = OrganizationMembership.CreateOwner(personId, provider.Id, providesServices: false);
        await SaveMembershipAsync(membership);

        using (var scope = Factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
            var toTerminate = await repo.GetByIdAsync(membership.Id);
            toTerminate!.Terminate("left");
            await repo.UpdateAsync(toTerminate);
            await uow.CommitAsync();
        }

        var result = await QueryMembershipsAsync(personId.Value);

        result.Should().BeEmpty("a terminated membership must not appear in the token's memberships[]");
    }

    [Fact]
    public async Task Excludes_another_persons_memberships()
    {
        var personId = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-info-mine-{Guid.NewGuid():N}@test.com"));
        var otherPersonId = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-info-other-{Guid.NewGuid():N}@test.com"));
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"membership-info-shared-org-{Guid.NewGuid():N}@test.com");

        await SaveMembershipAsync(
            OrganizationMembership.CreateOwner(personId, provider.Id, providesServices: false));
        await SaveMembershipAsync(
            OrganizationMembership.InviteExisting(otherPersonId, provider.Id));

        var result = await QueryMembershipsAsync(personId.Value);

        result.Should().ContainSingle(m => m.OrganizationId == provider.Id.Value);
    }
}
