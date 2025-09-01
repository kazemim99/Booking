namespace Booksy.Infrastructure.External.Storage
{

    /// <summary>
    /// Blob storage interface
    /// </summary>
    public interface IBlobStorage
    {
        /// <summary>
        /// Uploads a file
        /// </summary>
        Task<string> UploadAsync(string containerName, string fileName, Stream fileStream, string? contentType = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file
        /// </summary>
        Task<Stream> DownloadAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file
        /// </summary>
        Task DeleteAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        Task<bool> ExistsAsync(string containerName, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a file URL
        /// </summary>
        Task<string> GetUrlAsync(string containerName, string fileName, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists files in a container
        /// </summary>
        Task<IEnumerable<string>> ListFilesAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);
    }
}