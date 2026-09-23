Status: DONE
Verify: FULL

User report (2026-09-23): after yesterday's deploy, «سالن نهال»'s photos still do not load in the application.
Investigated with a 17-agent audit (workflow `media-url-coverage-audit`: 4 sweeps over backend emitters, client
renderers, storage/serving and live production, a cross-check, then 2 independent verifiers per gap), plus
read-only probes against production. Each surface (customer Flutter + Vue, provider, admin) is checked per item
(see memory: plan all three frontends).

## Findings
_Root causes, with evidence._

- **Why the photos don't load today (G1): the fix was never deployed.** Production runs `origin/master`
  680a67ea (2026-09-21 20:13). The URL fix (d42c5bce, 2026-09-22) is one of 16 commits on
  `feat/provider-auth-flutter` that were never pushed (`git ls-remote origin`: master 680a67ea, the branch
  cdebbd87). `deploy.yml` runs only on a push to master. Live: `/Providers/search`, `/Providers/{id}` and
  `/Providers/by-location` return Nahal's photos as `uploads/providers/0312bbc9-…/gallery/…_medium.webp`;
  resolved on customer.nahalkmi.ir that is 200 `text/html` (the SPA index), on back.nahalkmi.ir it is 200
  `image/webp` with `access-control-allow-origin: https://customer.nahalkmi.ir`. The earlier image fix that
  WAS deployed (feaba9bf, 2026-09-20: `Image.network` instead of `dart:io` caching on web) fixed a different
  layer, the loader, not the URLs.
- Storage is fine: all 9 of Nahal's gallery files (3 photos × thumb/medium/original) serve as `image/webp`
  from back.nahalkmi.ir. `/Providers/{id}/gallery` (absolute since before master) lists the same 3 photos.
- **G2, a latent data-loss defect, not triggered for Nahal yet: editing the business name deletes the whole gallery.**
  `Provider.UpdateBusinessProfile` does `Profile = BusinessProfile.Create(...)`: a new owned profile with an
  empty gallery, keeping only logo and profile image (tags, social links and website are dropped too).
  `ProviderWriteRepository.UpdateProviderAsync` then marks every tracked `GalleryImage` Deleted. The provider
  app's More → business details calls exactly this (PUT `/Providers/business`, name + description). A
  gallery-only salon like Nahal would lose every photo row and fall to the placeholder permanently. Reached
  also by PUT `/Providers/profile` (with an image) and registration step 3; `UpdateDraftInfo` (going back in
  registration) has the same pattern. Same code on master. Nahal's 3 rows still exist (verified live).
- **G3 the deploy gate cannot see this class of defect.** After publishing, `deploy.yml` checks only that
  `/health` and the three app roots answer 200 (a Flutter SPA root is 200 whatever the API sends). The
  integration tests that pin absolute URLs are not a deploy gate (the `test` job runs unit tests only).
