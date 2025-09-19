using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Infrastructure.Core.External
{
    public interface IBlobStorage
{
    Task<string> UploadAsync(
        string containerName,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string containerName,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<string> GetUrlAsync(
        string containerName,
        string fileName,
        TimeSpan? expiry = null);
}


}
