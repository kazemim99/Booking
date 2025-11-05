using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests;

public class GalleryImageTests
{
    private readonly ProviderId _providerId = ProviderId.New();

    [Fact]
    public void Create_Should_Create_Active_Image_With_Default_Values()
    {
        // Arrange & Act
        var image = GalleryImage.Create(
            _providerId,
            "https://example.com/image.webp",
            "https://example.com/thumb.webp",
            "https://example.com/medium.webp",
            0);

        // Assert
        Assert.NotEqual(Guid.Empty, image.Id);
        Assert.Equal(_providerId, image.ProviderId);
        Assert.Equal("https://example.com/image.webp", image.ImageUrl);
        Assert.Equal("https://example.com/thumb.webp", image.ThumbnailUrl);
        Assert.Equal("https://example.com/medium.webp", image.MediumUrl);
        Assert.Equal(0, image.DisplayOrder);
        Assert.True(image.IsActive);
        Assert.Null(image.Caption);
        Assert.Null(image.AltText);
        Assert.True((DateTime.UtcNow - image.UploadedAt).TotalSeconds < 5);
    }

    [Fact]
    public void UpdateMetadata_Should_Update_Caption_And_AltText()
    {
        // Arrange
        var image = GalleryImage.Create(_providerId, "url", "thumb", "medium", 0);

        // Act
        image.UpdateMetadata("Beautiful sunset", "A stunning sunset over the ocean");

        // Assert
        Assert.Equal("Beautiful sunset", image.Caption);
        Assert.Equal("A stunning sunset over the ocean", image.AltText);
    }

    [Fact]
    public void UpdateMetadata_Should_Allow_Null_Values()
    {
        // Arrange
        var image = GalleryImage.Create(_providerId, "url", "thumb", "medium", 0);
        image.UpdateMetadata("Caption", "Alt text");

        // Act
        image.UpdateMetadata(null, null);

        // Assert
        Assert.Null(image.Caption);
        Assert.Null(image.AltText);
    }

    [Fact]
    public void UpdateDisplayOrder_Should_Update_Order()
    {
        // Arrange
        var image = GalleryImage.Create(_providerId, "url", "thumb", "medium", 0);

        // Act
        image.UpdateDisplayOrder(5);

        // Assert
        Assert.Equal(5, image.DisplayOrder);
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_To_False()
    {
        // Arrange
        var image = GalleryImage.Create(_providerId, "url", "thumb", "medium", 0);

        // Act
        image.Deactivate();

        // Assert
        Assert.False(image.IsActive);
    }

    [Fact]
    public void Reactivate_Should_Set_IsActive_To_True()
    {
        // Arrange
        var image = GalleryImage.Create(_providerId, "url", "thumb", "medium", 0);
        image.Deactivate();

        // Act
        image.Reactivate();

        // Assert
        Assert.True(image.IsActive);
    }
}
