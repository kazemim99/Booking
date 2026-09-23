Status: ACTIVE
Verify: FULL

Source: the 2026-09-23 UI/UX review (numbers #1–#21 below refer to it) and the user's answers the same day — see
proposal.md. Flutter tasks are verified per task with `flutter analyze` + `flutter test` (the FAST tier has no Flutter
step); FULL runs at the end on the merged tree.

## Acceptance scenarios

- Given a salon profile, when the customer taps a service, then the booking flow opens with that service selected.
- Given the booking flow, when it opens, then the tab bar is not shown; back returns to the salon profile.
- Given today is closed or has no free time, when the time step opens, then it shows the first day that has free times.
- Given the salon's weekly hours, when the day strip renders, then closed weekdays are shown disabled.
- Given a day with no free time, when it is shown, then the empty state offers a jump to the next day with free times.
- Given a submitted booking in status Requested, when the success screen shows, then it recaps salon, date and time and
  says the salon still has to accept.
- Given an upcoming booking that can be cancelled/rescheduled, when its detail opens, then both actions are offered.
- Given a booking not among the newest 50 of either list, when its detail opens, then it still loads.
- Given the Past tab has no bookings, then its empty text speaks of past appointments, not upcoming ones.
- Given a completed past booking, then "book again" opens the booking flow at that salon with that service selected.
- Given a salon without an address, when its profile renders, then reviews (and the map, if it has coordinates) show.
- Given any provider card (home, nearby, explore, map), then rating/"no reviews yet", free slots and distance read the
  same way, in Persian digits; "★ 0.0 (0)" never appears.
- Given the theme, then every text/background pair it defines is ≥ 4.5:1 and every icon/UI pair ≥ 3:1 (guard tests).
- Given a signed-in customer opens a salon profile, then the visit is recorded; the heart adds/removes a favourite; home's
  recent/favourite cards open the salon.
- Given a viewport wider than a phone, then the app renders in a centred column of bounded width.
- Given the production database, when the deactivation script runs (twice), then exactly the two test salons are
  Inactive and nothing else changes.

Must not change: routes and deep links (`/providers/:id/book` stays the booking URL), return-to-intent after login,
selection preservation across the login round-trip, slot-taken recovery, optimistic cancel/reschedule + rollback,
wall-clock booking times (`wallClockIso`), the 7-day customer booking window, Toman prices, brand name.

## Tasks

### 0 Foundation
- [x] 0.1 Booking route renders above the shell (root navigator), keeps `/providers/:id/book`, accepts `?service=`

### A Theme, tokens and formatting (#7, #8, #19, decision 2)
- [x] A.1 ColorScheme defines secondaryContainer/onSecondaryContainer; no widget falls back to the green accent
- [x] A.2 `colorScheme.error` and destructive buttons use an AA red; the input error border keeps its own colour
- [x] A.3 Input hints ≥ 4.5:1 and field prefix/suffix icons ≥ 3:1 on white; guard tests compute the ratios
- [x] A.4 Bottom navigation shows a label under each icon; labels ≥ 4.5:1, inactive icons ≥ 3:1 on the bar
- [x] A.5 Weekday names are single words (یکشنبه، چهارشنبه، پنجشنبه) wherever JalaliFormatter.weekday is used
- [x] A.6 Prices use the Persian thousands separator «٬»

### B Web (#2, #10)
- [x] B.1 index.html: lang=fa dir=rtl, Persian title/description, theme-color, a splash removed on the first frame
- [x] B.2 manifest.json carries the app's Persian name, description and colours instead of Flutter placeholders
- [x] B.3 CanvasKit is loaded from the site itself, not gstatic.com
- [x] B.4 CI precompresses the bundle (.gz); the customer vhost enables gzip_static and gzip for js/wasm/json/css/fonts
- [x] B.5 On viewports wider than a phone the app renders in a centred column of bounded width

### C Salon profile (#3, #12, #15, #16)
- [x] C.1 Reviews and the location card render when the salon has no address
- [x] C.2 Tapping a service opens the booking flow with that service selected
- [x] C.3 Service prices wrap instead of truncating
- [x] C.4 A signed-in customer's profile visit is recorded (fire-and-forget; failure never blocks the page)
- [x] C.5 The profile app bar has a favourite toggle for signed-in customers; guests are sent to sign in

### D Booking wizard (#4, #9, #17, decision 4)
- [x] D.1 BookingStarted with a service id preselects that service and moves past the service step
- [x] D.2 The time step opens on the first day with free times (availability summary); falls back to today
- [x] D.3 The day strip disables the salon's closed weekdays
- [x] D.4 An empty day offers "next day with free times", which selects it
- [x] D.5 The step progress bar is visible on the blue app bar at every step
- [x] D.6 Confirm step: a compact summary card plus what happens next (the salon accepts the request)
- [x] D.7 Success screen recaps salon, date, time and says the booking awaits the salon's acceptance

### E Appointments (#5, #16, #18, #19)
- [x] E.1 Appointment detail offers cancel and reschedule under the same rules as the list cards
- [x] E.2 Appointment detail loads a booking missing from both lists via GET /Bookings/{id}
- [x] E.3 The Past tab's empty state speaks of past appointments
- [x] E.4 A completed past booking offers «رزرو مجدد», opening the booking flow with its service selected
- [x] E.5 Reschedule shows the current date/time, uses the salon's booking window and shows the day's empty reason
- [x] E.6 Detail app bar reads «جزئیات نوبت»; the salon button reads «مشاهده سالن»; review action follows canReview

### F Discovery cards, map and home (#11, #13, #16, #19)
- [x] F.1 ProviderMetaLine shows «از … تومان» (starting price) instead of the `$` band; no dangling «·» on wrap
- [x] F.2 Explore's result card uses ProviderMetaLine: Persian digits, «هنوز نظری ندارد», free slots
- [x] F.3 The map card opens the salon on tap, drops the full-width button and shows free slots
- [x] F.4 The map search field has a single border and a search icon; suggestions overlay the map
- [x] F.5 Stars use one colour everywhere (display and input)
- [x] F.6 Home's recent/favourite cards open the salon and show its logo

### G Reviews, inbox, profile, auth (#8, #14, #19, #20, #21)
- [x] G.1 Review dialog uses AppButton and themed inputs; stars have semantic labels and ≥ 48 dp targets
- [x] G.2 Inbox rows show a relative time; loading, empty and error use the shared state widgets
- [x] G.3 App-bar text buttons (inbox «خواندن همه», name screen «بعداً») are white on the bar
- [x] G.4 The name screen leaves only after the save succeeds and shows a failure in place
- [x] G.5 The OTP resend countdown uses Persian digits
- [x] G.6 The booking flow's no-services state says the salon has no services (not "no results")

### I Production data (#1)
- [x] I.0 Public listings (search, by-location) return Active salons only, as the specification's own comment says
- [x] I.1 deployment/sql/deactivate-test-salons.sql: idempotent, two ids only, integration-tested on the real schema
- [ ] I.2 Run the script on production and flush the provider cache
- [ ] I.3 Apply the customer vhost change on the box (root) and reload nginx

### Z Finish
- [ ] Z.1 Adversarial review of the merged diff against the review findings; fix what it confirms
- [ ] Z.2 FULL verify green; runbook and memory updated

### Parked
- [?] P.1 DECISION: account deletion — what happens to a deleted customer's bookings, reviews, ledger rows and phone?
- [?] P.2 DECISION: terms and privacy — who writes them, and at which URL do they live? (the login notice links nowhere)
- [?] P.3 DECISION: deploy — push feat/customer-app-ux-review-fixes to master (deploys production)? Protected operation.
- [?] P.4 DECISION: /Providers/search accepts includeInactive=true from anonymous callers, so anyone can still list Drafted/
      PendingVerification salons. Restrict it to admins? (security/exposure semantics; the admin panel relies on it)

## Decisions

- T1 Flutter tasks are verified with `flutter analyze` + `flutter test`; `scripts/verify -Tier fast` has no Flutter step.
- T1 "Note to the salon" is out of scope: the salon's app never shows `customerNotes`, so the note would go unread.
- T1 Account deletion is not wired to `DELETE /Customers/{id}`: that endpoint is a stub that returns success and does
  nothing, and a button that claims to delete an account while keeping it is worse than no button.
- T2 The two test salons are taken out with SQL, not an admin endpoint: `Provider.Deactivate` has no endpoint and
  production is 97 commits behind; its only side effect (`ProviderCacheInvalidationEventHandler`) is replaced by a cache
  flush. They are set to **Archived**, not Inactive: the search that production runs hides only Archived
  (`Status != Archived`), so Inactive would change nothing until a deploy; Drafted «سالن تست خودکار» cannot be
  Deactivated by the domain anyway. Reversible (the script carries the reverse statement), nothing deleted.
- T2 Public listings are Active-only (I.0). `SearchProvidersSpecification` says "default to active providers only" but
  filters `!= Archived`, so Drafted and PendingVerification salons reach customers; category counts already use Active
  and the admin panel has an activation step. Consequence to flag: a newly registered salon is invisible to customers
  until an admin activates it — which is what the verification step is for. «تجهیزات پزشکی آسان مدیکال»
  (PendingVerification) disappears through this, not through the script; its owner still needs asking.
- T1 Slice G.6 (service-step empty copy) is done in slice D, which owns the booking feature's files.
- T2 A.3: the customer app's hint (#667085, 4.97:1) and field icons (new token fieldIcon #808A9E, 3.47:1) diverge
  from the provider app's Coliride values (#C3CAD9 1.64:1, #D2DBEB 1.39:1) for accessibility; iconMuted stays, decorative only.
- T1 A.6 needed no functional change: the separator already was U+066C; Vazir draws it comma-like, which the review misread.
- T1 A.4: active tab = bold label + filled icon + M3-style pill; labels full white in both states (4.6:1).
- T1 B: frame width 560 on a #EBEEF3 backdrop; CanvasKit from the site; gzip_static + gzip level 5 at server level.
- T1 C: visit + favourite live in a per-page ProviderCustomerCubit; 409 on add / 404 on remove count as success.
  A real customer token carries the Customer role (JwtTokenService with user.Roles) — confirmed from source only.
- T1 D: the first-free-day search queries slots day by day (knows the visit's duration and staff; the availability
  summary is service-agnostic); at most window+1 requests. The day strip now sizes from the text scaler (fixes an
  existing 1.0x overflow, also in reschedule). BookingFlowPage takes an optional bloc for tests.
- T1 E: detail cancel waits for the server (no optimistic step there); list refreshes on return from a detail.
- T1 F: explore/map free times via the availability summary, first 20 ids (server cap); map DTO now reads totalReviews
  (every salon on the map read "no reviews" before); OSM credit wraps instead of truncating (policy requires it).
- T1 G: dialog insetPadding 16/24 so five 48 dp stars fit at 360 dp; relative-time rules in core/utils/relative_time.dart.
- T2 I.0: by-location already filtered Active; only search changed. Consequence (flag): a salon finishing registration
  in the provider app is PendingVerification and invisible to customers until an admin activates it. verifiedOnly=true
  now returns nothing unless includeInactive=true (no client sends it).
- T1 I.1: the script bumps Provider.Version and stamps LastModifiedBy so a stale save fails instead of reverting it.
- T2 Found during merge: favourites/recently-visited responses carry only ids, so the client's parser threw on any
  non-empty list — and C.4 makes every signed-in customer have visits. Fixed end to end in the polish pass (Z.1).

## Log

- 2026-09-23 Slices A–G and I implemented in 8 worktrees (16 agents: implement + adversarial review each, all
  reviews clean with minor findings only), cherry-picked onto origin/master 77cdaf13 as feat/customer-app-ux-review-fixes
  (one import conflict, resolved). Merged tree: flutter analyze clean, 581 tests green. A first run was stopped because
  isolation worktrees were cut from origin/master instead of the branch head; its 5 worktrees are left in .claude/worktrees.

- 2026-09-23 Change opened from the UI/UX review; user approved all findings and answered the four questions.
