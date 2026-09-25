using System.Net;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.Tests.Common;
using FluentAssertions;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Authorization;

/// <summary>
/// One person per phone number: when a salon owner's phone is also used on the customer side, the person becomes
/// <c>UserType.Both</c>, and a refreshed access token carries <c>user_type=Both</c>. The provider policies accepted only
/// "Provider"/"Admin", so after the first silent refresh the salon's own app got 403 on every provider route — the QA
/// walkthrough of 2026-09-22 showed Home "بارگذاری ناموفق بود" and an empty, failing calendar while the inbox (plain
/// [Authorize]) still worked. The shared test provider always carries "Provider", which is why no test saw it.
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class TwoSidedPersonTests : ServiceCatalogIntegrationTestBase
{
    public TwoSidedPersonTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    /// <summary>The claims a refreshed token of a person who is both a salon owner and a customer carries.</summary>
    private void AuthenticateAsTwoSidedOwner(Provider provider) => AuthenticateAs(new TestUser
    {
        UserId = provider.OwnerId.Value.ToString(),
        Email = provider.ContactInfo.Email.Value,
        Name = provider.Profile.BusinessName,
        Role = "Provider,Customer",
        AdditionalClaims = new Dictionary<string, string>
        {
            { "providerId", provider.Id.Value.ToString() },
            { "user_type", "Both" },
        },
    });

    [Fact]
    public async Task A_salon_owner_who_is_also_a_customer_can_list_their_bookings()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Two Sided Salon", $"{Guid.NewGuid():N}@test.com");
        AuthenticateAsTwoSidedOwner(provider);

        var response = await Client.GetAsync($"/api/v1/Bookings/provider/{provider.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Being_two_sided_grants_no_access_to_another_salon()
    {
        // The policy admits "Both" as a provider; which salon it may act for is still the per-request ownership check.
        var mine = await CreateAndAuthenticateAsProviderAsync("Mine", $"{Guid.NewGuid():N}@test.com");
        var theirs = await CreateAndAuthenticateAsProviderAsync("Theirs", $"{Guid.NewGuid():N}@test.com");
        AuthenticateAsTwoSidedOwner(mine);

        var response = await Client.GetAsync($"/api/v1/Bookings/provider/{theirs.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_customer_who_is_not_two_sided_still_cannot()
    {
        var provider = await CreateAndAuthenticateAsProviderAsync("Other Salon", $"{Guid.NewGuid():N}@test.com");
        AuthenticateAs(TestUser.Customer($"{Guid.NewGuid():N}@test.com"));

        var response = await Client.GetAsync($"/api/v1/Bookings/provider/{provider.Id.Value}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
