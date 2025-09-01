
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booksy.Infrastructure.External.Storage
{
    /// <summary>
    /// Azure Blob Storage implementation
    /// </summary>
    public class AzureBlobStorage : IBlobStorage
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AzureStorageSettings _settings;
        private readonly ILogger<AzureBlobStorage> _logger;

        public AzureBlobStorage(
            IOptions<AzureStorageSettings> settings,
            ILogger<AzureBlobStorage> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
        }

        public async Task<string> UploadAsync(
            string containerName,
            string fileName,
            Stream fileStream,
            string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

                var blobClient = containerClient.GetBlobClient(fileName);

                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType ?? "application/octet-stream"
                    }
                };

                await blobClient.UploadAsync(fileStream, options, cancellationToken);

                _logger.LogInformation("File {FileName} uploaded to container {ContainerName}", fileName, containerName);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} to container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        public async Task<Stream> DownloadAsync(
            string containerName,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

                return response.Value.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file {FileName} from container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        public async Task DeleteAsync(
            string containerName,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

                _logger.LogInformation("File {FileName} deleted from container {ContainerName}", fileName, containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FileName} from container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(
            string containerName,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                var response = await blobClient.ExistsAsync(cancellationToken);

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check existence of file {FileName} in container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        public async Task<string> GetUrlAsync(
            string containerName,
            string fileName,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                if (expiry.HasValue && blobClient.CanGenerateSasUri)
                {
                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = containerName,
                        BlobName = fileName,
                        Resource = "b",
                        ExpiresOn = DateTimeOffset.UtcNow.Add(expiry.Value)
                    };

                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    return blobClient.GenerateSasUri(sasBuilder).ToString();
                }

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get URL for file {FileName} in container {ContainerName}", fileName, containerName);
                throw;
            }
        }

        public async Task<IEnumerable<string>> ListFilesAsync(
            string containerName,
            string? prefix = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var files = new List<string>();

                await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
                {
                    files.Add(blobItem.Name);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files in container {ContainerName}", containerName);
                throw;
            }
        }
    }
}

// ========================================
// Booksy.Infrastructure.External.csproj
// ========================================
/*
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

 

  <ItemGroup>
    <ProjectReference Include="..\Booksy.Infrastructure.Core\Booksy.Infrastructure.Core.csproj" />
  </ItemGroup>

</Project>
*/