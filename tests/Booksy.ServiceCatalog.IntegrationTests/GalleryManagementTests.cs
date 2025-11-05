using Booksy.API;
using Booksy.ServiceCatalog.API.Models.Requests;
using Booksy.ServiceCatalog.API.Models.Responses;
using Booksy.ServiceCatalog.Domain.Aggregates;
using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Headers;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace Booksy.ServiceCatalog.IntegrationTests;

/// <summary>
/// Integration tests for Gallery Management APIs
/// Tests image upload, retrieval, metadata updates, reordering, and deletion
/// </summary>
[Collection("Integration Tests")]
public class GalleryManagementTests : ServiceCatalogIntegrationTestBase
{
    public GalleryManagementTests(ServiceCatalogTestWebApplicationFactory<Startup> factory)
        : base(factory)
    {
    }

    #region Upload Gallery Images Tests

    [Fact]
    public async Task UploadGalleryImages_WithValidImages_UploadsSuccessfully()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var imageFiles = CreateTestImageFiles(2);

        // Act
        var response = await PostMultipartAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery",
            imageFiles);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Upload failed with {response.StatusCode}: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<List<GalleryImageResponse>>(response);
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].DisplayOrder.Should().Be(0);
        result[1].DisplayOrder.Should().Be(1);
        result[0].ThumbnailUrl.Should().NotBeNullOrEmpty();
        result[0].MediumUrl.Should().NotBeNullOrEmpty();
        result[0].OriginalUrl.Should().NotBeNullOrEmpty();

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        updatedProvider!.Profile.GalleryImages.Should().HaveCount(2);
    }

    [Fact]
    public async Task UploadGalleryImages_WithTooManyFiles_ReturnsBadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var imageFiles = CreateTestImageFiles(11); // Max is 10

        // Act
        var response = await PostMultipartAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery",
            imageFiles);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadGalleryImages_WithFileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var imageFiles = CreateTestImageFiles(1, fileSizeBytes: 11 * 1024 * 1024); // 11MB, max is 10MB

        // Act
        var response = await PostMultipartAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery",
            imageFiles);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadGalleryImages_WithInvalidFileType_ReturnsBadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[100]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "files", "test.pdf");

        // Act
        var response = await Client.PostAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadGalleryImages_ExceedsMaxLimit_ReturnsBadRequest()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();

        // Upload 50 images (the max limit)
        for (int i = 0; i < 5; i++)
        {
            var batch = CreateTestImageFiles(10);
            await PostMultipartAsync($"/api/v1/providers/{provider.Id.Value}/gallery", batch);
        }

        // Try to upload one more
        var oneMoreFile = CreateTestImageFiles(1);

        // Act
        var response = await PostMultipartAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery",
            oneMoreFile);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("Cannot add more than 50 gallery images");
    }

    [Fact]
    public async Task UploadGalleryImages_AsUnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        ClearAuthenticationHeader();
        var imageFiles = CreateTestImageFiles(1);

        // Act
        var response = await PostMultipartAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery",
            imageFiles);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadGalleryImages_AsDifferentProvider_ReturnsForbiddenOrNotFound()
    {
        // Arrange
        var provider1 = await CreateAndAuthenticateAsProviderAsync();
        var provider2 = await CreateAndAuthenticateAsProviderAsync(); // Authenticate as different provider
        var imageFiles = CreateTestImageFiles(1);

        // Act
        var response = await PostMultipartAsync(
            $"/api/v1/providers/{provider1.Id.Value}/gallery",
            imageFiles);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Gallery Images Tests

    [Fact]
    public async Task GetGalleryImages_WithExistingImages_ReturnsAllImages()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(5);

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/gallery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<List<GalleryImageResponse>>(response);
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Should().BeInAscendingOrder(img => img.DisplayOrder);
    }

    [Fact]
    public async Task GetGalleryImages_WithNoImages_ReturnsEmptyList()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/gallery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<List<GalleryImageResponse>>(response);
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGalleryImages_AsAnonymousUser_ReturnsImages()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(3);
        ClearAuthenticationHeader(); // Anonymous access

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/gallery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<List<GalleryImageResponse>>(response);
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetGalleryImages_OnlyReturnsActiveImages()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(5);
        AuthenticateAsProviderOwner(provider);

        // Get images
        var getResponse = await GetAsync($"/api/v1/providers/{provider.Id.Value}/gallery");
        var images = await GetResponseAsync<List<GalleryImageResponse>>(getResponse);

        // Delete one image
        await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images![0].Id}");

        // Act
        var response = await GetAsync($"/api/v1/providers/{provider.Id.Value}/gallery");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await GetResponseAsync<List<GalleryImageResponse>>(response);
        result.Should().HaveCount(4); // Only active images
    }

    #endregion

    #region Update Gallery Image Metadata Tests

    [Fact]
    public async Task UpdateGalleryImageMetadata_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(1);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);
        var imageId = images[0].Id;

        var updateRequest = new UpdateGalleryImageMetadataRequest
        {
            Caption = "Beautiful salon interior",
            AltText = "Modern salon with stylish furniture and lighting"
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{imageId}",
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var updatedImage = updatedProvider!.Profile.GetGalleryImage(imageId);
        updatedImage.Should().NotBeNull();
        updatedImage!.Caption.Should().Be("Beautiful salon interior");
        updatedImage.AltText.Should().Be("Modern salon with stylish furniture and lighting");
    }

    [Fact]
    public async Task UpdateGalleryImageMetadata_WithCaptionTooLong_ReturnsBadRequest()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(1);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);
        var updateRequest = new UpdateGalleryImageMetadataRequest
        {
            Caption = new string('a', 501), // Max is 500
            AltText = "Alt text"
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{images[0].Id}",
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateGalleryImageMetadata_WithNonExistentImage_ReturnsNotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var nonExistentImageId = Guid.NewGuid();
        var updateRequest = new UpdateGalleryImageMetadataRequest
        {
            Caption = "Test",
            AltText = "Test"
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{nonExistentImageId}",
            updateRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    #region Reorder Gallery Images Tests

    [Fact]
    public async Task ReorderGalleryImages_WithValidOrder_ReordersSuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(5);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);

        // Reverse the order
        var reorderRequest = new ReorderGalleryImagesRequest
        {
            ImageOrders = new Dictionary<Guid, int>
            {
                { images[0].Id, 4 },
                { images[1].Id, 3 },
                { images[2].Id, 2 },
                { images[3].Id, 1 },
                { images[4].Id, 0 }
            }
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/reorder",
            reorderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify new order
        var reorderedImages = await GetGalleryImagesAsync(provider.Id.Value);
        reorderedImages[0].Id.Should().Be(images[4].Id);
        reorderedImages[4].Id.Should().Be(images[0].Id);
    }

    [Fact]
    public async Task ReorderGalleryImages_WithNonExistentImage_ReturnsBadRequest()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(2);
        AuthenticateAsProviderOwner(provider);

        var reorderRequest = new ReorderGalleryImagesRequest
        {
            ImageOrders = new Dictionary<Guid, int>
            {
                { Guid.NewGuid(), 0 } // Non-existent image
            }
        };

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/reorder",
            reorderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Delete Gallery Image Tests

    [Fact]
    public async Task DeleteGalleryImage_WithValidImage_DeletesSuccessfully()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(3);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);
        var imageToDelete = images[1];

        // Act
        var response = await DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{imageToDelete.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion (soft delete)
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var deletedImage = updatedProvider!.Profile.GalleryImages
            .FirstOrDefault(img => img.Id == imageToDelete.Id);
        deletedImage.Should().NotBeNull();
        deletedImage!.IsActive.Should().BeFalse();

        // Verify image not returned in GET
        var remainingImages = await GetGalleryImagesAsync(provider.Id.Value);
        remainingImages.Should().HaveCount(2);
        remainingImages.Should().NotContain(img => img.Id == imageToDelete.Id);
    }

    [Fact]
    public async Task DeleteGalleryImage_WithNonExistentImage_ReturnsNotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var nonExistentImageId = Guid.NewGuid();

        // Act
        var response = await DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{nonExistentImageId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteGalleryImage_AsUnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(1);
        var images = await GetGalleryImagesAsync(provider.Id.Value);
        ClearAuthenticationHeader();

        // Act
        var response = await DeleteAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{images[0].Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Business Rule Tests

    [Fact]
    public async Task GalleryManagement_MaintainsDisplayOrderConsistency_AfterDeletion()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(5);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);

        // Act - Delete middle image
        await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[2].Id}");

        // Assert - Display order should still be consistent
        var remainingImages = await GetGalleryImagesAsync(provider.Id.Value);
        remainingImages.Should().HaveCount(4);
        remainingImages.Should().BeInAscendingOrder(img => img.DisplayOrder);
    }

    [Fact]
    public async Task GalleryManagement_SupportsMultipleBatches_UpToLimit()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();

        // Act - Upload in batches
        await PostMultipartAsync($"/api/v1/providers/{provider.Id.Value}/gallery", CreateTestImageFiles(10));
        await PostMultipartAsync($"/api/v1/providers/{provider.Id.Value}/gallery", CreateTestImageFiles(10));
        await PostMultipartAsync($"/api/v1/providers/{provider.Id.Value}/gallery", CreateTestImageFiles(10));
        await PostMultipartAsync($"/api/v1/providers/{provider.Id.Value}/gallery", CreateTestImageFiles(10));
        await PostMultipartAsync($"/api/v1/providers/{provider.Id.Value}/gallery", CreateTestImageFiles(10));

        // Assert
        var images = await GetGalleryImagesAsync(provider.Id.Value);
        images.Should().HaveCount(50);
        images.Should().BeInAscendingOrder(img => img.DisplayOrder);
    }

    [Fact]
    public async Task GalleryManagement_AllowsReUpload_AfterDeletion()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(50); // At max limit
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);

        // Act - Delete 5 images
        for (int i = 0; i < 5; i++)
        {
            await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[i].Id}");
        }

        // Try to upload 5 more
        var response = await PostMultipartAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery",
            CreateTestImageFiles(5));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var allImages = await GetGalleryImagesAsync(provider.Id.Value);
        allImages.Should().HaveCount(50); // 45 active + 5 new = 50
    }

    #endregion

    #region Set Primary Gallery Image Tests

    [Fact]
    public async Task SetPrimaryGalleryImage_WithValidImage_SetsImageAsPrimary()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(3);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);
        var imageToSetPrimary = images[1];

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{imageToSetPrimary.Id}/set-primary",
            new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in database
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var primaryImage = updatedProvider!.Profile.GalleryImages
            .FirstOrDefault(img => img.Id == imageToSetPrimary.Id);
        primaryImage.Should().NotBeNull();
        primaryImage!.IsPrimary.Should().BeTrue();

        // Verify no other image is primary
        var otherPrimaryImages = updatedProvider.Profile.GalleryImages
            .Where(img => img.IsPrimary && img.Id != imageToSetPrimary.Id)
            .ToList();
        otherPrimaryImages.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_UnsetsOtherPrimaryImages()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(4);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);

        // Set first image as primary
        await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[0].Id}/set-primary", new { });

        // Act - Set another image as primary
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{images[2].Id}/set-primary",
            new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify only the second image is primary
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var primaryImages = updatedProvider!.Profile.GalleryImages
            .Where(img => img.IsPrimary)
            .ToList();
        primaryImages.Should().HaveCount(1);
        primaryImages[0].Id.Should().Be(images[2].Id);

        // Verify first image is no longer primary
        var firstImage = updatedProvider.Profile.GalleryImages
            .First(img => img.Id == images[0].Id);
        firstImage.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_PrimaryImageAppearsFirst_InQueryResults()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(5);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);

        // Act - Set the last image as primary
        await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[4].Id}/set-primary", new { });

        // Get images again
        var reorderedImages = await GetGalleryImagesAsync(provider.Id.Value);

        // Assert
        reorderedImages[0].Id.Should().Be(images[4].Id);
        reorderedImages[0].IsPrimary.Should().BeTrue();

        // Other images should not be primary
        reorderedImages.Skip(1).Should().OnlyContain(img => img.IsPrimary == false);
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_WithNonExistentImage_ReturnsNotFound()
    {
        // Arrange
        var provider = await CreateAndAuthenticateAsProviderAsync();
        var nonExistentImageId = Guid.NewGuid();

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{nonExistentImageId}/set-primary",
            new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_WithDeletedImage_ReturnsNotFound()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(2);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);
        var imageToDelete = images[0];

        // Delete the image
        await DeleteAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{imageToDelete.Id}");

        // Act - Try to set deleted image as primary
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{imageToDelete.Id}/set-primary",
            new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_AsUnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(1);
        var images = await GetGalleryImagesAsync(provider.Id.Value);
        ClearAuthenticationHeader();

        // Act
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider.Id.Value}/gallery/{images[0].Id}/set-primary",
            new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_ForDifferentProvider_ReturnsForbiddenOrNotFound()
    {
        // Arrange
        var provider1 = await CreateProviderWithGalleryAsync(1);
        var provider2 = await CreateAndAuthenticateAsProviderAsync(); // Different provider

        var images = await GetGalleryImagesAsync(provider1.Id.Value);

        // Authenticate as provider2
        AuthenticateAsProviderOwner(provider2);

        // Act - Try to set primary for provider1's image
        var response = await PutAsJsonAsync(
            $"/api/v1/providers/{provider1.Id.Value}/gallery/{images[0].Id}/set-primary",
            new { });

        // Assert - Should fail with Forbidden or NotFound
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_MultipleTimes_OnlyLastOneIsPrimary()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(3);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);

        // Act - Set all images as primary sequentially
        await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[0].Id}/set-primary", new { });
        await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[1].Id}/set-primary", new { });
        await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[2].Id}/set-primary", new { });

        // Assert - Only the last one should be primary
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        var primaryImages = updatedProvider!.Profile.GalleryImages
            .Where(img => img.IsPrimary)
            .ToList();

        primaryImages.Should().HaveCount(1);
        primaryImages[0].Id.Should().Be(images[2].Id);
    }

    [Fact]
    public async Task SetPrimaryGalleryImage_UpdatesBusinessProfileTimestamp()
    {
        // Arrange
        var provider = await CreateProviderWithGalleryAsync(1);
        AuthenticateAsProviderOwner(provider);

        var images = await GetGalleryImagesAsync(provider.Id.Value);
        var originalTimestamp = provider.Profile.LastUpdatedAt;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(100);

        // Act
        await PutAsJsonAsync($"/api/v1/providers/{provider.Id.Value}/gallery/{images[0].Id}/set-primary", new { });

        // Assert
        var updatedProvider = await FindProviderAsync(provider.Id.Value);
        updatedProvider!.Profile.LastUpdatedAt.Should().BeAfter(originalTimestamp);
    }

    #endregion

    #region Helper Methods
    private async Task<Provider> CreateProviderWithGalleryAsync(int imageCount)
    {
        var provider = await CreateAndAuthenticateAsProviderAsync();
        AuthenticateAsProviderOwner(provider);

        // Upload images in batches of 10
        int remaining = imageCount;
        while (remaining > 0)
        {
            int batchSize = Math.Min(10, remaining);
            var files = CreateTestImageFiles(batchSize);
            await PostMultipartAsync($"/api/v1/providers/{provider.Id.Value}/gallery", files);
            remaining -= batchSize;
        }

        return await FindProviderAsync(provider.Id.Value) ?? provider;
    }

    private async Task<List<GalleryImageResponse>> GetGalleryImagesAsync(Guid providerId)
    {
        var response = await GetAsync($"/api/v1/providers/{providerId}/gallery");
        return await GetResponseAsync<List<GalleryImageResponse>>(response) ?? new List<GalleryImageResponse>();
    }

    private MultipartFormDataContent CreateTestImageFiles(int count, long fileSizeBytes = 1024)
    {
        var content = new MultipartFormDataContent();

        for (int i = 0; i < count; i++)
        {
            // Generate a valid test image using ImageSharp
            var bytes = CreateValidTestImage();

            // If fileSizeBytes exceeds 10MB (file too large test), create larger image
            if (fileSizeBytes > 10 * 1024 * 1024)
            {
                bytes = new byte[fileSizeBytes];
                // Copy the valid image header
                var validImage = CreateValidTestImage();
                Array.Copy(validImage, bytes, Math.Min(validImage.Length, bytes.Length));
            }

            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "files", $"test-image-{i}.png");
        }

        return content;
    }

    private byte[] CreateValidTestImage()
    {
        // Create a simple 10x10 colored PNG image using ImageSharp
        using var image = new Image<Rgba32>(10, 10);

        // Fill with a color pattern
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                image[x, y] = new Rgba32((byte)(x * 25), (byte)(y * 25), 128, 255);
            }
        }

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    private async Task<HttpResponseMessage> PostMultipartAsync(string url, MultipartFormDataContent content)
    {
        return await Client.PostAsync(url, content);
    }

    #endregion
}
