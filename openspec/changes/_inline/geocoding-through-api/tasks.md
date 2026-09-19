Status: ACTIVE
Verify: FAST

The provider app's map search calls `nominatim.openstreetmap.org` straight from the browser. It fails
for the user on their network (2026-09-19, «پارس آباد, اردبیل» search), while the same request returns
200 from this workstation and from the production server (0.3-0.9 s). Every visitor's browser is also
an unidentified Nominatim client, which its usage policy does not allow at scale and which no cache
protects.

Fix: the app asks OUR API, the server asks Nominatim. One identified caller, one cache, and the
feature stops depending on each visitor's network being able to reach OSM.

## Acceptance scenarios
- G1 Given a browser that cannot reach nominatim.openstreetmap.org, when the user picks a city or taps
  the map, then the address/coordinates still resolve (the request goes to back.nahalkmi.ir).
- G2 Given two identical lookups, when the second is made, then it is served from cache — the upstream
  sees one request.
- G3 Given Nominatim is unreachable or slow, then the endpoint fails fast and the app keeps whatever
  the user typed (existing best-effort behaviour, no error dialog).
- G4 The server identifies itself to Nominatim with a contact User-Agent on every upstream call.
- G5 The endpoints are anonymous (onboarding uses them before a provider exists) and rate-limited per
  caller so one client cannot spend the shared Nominatim budget.

## Tasks
- [x] 1.1 IGeocodingProvider (Application) + Nominatim client (Infrastructure): UA, timeout, cache;
      unit tests with a fake HttpMessageHandler (G2, G3, G4)
- [x] 1.2 GeocodingController: GET search + reverse, anonymous, rate-limited (G1, G5)
- [x] 1.3 Provider app: GeocodingService calls the API instead of nominatim.org; update its tests
- [ ] 1.4 Verify FAST, deploy, confirm on the live site from a browser

## Decisions
- Pass Nominatim's JSON through rather than reshaping it: the app already parses that shape
  (`formatAddress`, `shortenAddress`), so the client change stays a base-URL change and the address
  formatting keeps its existing tests.

## Log
- 2026-09-19 Measured: browser (user) fails; workstation 200 + `access-control-allow-origin: *`;
  production server 200 in 0.33-0.88 s. Tiles (tile.openstreetmap.org) work for the user, so this is
  specific to the Nominatim host, not OSM in general.
- 2026-09-19 Implemented. IGeocodingProvider + NominatimGeocodingProvider (UA, 10s timeout, 24h
  memory cache, never throws; 7 unit tests with a fake handler, incl. cache-hit and failure paths),
  GET /api/v1/Geocoding/search|reverse (anonymous, public-api rate limit, 503 when unavailable),
  app GeocodingService switched to those endpoints on the unauthenticated app-base-URL client.
  Response passed through unchanged, so formatAddress/shortenAddress tests were untouched.
  475 app tests pass; verify FAST PASS.
