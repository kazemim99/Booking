using System.Net;
using FluentAssertions;
using Xunit;

namespace Booksy.ServiceCatalog.IntegrationTests.API.Authorization;

/// <summary>
/// A salon's roster lists its people WITH their phone numbers, and its pending invitations list the numbers invited.
/// Both only asked for a sign-in, so any signed-in customer, or any other salon, could read any salon's staff phones
/// (found 2026-09-23, openspec/changes/_inline/qa-walkthrough-2026-09-23b; the user decided: restrict it). Only
/// the salon's own people — any active member, as for its day book — or an admin may read them now.
/// </summary>
[Collection(BooksyHostTestCollection.Name)]
public class SalonRosterIsTheSalonsOwnTests : ServiceCatalogIntegrationTestBase
{
    public SalonRosterIsTheSalonsOwnTests(BooksyHostFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("members")]
    [InlineData("invitations")]
    public async Task Another_salon_cannot_read_it(string list)
    {
        var salon = await CreateTestProviderWithServicesAsync();
        var otherSalon = await CreateTestProviderWithServicesAsync();

        AuthenticateAsProviderOwner(otherSalon);
        var response = await Client.GetAsync($"/api/v1/providers/{salon.Id.Value}/hierarchy/{list}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("members")]
    [InlineData("invitations")]
    public async Task A_customer_cannot_read_it(string list)
    {
        var salon = await CreateTestProviderWithServicesAsync();

        AuthenticateAsUser(Guid.NewGuid(), "customer@test.com");
        var response = await Client.GetAsync($"/api/v1/providers/{salon.Id.Value}/hierarchy/{list}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("members")]
    [InlineData("invitations")]
    public async Task The_salons_owner_still_reads_it(string list)
    {
        var salon = await CreateTestProviderWithServicesAsync();

        AuthenticateAsProviderOwner(salon);
        var response = await Client.GetAsync($"/api/v1/providers/{salon.Id.Value}/hierarchy/{list}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }
}
