Status: ACTIVE
Verify: FAST

User report (2026-09-20, customer app at customer.nahalkmi.ir, with screenshots). Nine points, all
on what a customer sees while choosing a salon.

## Findings (measured against production, salon «سالن نهال»)
- **Images**: `/Providers/{id}` and `/Providers/search` carry NO image field at all. The three
  photos exist and are served (`/Providers/{id}/gallery` -> thumbnail/medium/large URLs), so the
  app has nothing to show, not a display bug. The provider's chosen primary image is not marked in
  what the customer can read either.
- **Categories**: `/Categories` is a fixed list (6+) with no provider counts, so the app lists
  categories nobody offers.
- **Prices**: services return `currency: "USD"` — the seeded/default currency is wrong at the
  source, and the app prints it raw («USD ۱۵۰۰۰۰۰»), unseparated.
- **Rating**: `averageRating`/`totalReviews` ARE in both payloads (0 today). Reviews have their own
  controller: GET `/Reviews/providers/{providerId}`, POST `/Reviews/bookings/{bookingId}` — the app
  uses neither.
- **Free slots**: nothing exposes "how many free times today"; `/Bookings/available-slots` answers
  one service and one day at a time.
- **Address**: detail carries `formattedAddress`, city, state and lat/long; the search item carries
  only city/state, and the app shows just the city.
- **Working hours**: `businessHours` is a per-day list with `breaks` (empty here), so grouping and
  break display are the app's job.
- **Search**: the map screen takes free text; the geocoding search endpoint the provider app uses
  (`/Geocoding/search`) can back an autocomplete.

## Tasks
- [x] 1.1 Prices in Toman everywhere, grouped in threes, never "USD" (source default + app)
- [x] 1.2 The provider's photos reach the customer: primary image on cards/profile, gallery as a
      slider on the profile
- [x] 1.3 Categories with no providers are not offered
- [x] 1.4 Profile shows the street address, not only the city, and a map that opens a navigation app
- [x] 1.5 Working hours grouped: identical days on one row, the odd day on its own, breaks shown
- [x] 1.6 Provider card shows rating and how many free times it has
- [x] 1.7 Ratings and comments: read them on the profile, leave one after a visit
- [x] 1.8 Search suggests cities/villages/provinces as you type
- [ ] 1.9 Verify, deploy, confirm live

## Log
- 2026-09-20 1.1 Toman everywhere: PlatformCurrency.Code ("IRT") replaces the "USD"/"IRR" literals
  on the catalogue paths (service creation, provider settings, statistics, specifications);
  migration RelabelUsdPricesAsToman relabels existing USD rows in every ServiceCatalog currency
  column and touches no amount; rows already marked IRR are left alone because the payment
  gateways settle in Rial (FOLLOW-UPS #67). App: the four screens that interpolated the raw API
  currency now use PriceFormatter («۱٬۵۰۰٬۰۰۰ تومان»), with a source-scan test that bans the
  bypass. 1 integration + 2 app tests.
- 1.2 BusinessProfile.DisplayImageUrl (chosen gallery photo, else first, else logo/profile image)
  feeds every customer-facing projection — search, map, featured, by-status, detail — so the cards
  stop showing a placeholder while the salon's photos sit on the server; the detail response
  carries the whole gallery, chosen first, for the profile slider. 2 integration tests.
- 2026-09-20 The photos still did not appear after 1.2 shipped, and the reason was in the app, not
  the data: every salon photo went through `cached_network_image`, whose cache manager stores files
  through dart:io — which a browser build does not have — so each one fell through to the
  placeholder while the URLs were served correctly (verified anonymously against production:
  search returns logoUrl, /uploads answers 200 with the right CORS headers). ProviderImage now uses
  the browser's own Image.network on web and keeps the cached loader elsewhere; the home promo strip
  and the search result card were rendering their own copies and now share it.
- 2026-09-20 1.3 The home category row now takes the catalogue's counts and drops what no salon
  offers (the API already answered with one category; the row was a hard-coded list of six). With
  nothing known yet — still loading, or the call failed — the full row shows, because an empty strip
  reads as "no categories". 3 widget tests.
- 1.4 The profile parsed `address.street`, which this API never sends, so every salon showed its
  city and nothing else; it now reads `formattedAddress` plus the coordinates, and a location card
  puts the salon on a small map with one tap to hand the point to نشان / بلد / گوگل مپ. 3 tests.
- 1.5 Working hours: days that keep the same hours share a row, neighbouring ones reading as
  «شنبه، دوشنبه تا جمعه», and mid-day breaks are parsed and shown under their row. 4 grouping tests.
- (also) The profile header is a swipeable gallery of the salon's photos, chosen photo first,
  falling back to the single hero image when there is only one. 3 tests.
- 2026-09-20 1.6 GET /providers/availability-summary answers a whole results page in one request:
  per salon, the first day with free times, how many, and the earliest — computed on the salon's
  SHORTEST active service, cached two minutes, capped at 20 salons, rate-limited, and a salon that
  cannot be booked reports zero instead of failing the list. 4 integration tests. The card shows
  «امروز ۵ وقت خالی»; the rating was already wired and hides itself until someone rates.
  (Peer booking-aa flagged the 20x7 sequential-computation risk — hence the cache and the cap.)
- 1.7 Reviews: read them on the profile (average, what people wrote, the salon's reply) and leave
  one from a finished appointment. The app's review URLs were both wrong — `/Reviews/provider/{id}`
  (singular) 404s and the POST route was `/Reviews` — which is why no review ever loaded or saved.
  4 widget tests.
- 1.8 Typing a city, village or province suggests places, debounced, and picking one moves the map
  without a second lookup. The lookup now goes through our own /Geocoding/search: the browser
  cannot set the User-Agent Nominatim's policy requires, which is what broke the provider app's
  direct calls in production. 3 tests.
- 2026-09-20 (user, same session) My-location put the customer in Dubai while they stood in
  پارس‌آباد: a browser behind a VPN has no GPS or Wi-Fi data, so it falls back to the IP address
  and reports a radius of tens of kilometres. A fix coarser than 20 km is now refused — the map
  stays where it is and says the VPN may be the reason. The hamburger menu is gone: every
  destination it held is on the bottom bar.
