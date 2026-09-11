using Booksy.API;
using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Membership;

/// <summary>
/// Who may act on a salon, proven at the real HTTP boundary.
///
/// <para>Provider-scoped routes used to ask "does the caller OWN this provider?". An
/// employed stylist, receptionist or manager owns nothing, so every one of those routes
/// answered 403 and the provider app was blank for anyone but a salon owner. Access is
/// now decided by an active membership of the salon.</para>
///
/// <para>These run through the controller so they cover the authorization policy, the
/// claims plumbing and the membership lookup together — the unit tests for
/// <c>CanManageOrganizationQueryHandler</c> cover the rule itself, and neither would have
/// caught a controller still calling the old owner-only check.</para>
/// </summary>
[Collection(ServiceCatalogTestCollection.Name)]
public class MembershipAuthorizationTests : ServiceCatalogIntegrationTestBase
{
    public MembershipAuthorizationTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Creates a real person, makes them an active member of the salon with the given
    /// roles, and authenticates as them WITHOUT a providerId claim — exactly the token an
    /// employee carries, and the case the owner-only check used to reject.
    /// </summary>
    private async Task<Guid> AuthenticateAsMemberAsync(
        Domain.Aggregates.Provider organization,
        string email,
        params MembershipRole[] roles)
    {
        var personId = await CreateAndAuthenticateAsRealUserAsync(email);

        var membership = OrganizationMembership.InviteExisting(
            UserId.From(personId), organization.Id);
        membership.Accept();
        foreach (var role in roles)
            membership.AssignRole(role);

        await CreateEntityAsync(membership);

        AuthenticateAsEmployee(personId, email);

        return personId;
    }

    /// <summary>
    /// Authenticates as a plain employee: <c>user_type: Provider</c> — which is what the
    /// invitation flow gives a new staff account — but NO providerId claim and no
    /// ownership, since they own no salon. This is the token the owner-only check used to
    /// reject on every provider-scoped route.
    /// </summary>
    private void AuthenticateAsEmployee(Guid personId, string email) =>
        AuthenticateAs(new TestUser
        {
            UserId = personId.ToString(),
            Email = email,
            Name = email,
            Role = "Provider",
            AdditionalClaims = new Dictionary<string, string>
            {
                { "user_type", "Provider" }
            }
        });

    [Fact]
    public async Task An_Employed_Stylist_Can_Read_Their_Salons_Bookings()
    {
        // The regression this change exists for.
        var organization = await CreateAndAuthenticateAsProviderAsync(
            "Aria Barbers", "aria-owner@test.com");
        await AuthenticateAsMemberAsync(
            organization, "stylist@test.com", MembershipRole.StaffProvider);

        var response = await GetAsync($"/api/v1/Bookings/provider/{organization.Id.Value}");

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            "an active member of the salon works there, so the day book is theirs to see");
    }

    [Fact]
    public async Task A_Receptionist_Can_Read_The_Salons_Bookings()
    {
        var organization = await CreateAndAuthenticateAsProviderAsync(
            "Pars Salon", "pars-owner@test.com");
        await AuthenticateAsMemberAsync(
            organization, "reception@test.com", MembershipRole.Receptionist);

        var response = await GetAsync($"/api/v1/Bookings/provider/{organization.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Someone_With_No_Membership_Still_Cannot_Read_A_Salons_Bookings()
    {
        // Loosening the rule from "owner" to "member" must not loosen it to "anyone".
        var organization = await CreateAndAuthenticateAsProviderAsync(
            "Closed Doors", "closed-owner@test.com");

        // Authenticated as an employee-shaped token so the ProviderOrAdmin policy passes
        // and the MEMBERSHIP rule is what actually decides — otherwise this would pass
        // for the wrong reason, on the user_type claim alone.
        var stranger = await CreateAndAuthenticateAsRealUserAsync("stranger@test.com");
        AuthenticateAsEmployee(stranger, "stranger@test.com");

        var response = await GetAsync($"/api/v1/Bookings/provider/{organization.Id.Value}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_Member_Of_Another_Salon_Cannot_Read_This_Salons_Bookings()
    {
        // A person can work at several salons; a membership at one grants nothing at another.
        var mySalon = await CreateAndAuthenticateAsProviderAsync(
            "My Salon", "my-owner@test.com");
        var otherSalon = await CreateAndAuthenticateAsProviderAsync(
            "Other Salon", "other-owner@test.com");

        await AuthenticateAsMemberAsync(
            mySalon, "moonlighter@test.com", MembershipRole.StaffProvider);

        var response = await GetAsync($"/api/v1/Bookings/provider/{otherSalon.Id.Value}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_Terminated_Member_Loses_Access()
    {
        var organization = await CreateAndAuthenticateAsProviderAsync(
            "Revolving Door", "revolving-owner@test.com");

        var personId = await CreateAndAuthenticateAsRealUserAsync("leaver@test.com");
        var membership = OrganizationMembership.InviteExisting(
            UserId.From(personId), organization.Id);
        membership.Accept();
        membership.AssignRole(MembershipRole.StaffProvider);
        membership.Terminate("moved away");
        await CreateEntityAsync(membership);

        AuthenticateAsEmployee(personId, "leaver@test.com");

        var response = await GetAsync($"/api/v1/Bookings/provider/{organization.Id.Value}");

        // Terminating a membership is how a salon actually revokes access.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_Stylist_Cannot_Rewrite_The_Salons_Settings()
    {
        // Working at a salon is not running it: the two permissions are separate.
        var organization = await CreateAndAuthenticateAsProviderAsync(
            "Not Yours", "notyours-owner@test.com");
        await AuthenticateAsMemberAsync(
            organization, "stylist2@test.com", MembershipRole.StaffProvider);

        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{organization.Id.Value}/settings/business-hours",
            new { businessHours = new Dictionary<string, object>() });

        response.StatusCode.Should().NotBe(
            HttpStatusCode.OK,
            "a stylist working here does not get to change the shop's opening hours");
    }
}
