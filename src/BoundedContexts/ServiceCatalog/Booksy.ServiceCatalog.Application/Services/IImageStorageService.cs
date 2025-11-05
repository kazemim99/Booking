using Microsoft.AspNetCore.Http;

namespace Booksy.ServiceCatalog.Application.Services;

public interface IImageStorageService
{
    Task<string> SaveProfileImageAsync(Guid providerId, IFormFile image);
    Task<string> SaveBusinessLogoAsync(Guid providerId, IFormFile image);
    Task DeleteImageAsync(string imageUrl);
    bool IsValidImageType(IFormFile file);
}
