Status: DONE
Verify: FAST

User report (2026-09-19), on the live admin panel, about the provider «سالن نهال» (09123135143):
dates render in the wrong direction, the phone shows as +98…, the provider's services show as zero
though it entered several, and none of its 3 gallery photos load.

## Findings (measured on production with the admin token)
- Services: GET /Services/provider/{id} returns 8 Active services, while GET /Providers/{id} and
  /Providers/by-status/{status} return serviceCount 0 and services []. A backend query defect.
- Photos: the 3 files exist in the API's uploads volume, but https://back.nahalkmi.ir/uploads/... is
  answered by the Vue web container's SPA fallback (text/html): its nginx forwards /api/ only. So no
  uploaded image loads anywhere — provider app included. The API also builds the URLs from the
  request scheme, which behind TLS termination is http://, and an HTTPS page blocks those.
- Phone: stored E.164 (+989123135143); the admin shows it raw.

## Tasks
- [x] 1.1 Web container nginx proxies /uploads/ to the API
- [x] 1.2 Absolute media URLs use a configured public base (App:PublicBaseUrl = https://back.nahalkmi.ir)
      instead of the proxied request's scheme; unit test
- [x] 1.3 Provider list/detail report the provider's real services; test
- [x] 1.4 Admin: phone shown in the local format (0912 313 5143); dates rendered left-to-right inside
      the RTL layout; tests
- [x] 1.5 Verify, deploy, confirm on the live site with the admin token

## Log
- 2026-09-19 1.1 web container nginx: `location ^~ /uploads/` -> booksy-api (^~ beats the image
  regex for .png/.jpg); nginx -t passes in nginx:alpine. The API itself served the file (200
  image/webp) from inside the box — only the route was missing.
- 1.2 UrlService prefers App:PublicBaseUrl (compose: https://back.nahalkmi.ir); 4 unit tests.
- 1.3 GetProviderById / GetProvidersByStatus / GetProviderByOwnerId counted provider.Services, a
  stale always-empty collection; now CountByProviderAsync on the services table. 2 integration
  tests reproduced 0 before the fix.
- 1.4 formatPhone (+98/0098/98/0 -> «0912 313 5143», LTR-isolated; 5 tests) in provider details and
  the users table; dates first-strong isolated (1 test). 54 admin tests pass; verify FAST PASS.
- 2026-09-19 Deployed (e899b0aa). Live with the admin token: Salon Nahal serviceCount 8; gallery URLs
  are https://back.nahalkmi.ir/uploads/... and fetch as 200 image/webp.
- 2026-09-19 Provider booking failure (same session): the app sent the slot as .toUtc(), 09:00 Tehran ->
  05:30Z, and the server refused it ("زمان رزرو باید بین 09:00 و 18:00 باشد"); the listed value
  unchanged booked fine (test booking cancelled at once). Fixed client-side (wallClockIso, 2 tests);
  failures now show the server's reason (3 tests). Timezone model recorded as FOLLOW-UPS #63.
- 2026-09-19 verify FULL PASS (17 steps, 396 s) on 0e79e227.
