using AsanRezerve.Core.Domain.Exceptions;
using AsanRezerve.ServiceCatalog.Domain.Entities;
using AsanRezerve.ServiceCatalog.Domain.ValueObjects;

namespace AsanRezerve.ServiceCatalog.Domain.UnitTests;

public class BusinessProfileGalleryTests
{
    private readonly ProviderId _providerId = ProviderId.New();

    [Fact]
    public void AddGalleryImage_Should_Add_Image_Successfully()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");

        // Act
        var image = profile.AddGalleryImage(
            _providerId,
            "https://example.com/image.webp",
            "https://example.com/thumb.webp",
            "https://example.com/medium.webp");

        // Assert
        Assert.NotNull(image);
        Assert.Single(profile.GalleryImages);
        Assert.Equal(0, image.DisplayOrder);
        Assert.True(image.IsActive);
    }

    [Fact]
    public void AddGalleryImage_Should_Assign_Sequential_Display_Order()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");

        // Act
        var image1 = profile.AddGalleryImage(_providerId, "url1", "thumb1", "medium1");
        var image2 = profile.AddGalleryImage(_providerId, "url2", "thumb2", "medium2");
        var image3 = profile.AddGalleryImage(_providerId, "url3", "thumb3", "medium3");

        // Assert
        Assert.Equal(0, image1.DisplayOrder);
        Assert.Equal(1, image2.DisplayOrder);
        Assert.Equal(2, image3.DisplayOrder);
    }

    [Fact]
    public void AddGalleryImage_Should_Throw_When_Max_Limit_Reached()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");

        // Add images up to the max limit (20)
        for (int i = 0; i < 20; i++)
        {
            profile.AddGalleryImage(_providerId, $"url{i}", $"thumb{i}", $"medium{i}");
        }

        // Act & Assert — the next one exceeds the limit
        var exception = Assert.Throws<DomainValidationException>(() =>
            profile.AddGalleryImage(_providerId, "url21", "thumb21", "medium21"));

        Assert.Contains("Cannot add more than 20 gallery images", exception.Message);
    }

    [Fact]
    public void RemoveGalleryImage_Should_Remove_Image_Completely()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");
        var image = profile.AddGalleryImage(_providerId, "url", "thumb", "medium");

        // Act
        profile.RemoveGalleryImage(image.Id);

        // Assert - Image should be completely removed from collection
        Assert.Empty(profile.GalleryImages);
    }

    [Fact]
    public void RemoveGalleryImage_Should_Throw_When_Image_Not_Found()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            profile.RemoveGalleryImage(nonExistentId));

        Assert.Contains("Gallery image not found", exception.Message);
    }

    [Fact]
    public void ReorderGalleryImages_Should_Update_Display_Order()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");
        var image1 = profile.AddGalleryImage(_providerId, "url1", "thumb1", "medium1");
        var image2 = profile.AddGalleryImage(_providerId, "url2", "thumb2", "medium2");
        var image3 = profile.AddGalleryImage(_providerId, "url3", "thumb3", "medium3");

        var newOrder = new Dictionary<Guid, int>
        {
            { image1.Id, 2 },
            { image2.Id, 0 },
            { image3.Id, 1 }
        };

        // Act
        profile.ReorderGalleryImages(newOrder);

        // Assert
        Assert.Equal(2, image1.DisplayOrder);
        Assert.Equal(0, image2.DisplayOrder);
        Assert.Equal(1, image3.DisplayOrder);
    }

    [Fact]
    public void ReorderGalleryImages_Should_Throw_When_Image_Not_Found()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");
        var nonExistentId = Guid.NewGuid();

        var newOrder = new Dictionary<Guid, int>
        {
            { nonExistentId, 0 }
        };

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            profile.ReorderGalleryImages(newOrder));

        Assert.Contains("Gallery image", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void GetGalleryImage_Should_Return_Image_When_Exists()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");
        var image = profile.AddGalleryImage(_providerId, "url", "thumb", "medium");

        // Act
        var result = profile.GetGalleryImage(image.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(image.Id, result.Id);
    }

    [Fact]
    public void GetGalleryImage_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description", "\"profileImageUrl\"");
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = profile.GetGalleryImage(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    // ----------------------------------------------------------------------------------------------
    // Rule: every meaningful gallery change advances Profile.LastUpdatedAt.
    //
    // Add, remove, reorder and set-primary already did. Metadata editing was the gap — callers reached
    // the child image through GetGalleryImage and mutated it directly, so the profile's timestamp never
    // moved and anything using it for cache invalidation or "last changed" display saw a stale value.
    // ----------------------------------------------------------------------------------------------

    private static (BusinessProfile Profile, Guid ImageId) ProfileWithOneImage()
    {
        var profile = BusinessProfile.Create("Test Business", "Test Description", "profileImageUrl");
        var image = profile.AddGalleryImage(
            ProviderId.New(),
            "https://example.com/image.webp",
            "https://example.com/thumb.webp",
            "https://example.com/medium.webp");
        return (profile, image.Id);
    }

    [Fact]
    public void UpdateGalleryImageMetadata_Should_Advance_LastUpdatedAt()
    {
        var (profile, imageId) = ProfileWithOneImage();
        var before = profile.LastUpdatedAt;

        var changed = profile.UpdateGalleryImageMetadata(imageId, "A caption", "Alt text");

        Assert.True(changed);
        Assert.True(
            profile.LastUpdatedAt > before,
            "editing a gallery image's metadata is a meaningful change and must advance LastUpdatedAt");
    }

    [Fact]
    public void UpdateGalleryImageMetadata_Should_Apply_The_New_Values()
    {
        var (profile, imageId) = ProfileWithOneImage();

        profile.UpdateGalleryImageMetadata(imageId, "A caption", "Alt text");

        var image = profile.GetGalleryImage(imageId);
        Assert.Equal("A caption", image!.Caption);
        Assert.Equal("Alt text", image.AltText);
    }

    [Fact]
    public void UpdateGalleryImageMetadata_Should_Be_A_NoOp_When_Nothing_Changes()
    {
        var (profile, imageId) = ProfileWithOneImage();
        profile.UpdateGalleryImageMetadata(imageId, "A caption", "Alt text");
        var after = profile.LastUpdatedAt;

        var changed = profile.UpdateGalleryImageMetadata(imageId, "A caption", "Alt text");

        Assert.False(changed);
        Assert.Equal(after, profile.LastUpdatedAt);
    }

    [Fact]
    public void UpdateGalleryImageMetadata_Should_Reject_An_Unknown_Image()
    {
        var (profile, _) = ProfileWithOneImage();

        Assert.Throws<DomainValidationException>(
            () => profile.UpdateGalleryImageMetadata(Guid.NewGuid(), "caption", "alt"));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("reorder")]
    [InlineData("set-primary")]
    [InlineData("metadata")]
    public void Every_Meaningful_Gallery_Change_Advances_LastUpdatedAt(string operation)
    {
        var profile = BusinessProfile.Create("Test Business", "Test Description", "profileImageUrl");
        var providerId = ProviderId.New();
        var first = profile.AddGalleryImage(providerId, "url1", "thumb1", "medium1");
        var second = profile.AddGalleryImage(providerId, "url2", "thumb2", "medium2");

        var before = profile.LastUpdatedAt;

        switch (operation)
        {
            case "add":
                profile.AddGalleryImage(providerId, "url3", "thumb3", "medium3");
                break;
            case "remove":
                profile.RemoveGalleryImage(second.Id);
                break;
            case "reorder":
                profile.ReorderGalleryImages(new Dictionary<Guid, int>
                {
                    [first.Id] = 1,
                    [second.Id] = 0,
                });
                break;
            case "set-primary":
                profile.SetPrimaryGalleryImage(second.Id);
                break;
            case "metadata":
                profile.UpdateGalleryImageMetadata(first.Id, "new caption", "new alt");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }

        Assert.True(
            profile.LastUpdatedAt > before,
            $"'{operation}' is a meaningful gallery change and must advance Profile.LastUpdatedAt");
    }
}
