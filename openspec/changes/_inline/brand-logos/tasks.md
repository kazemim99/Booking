Status: DONE
Verify: FULL

2026-09-26: the designer delivered the AsanRezerve logo set (9 SVGs: two app icons, the symbol mark in colour /
mono / reversed variants, Persian and Latin horizontal lockups). The user asked that each piece go where it fits
(splash, app icon, …) and that **the default Flutter icon is no longer used anywhere**.

## Findings

- Both Flutter apps still ship Flutter's default launcher icon on every platform: Android `mipmap-*/ic_launcher.png`,
  web `favicon.png` + `icons/Icon-*.png`, provider iOS `AppIcon.appiconset`, customer Windows `app_icon.ico`.
  Their native launch screens are plain white, the provider iOS `LaunchImage` is Flutter's transparent placeholder,
  and the Android labels are the package names (`asan_rezerve_customer_app`).
- The in-app splash pages show a Material icon (`calendar_today_rounded`, `storefront_rounded`), not the brand.
- The web app's favicon is Vue's default, its `<title>` is "Vite App", and `SimpleHeader` shows a twemoji clip-art
  `logo.svg`. The admin app uses `vite.svg` as its favicon.
- The brand colours in the SVGs are already the apps' palette (`#3777C0` app bar, `#0AC075` success, `#0F1724` ink).

## Mapping

| Asset | Used for |
|---|---|
| `appicon-blue` | Customer app launcher icon: Android (legacy + adaptive + Android 13 themed), web PWA, Windows |
| `appicon-white` | Provider app launcher icon, so a salon owner who has both apps can tell them apart |
| `symbol-reversed-blue` | Full-bleed square for the customer iOS-style and maskable PWA icons (the OS applies the mask) |
| `symbol-color` | Native splash (Android/iOS) on white, in-app Flutter splash, web favicons, web/admin logo marks |
| `symbol-reversed-dark` | Android native splash in dark mode |
| `symbol-mono-black` / `-blue` | Android 13 themed-icon monochrome layer shape; otherwise kept in `branding/` for print/one-colour use |
| `horizontal-persian-color` / `-latin-color` | Web header and admin sidebar lockups (drawn inline with the app's own Vazir font, not `<img>`: the SVG's `@import` of Google Fonts does not load inside `<img>`) |

## Tasks

- [x] 1 Commit the source SVGs to `branding/` with a README that states the mapping above.
- [x] 2 Rasterise the Flutter launcher icons (Android mipmaps + adaptive/monochrome, provider iOS AppIcon, web
  favicon/PWA icons, customer Windows ico) and native launch screens (Android light/dark, provider iOS
  LaunchImage). Replace the package-name labels and the provider's default web manifest text.
- [x] 3 Flutter in-app splash: a painted `BrandMark` widget (no new dependency) replaces the Material icon, with a
  widget test in each app.
- [x] 4 Web: favicon (SVG + ico + apple-touch-icon), `<title>`, a `BrandLogo` component in the headers and login
  views; admin: favicon, title, sidebar logo.
- [x] 5 Frontend and admin type-check, lint, unit tests; Flutter analyze/test; FULL verify.

## Log

- 2026-09-26 Environment had neither Docker running nor a .NET SDK installed; started `dockerd` (works in this
  container) and installed `dotnet-sdk-10.0` via apt (no net9.0 SDK package exists; no `global.json` pins a
  version, and `DOTNET_ROLL_FORWARD=Major` lets the net9.0 test hosts run on it — confirmed with one project
  before trusting it for the full run). `dotnet build AsanRezerve.sln`: 0 errors (355 pre-existing warnings). All
  ten FAST unit/architecture projects green (1663 tests). Flutter: both apps' `flutter analyze` clean, `flutter
  test` green (customer 857/857, provider 757/757, including the new `brand_mark_test.dart` in each). Web:
  frontend `vue-tsc` clean, lint 0 errors (283 pre-existing warnings, unchanged), unit 215/215 (verify.sh's
  scoped run); admin `vue-tsc` clean, unit 148/148.
- 2026-09-26 `scripts/verify.sh full`'s DB step failed first: this container's egress proxy 403s
  `builds.dotnet.microsoft.com` and Docker Hub's anonymous-pull rate limit (429) blocks Testcontainers from
  resolving `postgres:16-alpine` even though that image was already cached locally (`docker run` off the cache
  works; `docker pull`'s manifest check does not). Used the fixture's own escape hatch
  (`PostgresTestContainerFixture.ExternalServerVariable`, `ASANREZERVE_TEST_POSTGRES`, built for exactly this —
  "sandboxed agents, locked-down CI runners"): started the cached image directly with `docker run -d`, pointed
  the suite at it. `AsanRezerve.Host.IntegrationTests`: 901/901. Re-ran `scripts/verify.sh full --all` end to
  end against that server: **PASS, 21/21 steps, 0 failed** (`.verify/status.json`). Nothing touched after this
  entry; the recorded tree is this file's final state.
