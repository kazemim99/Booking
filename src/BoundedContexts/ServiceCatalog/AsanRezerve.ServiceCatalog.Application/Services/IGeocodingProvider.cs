namespace AsanRezerve.ServiceCatalog.Application.Services;

/// <summary>
/// Place lookup for the onboarding map picker, performed by the SERVER rather than the browser.
///
/// <para>Clients used to call the geocoder directly; that broke wherever a user's network could not
/// reach it, and made every visitor an unidentified client of a shared free service. Going through
/// the API gives one identified caller and one cache.</para>
///
/// <para>Best effort by contract: an unreachable or failing upstream yields <c>null</c>, never an
/// exception, so a map tap can never break the form the user is filling in.</para>
/// </summary>
public interface IGeocodingProvider
{
    /// <summary>Place name to candidates, as the upstream's raw JSON, or null.</summary>
    Task<string?> SearchAsync(string query, int limit, CancellationToken cancellationToken);

    /// <summary>Coordinates to an address, as the upstream's raw JSON, or null.</summary>
    Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken);
}