- **G7 browser-cache CORS miss (found in the probe, not by the audit's verifiers).** `/uploads/*` is served
  `Cache-Control: public, max-age=2592000`, and `Vary: Origin` is sent ONLY when the request carries an
  `Origin` header (ASP.NET CORS). A browser that first loads a photo through a plain `<img>` (admin panel,
  Vue site on back.) caches a copy with no CORS header and no Vary. All `*.nahalkmi.ir` apps share one cache
  partition (same site), so the Flutter apps' later CORS fetch of the same URL can be answered from that
  copy and fail → placeholder, intermittently, only in browsers that opened admin/Vue first.
- G6 the POST gallery-upload response still returns the stored relative paths; Vue `/provider/gallery` pushes
  them into the grid, where they resolve under `/provider/…` → index.html until reload. Low traffic (no nav
  link to that view; the dashboard's GalleryManager re-fetches). Registration GalleryStep/preview resume case too.
- G4/G5 (verifiers: not live defects, hardening): staff `PhotoUrl` (qualified-staff, hierarchy members,
  PATCH membership) and `OrganizationLogo` (memberships/me, invitation summary/detail, send invitation) are
  sent raw, and OrganizationLogo reads `LogoUrl` instead of `DisplayImageUrl` (null for a gallery-only
  salon). Only Vue on back. renders them today (root-relative loads there). Separately,
  `AcceptInvitationView.vue` binds `:src` to `organizationName` — a real bug: the invitation logo never shows.

## Decisions
_Reversible engineering calls, recorded._

- Partial-update semantics on the profile, matching the rule already written in
  `UpdateBusinessProfileCommandHandler` ("omitting a field must leave the stored value alone"): editing name/
  description changes only those; a null profile image / draft logo keeps the stored one. Previously a draft
  re-save without a logo cleared it — that was a side effect of the same replace pattern, not a feature.
- `GetDraftProviderByOwnerIdAsync` has no status filter, so step 3 can reach an active provider. Not changed
  here: filtering would send an active owner down the "create new draft" path (a second provider). With the
  profile updated in place the gallery survives either way. Logged as a follow-up.
- G7 fixed on both ends: the API always sends `Vary: Origin` on static files (so no cache serves a no-CORS copy
  to a CORS request), and the Flutter apps use `WebHtmlElementStrategy.fallback` (a failed CORS fetch falls back
  to an `<img>` element instead of the placeholder).
- G3: a read-only post-deploy smoke script, not re-enabling the integration tests in `deploy.yml` (dotnet.yml
  already runs them; making them a deploy gate changes deploy latency — a separate call).
- Pushing/deploying: the user asked (2026-09-23) to commit, push directly to master and switch the checkout to
  master once everything is verified. Pushing master deploys the whole branch (reviews migration + recompute step,
  the QA fixes, this change) — see the runbook's post-deploy steps.
- Found on the way, logged rather than fixed: FOLLOW-UPS #69 (step 3 reaches active salons), #70 (Redis-cached
  provider probably cannot be deserialized — unverified).

## Tasks

- [x] 1 G2 business edit keeps the gallery — domain unit tests (UpdateBusinessProfile, UpdateDraftInfo keep
      gallery/tags/social/website; null image/logo keeps stored) + Host test (PUT /providers/business and
      PUT /providers/profile on a salon with a gallery → GET detail still has every photo, logoUrl = primary)
      RED → fix in place (`BusinessProfile.UpdateDetails`) → GREEN
- [x] 2 G6 gallery upload response absolute — Host test RED → `UploadGalleryImagesCommandHandler` uses IUrlService
- [x] 3 G4 staff photo absolute in qualified-staff, hierarchy members, PATCH membership — test RED → fix
- [x] 4 G5 OrganizationLogo = absolute DisplayImageUrl in the 4 membership/invitation handlers — test RED → fix;
      Vue `AcceptInvitationView.vue` binds the logo, not the name (+ unit test)
- [x] 5 G7 `Vary: Origin` on every static-file response — Host test RED → fix; customer + provider Flutter
      `Image.network(webHtmlElementStrategy: fallback)` (+ widget test pins the strategy)
- [x] 6 G3 `tests/e2e/media-url-smoke.sh` (read-only: media URLs absolute https on search + detail, one upload
      fetch is image/* with the CORS header) + deploy.yml step after publish + keystone job step
- [x] 7 Surfaces: customer (Flutter + Vue), provider, admin — each checked, recorded below
- [x] 8 FULL verify green; commit; report (the fix reaches users only when the branch is deployed)
      FULL PASS on master 6797842c (11 steps, 796 integration tests); the touched frontends run by hand on
      the same tree (verify skips apps it sees as untouched once committed): Vue type-check/lint/145 unit,
      customer app analyze + 360 tests, provider app analyze + 615 tests — all green. Shipped on master
      without booking-64's in-progress 9fe50414/db17b2c4 (their call: hold).

## Surfaces

- **Customer — Flutter (customer.nahalkmi.ir, and native).** The cause of the report. All 6 photo sites read
  search / by-location / detail, absolute since d42c5bce — no client change needed for the URLs. `ProviderImage`
  now falls back to an HTML image on the web when a CORS fetch fails (G7); native unaffected (CachedNetworkImage).
  Test: `widgets_test.dart` (web strategy), `ProviderPhotosReachCustomersTests` (URLs).
- **Customer — Vue (back.nahalkmi.ir).** Staff photos in the booking wizard (qualified-staff) now absolute —
  loaded before too (same host, root-relative); the Vue URL helpers pass absolute URLs through. Nothing else.
- **Provider — Flutter (provider.nahalkmi.ir).** Renaming the salon (More → business details) no longer deletes
  the gallery (G2 — the real risk for this surface). Gallery tiles fall back to an HTML image on a CORS failure.
  Upload responses are ignored (it re-fetches GET /gallery), so G6 does not reach it. organizationLogo is parsed,
  not rendered — nothing to change. Test: `more_test.dart`.
- **Provider — Vue salon dashboard.** `/provider/gallery` shows new uploads immediately (G6); invitation screen
  now shows the salon's photo (it dropped the logo and bound src to the name); my-profile / my-organization show
  the salon's gallery photo as the organization logo. Test: `AcceptInvitationView.spec.ts`.
- **Admin (admin.nahalkmi.ir).** ProviderTable avatars read search / by-status — absolute since d42c5bce;
  ProviderDetails reads /gallery (absolute already). Admin's plain `<img>` loads were what could poison the shared
  browser cache for the Flutter apps — fixed server-side by `Vary: Origin` (G7). No admin code change needed.
