using AsanRezerve.Core.Domain.ValueObjects;
using AsanRezerve.ServiceCatalog.Application.Abstractions.Persistence;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Domain.Repositories;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Memberships;

/// <summary>
/// refactor-identity-and-membership §5.5: authorization for the owner-only membership
/// actions, proven through the real HTTP pipeline rather than a mocked handler. This is
/// what caught a real defect the app-layer unit tests (mocked ClaimsPrincipal, no
/// middleware) could not: Terminate/ChangeRoles/RevokeInvitation's "not the owner" branch
/// threw UnauthorizedAccessException, which AsanRezerve.API's ExceptionHandlingMiddleware maps to
/// 401 -- not the 403 every one of these actions' own XML docs promised. Fixed alongside
/// (now ForbiddenException); these tests pin the corrected status code at the boundary.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class MembershipsControllerAuthorizationTests : ServiceCatalogIntegrationTestBase
{
    public MembershipsControllerAuthorizationTests(AsanRezerveHostFactory factory)
        : base(factory) { }

    private async Task<(Guid OrganizationId, Guid MembershipId)> SeedOrgWithStaffAsync()
    {
        var owner = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-authz-owner-{Guid.NewGuid():N}@test.com"));
        var provider = await CreateAndAuthenticateAsProviderAsync(
            email: $"membership-authz-org-{Guid.NewGuid():N}@test.com");
        var staffPerson = UserId.From(await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-authz-staff-{Guid.NewGuid():N}@test.com"));

        var membership = OrganizationMembership.InviteExisting(staffPerson, provider.Id);
        membership.Accept();

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrganizationMembershipRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IServiceCatalogUnitOfWork>();
        await repo.SaveAsync(membership);
        await uow.CommitAsync();

        return (provider.Id.Value, membership.Id);
    }

    [Fact]
    public async Task Terminate_By_A_Stranger_Returns_403_Not_401()
    {
        var (_, membershipId) = await SeedOrgWithStaffAsync();
        var stranger = await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-authz-stranger-{Guid.NewGuid():N}@test.com");
        AuthenticateAsUser(stranger);

        var response = await Client.PostAsync(
            $"/api/v1/memberships/{membershipId}/terminate",
            JsonContent.Create(new { Reason = "trying to remove someone else's staff" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "a stranger is authenticated but not permitted -- that's 403, not 401");
    }

    [Fact]
    public async Task ChangeRoles_By_A_Stranger_Returns_403_Not_401()
    {
        var (_, membershipId) = await SeedOrgWithStaffAsync();
        var stranger = await CreateAndAuthenticateAsRealUserAsync(
            email: $"membership-authz-stranger2-{Guid.NewGuid():N}@test.com");
        AuthenticateAsUser(stranger);

        var response = await Client.PatchAsync(
            $"/api/v1/memberships/{membershipId}/roles",
            JsonContent.Create(new { Roles = new[] { "Manager" } }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "a stranger is authenticated but not permitted -- that's 403, not 401");
    }

    [Fact]
    public async Task Terminate_By_The_Organizations_Owner_Succeeds()
    {
        var (organizationId, membershipId) = await SeedOrgWithStaffAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var providerRepo = scope.ServiceProvider.GetRequiredService<IProviderReadRepository>();
            var provider = await providerRepo.GetByIdAsync(ProviderId.From(organizationId));
            AuthenticateAsUser(provider!.OwnerId.Value);
        }

        var response = await Client.PostAsync(
            $"/api/v1/memberships/{membershipId}/terminate",
            JsonContent.Create(new { Reason = "role ended" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
