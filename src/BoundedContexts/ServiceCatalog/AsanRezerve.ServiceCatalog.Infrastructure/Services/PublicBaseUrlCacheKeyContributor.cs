using AsanRezerve.Core.Application.Abstractions.Caching;
using AsanRezerve.ServiceCatalog.Application.Abstractions;

namespace AsanRezerve.ServiceCatalog.Infrastructure.Services;

/// <summary>
/// Cached salon reads carry absolute photo URLs built from <see cref="IUrlService.GetBaseUrl"/>. Production
/// configures that base (<c>App:PublicBaseUrl</c>), so this is one constant; without it (development) the base is
/// the request's host, and a page cached for a phone on the LAN must not be served to a browser on localhost.
/// </summary>
public sealed class PublicBaseUrlCacheKeyContributor(IUrlService urls) : ICacheKeyContributor
{
    public string? Contribute() => urls.GetBaseUrl();
}
