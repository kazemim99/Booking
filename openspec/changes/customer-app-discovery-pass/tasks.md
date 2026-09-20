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
- [ ] 1.3 Categories with no providers are not offered
- [ ] 1.4 Profile shows the street address, not only the city, and a map that opens a navigation app
- [ ] 1.5 Working hours grouped: identical days on one row, the odd day on its own, breaks shown
- [ ] 1.6 Provider card shows rating and how many free times it has
- [ ] 1.7 Ratings and comments: read them on the profile, leave one after a visit
- [ ] 1.8 Search suggests cities/villages/provinces as you type
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
