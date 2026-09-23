using Booksy.Core.Domain.ValueObjects;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.Domain.Enums;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests.ProviderAggregate;

/// <summary>
/// Editing a salon's name or description must not touch its photos
/// (openspec/changes/_inline/salon-images-load, task 1).
///
/// <para>Both edits used to replace the owned <c>BusinessProfile</c> with a fresh one, which carries an empty
/// gallery. The write repository then deleted every photo row missing from the new profile — so a salon that
/// renamed itself from the provider app lost its whole gallery, and every customer card fell back to the
/// placeholder.</para>
/// </summary>
public class ProviderBusinessProfileEditTests
{
    private const string Photo = "uploads/providers/p/gallery/a_medium.webp";
    private const string OtherPhoto = "uploads/providers/p/gallery/b_medium.webp";

    private static ContactInfo NewContactInfo() => ContactInfo.Create(
        Email.Create("owner@salon.example"),
        PhoneNumber.From("+989120000000"));

    private static BusinessAddress NewAddress() => BusinessAddress.Create(
        "خیابان ولیعصر، پلاک ۱۲", "خیابان ولیعصر", "تهران", "تهران", "1234567890", "IR");

    private static Provider SalonWithPhotos(Func<Provider> create)
    {
        var provider = create();
        provider.Profile.AddGalleryImage(provider.Id, OtherPhoto, OtherPhoto, OtherPhoto);
        provider.Profile.AddGalleryImage(provider.Id, Photo, Photo, Photo);
        provider.Profile.SetPrimaryGalleryImage(provider.Profile.GalleryImages.Single(i => i.MediumUrl == Photo).Id);
        provider.Profile.AddTag("رنگ مو");
        provider.Profile.AddSocialMedia("instagram", "https://instagram.com/nahal");
        return provider;
    }

    private static Provider ActiveSalon() => SalonWithPhotos(() => Provider.RegisterProvider(
        UserId.From(Guid.NewGuid()), "سالن نهال", "سالن زیبایی", ServiceCategory.BeautySalon,
        NewContactInfo(), NewAddress()));

    private static Provider DraftSalon(string? logoUrl = null) => SalonWithPhotos(() => Provider.CreateDraft(
        UserId.From(Guid.NewGuid()), "نهال", "کاظمی", "سالن نهال", "سالن زیبایی", ServiceCategory.BeautySalon,
        NewContactInfo(), NewAddress(), logoUrl: logoUrl));

    [Fact]
    public void Renaming_the_salon_keeps_every_photo_and_the_one_it_chose()
    {
        var provider = ActiveSalon();

        provider.UpdateBusinessProfile("سالن نهال نو", "توضیح تازه", profileImageUrl: null);

        Assert.Equal("سالن نهال نو", provider.Profile.BusinessName);
        Assert.Equal("توضیح تازه", provider.Profile.BusinessDescription);
        Assert.Equal(2, provider.Profile.GalleryImages.Count);
        Assert.Equal(Photo, provider.Profile.DisplayImageUrl);
    }

    [Fact]
    public void Renaming_the_salon_keeps_its_tags_and_social_links()
    {
        var provider = ActiveSalon();

        provider.UpdateBusinessProfile("سالن نهال نو", "توضیح تازه", profileImageUrl: null);

        Assert.Contains("رنگ مو", provider.Profile.Tags);
        Assert.Equal("https://instagram.com/nahal", provider.Profile.SocialMedia["instagram"]);
    }

    [Fact]
    public void Renaming_the_salon_keeps_the_same_profile_rather_than_replacing_it()
    {
        // The owned profile's identity is what persistence tracks; a new instance is what made the old gallery
        // rows look removed.
        var provider = ActiveSalon();
        var before = provider.Profile;

        provider.UpdateBusinessProfile("سالن نهال نو", "توضیح تازه", profileImageUrl: null);

        Assert.Same(before, provider.Profile);
    }

    [Fact]
    public void A_null_profile_image_keeps_the_stored_one_and_a_new_one_replaces_it()
    {
        var provider = ActiveSalon();
        provider.UpdateBusinessProfile("سالن نهال", "سالن زیبایی", "/uploads/providers/p/profile_1.jpg");

        provider.UpdateBusinessProfile("سالن نهال نو", "سالن زیبایی", profileImageUrl: null);
        Assert.Equal("/uploads/providers/p/profile_1.jpg", provider.Profile.ProfileImageUrl);

        provider.UpdateBusinessProfile("سالن نهال نو", "سالن زیبایی", "/uploads/providers/p/profile_2.jpg");
        Assert.Equal("/uploads/providers/p/profile_2.jpg", provider.Profile.ProfileImageUrl);
    }

    [Fact]
    public void Going_back_in_registration_keeps_the_photos_already_uploaded()
    {
        var provider = DraftSalon();

        provider.UpdateDraftInfo("نهال", "کاظمی", "سالن نهال نو", "توضیح تازه", ServiceCategory.HairSalon,
            NewContactInfo(), NewAddress());

        Assert.Equal("سالن نهال نو", provider.Profile.BusinessName);
        Assert.Equal(2, provider.Profile.GalleryImages.Count);
        Assert.Equal(Photo, provider.Profile.DisplayImageUrl);
        Assert.Contains("رنگ مو", provider.Profile.Tags);
    }

    [Fact]
    public void Going_back_in_registration_without_a_logo_keeps_the_stored_logo()
    {
        var provider = DraftSalon(logoUrl: "/uploads/providers/p/logo_1.png");

        provider.UpdateDraftInfo("نهال", "کاظمی", "سالن نهال", "سالن زیبایی", ServiceCategory.BeautySalon,
            NewContactInfo(), NewAddress(), logoUrl: null);
        Assert.Equal("/uploads/providers/p/logo_1.png", provider.Profile.LogoUrl);

        provider.UpdateDraftInfo("نهال", "کاظمی", "سالن نهال", "سالن زیبایی", ServiceCategory.BeautySalon,
            NewContactInfo(), NewAddress(), logoUrl: "/uploads/providers/p/logo_2.png");
        Assert.Equal("/uploads/providers/p/logo_2.png", provider.Profile.LogoUrl);
    }
}
