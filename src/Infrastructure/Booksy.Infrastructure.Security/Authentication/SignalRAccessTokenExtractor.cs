using Microsoft.AspNetCore.Http;

namespace Booksy.Infrastructure.Security.Authentication
{
    /// <summary>
    /// Extracts the JWT for a SignalR hub connection during JWT-bearer negotiation.
    /// <para>
    /// Browsers cannot set custom headers on a WebSocket handshake, so the SignalR client appends the token as a
    /// <c>?access_token=...</c> <b>query-string</b> parameter. We therefore read it from the query string for any
    /// <c>/hubs</c> path (with a header fallback for non-browser clients). For non-hub paths we return null so the
    /// normal <c>Authorization: Bearer</c> header path is used and the token isn't sourced from an untrusted query.
    /// </para>
    /// </summary>
    public static class SignalRAccessTokenExtractor
    {
        public static string? Extract(IQueryCollection query, IHeaderDictionary headers, PathString path)
        {
            if (!path.StartsWithSegments("/hubs"))
                return null;

            var fromQuery = query["access_token"].ToString();
            if (!string.IsNullOrEmpty(fromQuery))
                return fromQuery;

            var fromHeader = headers["access_token"].ToString();
            return string.IsNullOrEmpty(fromHeader) ? null : fromHeader;
        }
    }
}
