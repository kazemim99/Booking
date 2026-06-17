using Booksy.ServiceCatalog.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Booksy.ServiceCatalog.Infrastructure.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly string _uploadPath;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedMimeTypes = {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    public ImageStorageService(IConfiguration configuration)
    {
        var webRootPath = configuration.GetValue<string>("WebRootPath")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

        _uploadPath = Path.Combine(webRootPath, "uploads", "providers");
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string> SaveProfileImageAsync(Guid providerId, IFormFile image)
    {
        if (!IsValidImageType(image))
        {
            throw new ArgumentException("Invalid image format");
        }

        var providerFolder = Path.Combine(_uploadPath, providerId.ToString());
        Directory.CreateDirectory(providerFolder);

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var fileName = $"profile_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(providerFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        // Return relative URL
        return $"/uploads/providers/{providerId}/{fileName}";
    }

    public async Task<string> SaveBusinessLogoAsync(Guid providerId, IFormFile image)
    {
        if (!IsValidImageType(image))
        {
            throw new ArgumentException("Invalid image format");
        }

        var providerFolder = Path.Combine(_uploadPath, providerId.ToString());
        Directory.CreateDirectory(providerFolder);

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var fileName = $"logo_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(providerFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }

        // Return relative URL
        return $"/uploads/providers/{providerId}/{fileName}";
    }

    public Task DeleteImageAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return Task.CompletedTask;
        }

        var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var filePath = Path.Combine(webRootPath, imageUrl.TrimStart('/'));

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public bool IsValidImageType(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return false;
        }

        // Check extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return false;
        }

        // Check MIME type
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return false;
        }

        return true;
    }

    public async Task<string?> MoveImageToProviderAsync(string currentImageUrl, Guid newProviderId)
    {
        if (string.IsNullOrWhiteSpace(currentImageUrl))
        {
            return null;
        }

        // Extract the file path from the URL
        var currentFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", currentImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(currentFilePath))
        {
            // File doesn't exist, return the original URL (it might already be in the correct location)
            return currentImageUrl;
        }

        // Extract the filename from the current path
        var fileName = Path.GetFileName(currentFilePath);

        // Create the new provider folder
        var newProviderFolder = Path.Combine(_uploadPath, newProviderId.ToString());
        Directory.CreateDirectory(newProviderFolder);

        // Build the new file path
        var newFilePath = Path.Combine(newProviderFolder, fileName);

        // If the file is already in the correct location, just return the URL
        if (currentFilePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return currentImageUrl;
        }

        // Copy the file to the new location (don't delete the old one in case of errors)
        using (var sourceStream = new FileStream(currentFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var destinationStream = new FileStream(newFilePath, FileMode.Create, FileAccess.Write))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }

        // Delete the old file and folder if they're different
        try
        {
            File.Delete(currentFilePath);

            // Try to delete the old provider folder if it's empty
            var oldProviderFolder = Path.GetDirectoryName(currentFilePath);
            if (oldProviderFolder != null && Directory.Exists(oldProviderFolder))
            {
                if (!Directory.EnumerateFileSystemEntries(oldProviderFolder).Any())
                {
                    Directory.Delete(oldProviderFolder);
                }
            }
        }
        catch
        {
            // Ignore errors when deleting old files - the new file is already in place
        }

        // Return the new URL
        return $"/uploads/providers/{newProviderId}/{fileName}";
    }
}
