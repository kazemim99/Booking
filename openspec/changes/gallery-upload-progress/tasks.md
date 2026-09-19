Status: STOPPED(awaiting production deploy of 703e2cd2 and a live check; code, tests and verify FAST are done)
Verify: FAST

Uploading gallery photos sent every picked image in ONE multipart request with no progress, and the
Gallery page showed nothing until the whole batch finished. With several photos on a mobile
connection that is long enough that the provider thinks the app froze (user, 2026-09-19, with a
reference mock: per-photo thumbnail, circular % over it, size sent / total, a done check, per-photo
cancel, and a footer with overall progress and "cancel all").

## Acceptance scenarios
- U1 A picked photo appears immediately, from the local file, before any byte reaches the server.
- U2 Each photo shows its own upload percentage and bytes sent / total while it uploads.
- U3 A finished photo shows a done mark; a failed one says so and can be retried.
- U4 A footer shows how many are still uploading and the overall percentage, with "cancel all".
- U5 Cancelling a photo stops its request and removes it; the rest continue.
- U6 Photos keep the order they were picked in (the server numbers them in arrival order).

## Tasks
- [x] 1.1 Upload queue (pure Dart): one request per photo, sequential, byte progress, cancel,
      cancel all, retry, overall progress; unit tests with a fake uploader (U2-U6)
- [x] 1.2 Upload tile + summary footer widgets; widget tests (U1-U5)
- [x] 1.3 Gallery page: pick -> queue; the grid refreshes as each photo lands
- [x] 1.4 Onboarding gallery step: upload on Next through the queue with the same tiles
- [ ] 1.5 Verify FAST + flutter, deploy, confirm on the live site

## Decisions
- One request per photo, sent one after another rather than in parallel. The server adds each photo
  to the provider aggregate and numbers it max+1; parallel requests on the same aggregate would race
  for the same version and fail with a concurrency conflict, and would scramble the order. Sequential
  keeps the order the user picked and still gives every photo its own progress (queued photos show
  their thumbnail with "در صف"). Tier 1: client-side only, reversible.
- Both endpoints already append (UploadGalleryImagesCommand -> provider.UploadGalleryImage), so no
  backend change is needed.

## Log
- 2026-09-19 Investigated: onboarding uploadGallery and home uploadGalleryImages each post ALL images
  in one FormData with no onSendProgress; the Gallery page re-fetches only afterwards.
- 2026-09-19 Built. UploadQueue (10 unit tests: order, per-photo and size-weighted overall progress,
  failure + retry without stalling the rest, cancel one / all stops the request, per-photo landing
  callback, whenIdle). UploadProgressPanel/UploadTile/UploadSummaryBar (7 widget tests). Gallery
  page: pick -> queue, the grid refreshes quietly as each photo lands (no full-page spinner); the
  existing upload test was rewritten for the new requirement with stronger assertions (one request
  per photo, never the batch call, done state shown, grid re-read). Onboarding step: Next queues the
  photos cover-first, shows the tiles, advances only once every photo landed; picker made injectable
  for the test that proves order and advance. 522 app tests pass; verify FAST PASS.
