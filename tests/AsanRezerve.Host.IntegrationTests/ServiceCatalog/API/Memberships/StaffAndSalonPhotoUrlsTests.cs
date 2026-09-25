using System.Net;
using System.Net.Http.Json;
using AsanRezerve.Infrastructure.Core.Caching;
using AsanRezerve.ServiceCatalog.Domain.Aggregates;
using AsanRezerve.ServiceCatalog.Domain.Aggregates.OrganizationMembershipAggregate;
using AsanRezerve.ServiceCatalog.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AsanRezerve.ServiceCatalog.IntegrationTests.API.Memberships;

/// <summary>
/// Staff photos and the salon logo shown on team and invitation screens are sent as absolute URLs, and the salon
/// logo is the same photo customers see (openspec/changes/_inline/salon-images-load, G4/G5).
///
/// <para>Uploads are stored relative ("/uploads/…" for staff photos, "uploads/…" for gallery photos). These
/// endpoints sent them raw, so any screen not served from the API's own host resolved them against itself and got
/// the SPA's index.html. The salon logo also read only <c>LogoUrl</c>, which the provider app never sets — a
/// gallery-only salon showed no picture at all.</para>
/// </summary>
[Collection(AsanRezerveHostTestCollection.Name)]
public class StaffAndSalonPhotoUrlsTests : ServiceCatalogIntegrationTestBase
{
    public StaffAndSalonPhotoUrlsTests(AsanRezerveHostFactory factory) : base(factory)
    {
    }

    private sealed record Salon(Provider Provider, Guid ServiceId, Guid MembershipId, string StaffPhoto, string GalleryPhoto);

    private async Task<Salon> SalonWithStaffPhotoAndGalleryAsync()
    {
        var provider = await CreateTestProviderWithServicesAsync();
        var id = provider.Id.Value;
        var galleryPhoto = $"uploads/providers/{id}/gallery/{Guid.NewGuid():N}_medium.webp";
        var staffPhoto = $"/uploads/providers/{id}/profile_{Guid.NewGuid():N}.jpg";
        var membershipId = await GetBookableMemberIdAsync(provider);

        // A fresh scope: making the salon bookable saved the provider elsewhere, so the test's tracked copy is stale.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ServiceCatalogDbContext>();
            var fresh = await db.Set<Provider>().SingleAsync(p => p.Id == provider.Id);
            fresh.Profile.AddGalleryImage(fresh.Id, galleryPhoto, galleryPhoto, galleryPhoto);
            var membership = await db.Set<OrganizationMembership>().SingleAsync(m => m.Id == membershipId);
            membership.UpdateStaffDetails(displayName: null, bioOverride: null, photoUrl: staffPhoto);
            await db.SaveChangesAsync();
        }

        // Committed around the unit of work, so no domain event invalidated the provider the fixture already cached
        // (see MakeBookableAsync); a real upload does.
        var cache = Scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cache.RemoveAsync($"Provider:{provider.Id.Value}");
        await cache.RemoveAsync($"Provider:owner:{provider.OwnerId.Value}");

        var service = await GetFirstServiceForProviderAsync(id);
        return new Salon(provider, service.Id.Value, membershipId, staffPhoto, galleryPhoto);
    }

    private static async Task<JToken> BodyAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(text);
        return JToken.Parse(text);
    }

    private static void ShouldAllBeAbsolute(JToken body, string field, string storedPath)
    {
        var values = body.SelectTokens($"$..{field}").Select(t => t.Value<string>()).Where(v => v is not null).ToList();
        values.Should().NotBeEmpty($"the response carries {field}");
        values.Should().AllSatisfy(v =>
        {
            v.Should().StartWith("http", "a relative path resolves against the reader's host, not the API's");
            v.Should().EndWith("/" + storedPath.TrimStart('/'));
        });
    }

    [Fact]
    public async Task The_customer_picking_a_stylist_gets_an_absolute_staff_photo()
    {
        var salon = await SalonWithStaffPhotoAndGalleryAsync();
        ClearAuthenticationHeader();

        var body = await BodyAsync(await Client.GetAsync(
            $"/api/v1/services/provider/{salon.Provider.Id.Value}/{salon.ServiceId}/qualified-staff"));

        ShouldAllBeAbsolute(body, "photoUrl", salon.StaffPhoto);
    }

    [Fact]
    public async Task The_salons_team_list_gets_absolute_staff_photos()
    {
        var salon = await SalonWithStaffPhotoAndGalleryAsync();

        var body = await BodyAsync(await Client.GetAsync(
            $"/api/v1/providers/{salon.Provider.Id.Value}/hierarchy/members"));

        ShouldAllBeAbsolute(body, "photoUrl", salon.StaffPhoto);
    }

    [Fact]
    public async Task Editing_a_member_answers_with_an_absolute_staff_photo()
    {
        var salon = await SalonWithStaffPhotoAndGalleryAsync();
        var newPhoto = $"/uploads/providers/{salon.Provider.Id.Value}/profile_{Guid.NewGuid():N}.jpg";

        var body = await BodyAsync(await Client.PatchAsync(
            $"/api/v1/memberships/{salon.MembershipId}", JsonContent.Create(new { photoUrl = newPhoto })));

        ShouldAllBeAbsolute(body, "photoUrl", newPhoto);
    }

    [Fact]
    public async Task My_memberships_show_the_salons_own_photo_as_an_absolute_url()
    {
        var salon = await SalonWithStaffPhotoAndGalleryAsync();

        var body = await BodyAsync(await Client.GetAsync("/api/v1/memberships/me"));

        var mine = body.SelectTokens("$..organizationId")
            .Single(t => t.Value<string>() == salon.Provider.Id.Value.ToString()).Parent!.Parent!;
        ShouldAllBeAbsolute(mine, "organizationLogo", salon.GalleryPhoto);
    }

    [Fact]
    public async Task An_invitation_shows_the_salons_own_photo_as_an_absolute_url()
    {
        var salon = await SalonWithStaffPhotoAndGalleryAsync();

        var sent = await Client.PostAsJsonAsync(
            $"/api/v1/providers/{salon.Provider.Id.Value}/hierarchy/invitations",
            new { inviteePhoneNumber = $"+9891{Random.Shared.Next(10000000, 99999999)}", inviteeName = "Sara" });
        sent.StatusCode.Should().Be(HttpStatusCode.Created, await sent.Content.ReadAsStringAsync());
        var sentBody = await BodyAsync(sent);
        ShouldAllBeAbsolute(sentBody, "organizationLogo", salon.GalleryPhoto);
        var invitationId = sentBody.SelectToken("$..invitationId")!.Value<string>();

        ClearAuthenticationHeader();
        ShouldAllBeAbsolute(
            await BodyAsync(await Client.GetAsync($"/api/v1/memberships/invitations/{invitationId}")),
            "organizationLogo", salon.GalleryPhoto);
        ShouldAllBeAbsolute(
            await BodyAsync(await Client.GetAsync(
                $"/api/v1/providers/{salon.Provider.Id.Value}/hierarchy/invitations/{invitationId}")),
            "organizationLogo", salon.GalleryPhoto);
    }
}
