using Booksy.ServiceCatalog.Domain.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using Booksy.ServiceCatalog.Domain.Exceptions;
using Booksy.Core.Domain.Exceptions;

namespace Booksy.ServiceCatalog.Infrastructure.Services.Images;

/// <summary>
/// Image optimization service using ImageSharp library
/// </summary>
public class ImageSharpOptimizationService : IImageOptimizationService
{
    public async Task<OptimizedImage> OptimizeAsync(
        Stream sourceStream,
        ImageOptimizationOptions options,
        CancellationToken cancellationToken = default)
    {
      
            using var image = await Image.LoadAsync(sourceStream, cancellationToken);

            // Generate thumbnail (square crop)
            var thumbnail = await GenerateThumbnailAsync(image, options.ThumbnailSize, options.Quality, cancellationToken);

            // Generate medium (preserve aspect ratio)
            var medium = await GenerateResizedAsync(image, options.MediumSize, options.Quality, cancellationToken);

            // Generate original (max size, preserve aspect ratio)
            var original = await GenerateResizedAsync(image, options.OriginalMaxSize, options.Quality, cancellationToken);

            return new OptimizedImage(thumbnail, medium, original);
     

    }

    private async Task<byte[]> GenerateThumbnailAsync(
        Image image,
        int size,
        int quality,
        CancellationToken cancellationToken)
    {
        using var thumbnail = image.Clone(ctx =>
        {
            // Resize and crop to square
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            });
        });

        return await SaveAsWebPAsync(thumbnail, quality, cancellationToken);
    }

    private async Task<byte[]> GenerateResizedAsync(
        Image image,
        int maxSize,
        int quality,
        CancellationToken cancellationToken)
    {
        // Only resize if image is larger than max size
        var shouldResize = image.Width > maxSize || image.Height > maxSize;

        if (!shouldResize)
        {
            return await SaveAsWebPAsync(image, quality, cancellationToken);
        }

        using var resized = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(maxSize, maxSize),
                Mode = ResizeMode.Max // Preserve aspect ratio
            });
        });

        return await SaveAsWebPAsync(resized, quality, cancellationToken);
    }

    private async Task<byte[]> SaveAsWebPAsync(
        Image image,
        int quality,
        CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();

        var encoder = new WebpEncoder
        {
            Quality = quality,
            Method = WebpEncodingMethod.BestQuality
        };

        await image.SaveAsync(ms, encoder, cancellationToken);
        return ms.ToArray();
    }
}
