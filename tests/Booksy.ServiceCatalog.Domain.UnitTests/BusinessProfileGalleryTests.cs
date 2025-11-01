using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.Entities;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.UnitTests;

public class BusinessProfileGalleryTests
{
    private readonly ProviderId _providerId = ProviderId.New();

    [Fact]
    public void AddGalleryImage_Should_Add_Image_Successfully()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description");

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
        var profile = BusinessProfile.Create("Test Business", "Test Description");

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
        var profile = BusinessProfile.Create("Test Business", "Test Description");

        // Add 50 images (max limit)
        for (int i = 0; i < 50; i++)
        {
            profile.AddGalleryImage(_providerId, $"url{i}", $"thumb{i}", $"medium{i}");
        }

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            profile.AddGalleryImage(_providerId, "url51", "thumb51", "medium51"));

        Assert.Contains("Cannot add more than 50 gallery images", exception.Message);
    }

    [Fact]
    public void RemoveGalleryImage_Should_Deactivate_Image()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description");
        var image = profile.AddGalleryImage(_providerId, "url", "thumb", "medium");

        // Act
        profile.RemoveGalleryImage(image.Id);

        // Assert
        Assert.False(image.IsActive);
        Assert.Single(profile.GalleryImages);
    }

    [Fact]
    public void RemoveGalleryImage_Should_Throw_When_Image_Not_Found()
    {
        // Arrange
        var profile = BusinessProfile.Create("Test Business", "Test Description");
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
        var profile = BusinessProfile.Create("Test Business", "Test Description");
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
        var profile = BusinessProfile.Create("Test Business", "Test Description");
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
        var profile = BusinessProfile.Create("Test Business", "Test Description");
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
        var profile = BusinessProfile.Create("Test Business", "Test Description");
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = profile.GetGalleryImage(nonExistentId);

        // Assert
        Assert.Null(result);
    }
}
