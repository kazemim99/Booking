Status: ACTIVE
Verify: FAST

<!-- ACTIVE since 2026-09-21: approved by the user, order left to me — 1, then 3, then 2 (push last,
     because it is the only slice that cannot be verified on a device from this environment). -->

Change: add-notification-clients. Three slices, each shippable on its own, ordered so the cheapest fix to
the worst lie goes first and the riskiest work is last.

Every slice is written test-first, as the repository standard requires. What "test" means differs per client
and is stated per task, because a Vue component test and a Flutter widget test protect different things.

## 1. Preferences take effect (backend default + booksy-frontend)

REWRITTEN 2026-09-21 after reading the code — the first draft targeted a dead component. See the log.

- [x] 1.1 BACKEND, test-first: `NotificationPreference.Default` includes `PushNotification`. Failing test
      first: a user whose preferences row is created by saving one unrelated field must still be sent push.
      Today they are not — the row is built from a Default that predates push.
- [x] 1.2 A preferences client in booksy-frontend for `GET`/`PUT /api/v1/notifications/preferences`, with
      unit tests for the channel mapping. The rule the tests pin: a save changes ONLY the channels the screen
      shows and carries every other channel through unchanged. There is no in-app toggle; omitting in-app
      from the PUT would switch it off.
- [?] 1.3 DECISION (see log 'what a preference screen may promise'). Customer preferences screen reads and writes that client instead of
      `PATCH /customers/{id}/preferences`, whose values nothing reads. Tested: toggling SMS off sends a
      channel set without SMS and with in-app intact.
- [?] 1.4 DECISION (same). Provider `NotificationSettings.vue` reads and writes that client instead of
      `PUT /provider-settings/{id}/notification-settings`, a route that does not exist.
- [?] 1.5 DECISION (same). `ReminderTiming` and any category toggle: the backend does not honour them (reminder offsets are
      fixed; `ShouldSend` ignores `EnabledTypes`). Remove them or mark them not in effect — never leave a
      control that silently does nothing. DEFAULT TAKEN: mark as not in effect, because removing a visible
      control is a UX call and a label is reversible. Raised at the end.
- [?] 1.6 Follows 1.3/1.4. A failed save shows an error and does not display the attempted values as saved.
- [x] 1.7 `ProfilePreferences.vue` and the unrouted `views/ProviderProfileView.vue` that renders it: dead
      code, and the component that misled the first draft of this change. Delete, after confirming again
      that nothing imports either.

## 2. Devices register for push (booksy-customer-app, booksy-provider-app)

- [ ] 2.1 Add `firebase_core` + `firebase_messaging` to both apps. Platform config
      (`google-services.json`, `GoogleService-Info.plist`) is account-specific and must come from whoever
      owns the Firebase project — it is not in the repository.
- [ ] 2.2 A `PushRegistrationService` per app behind an interface, so the token source and the API call can
      both be faked. Unit tests for: register on sign-in, re-register on refresh, revoke on sign-out,
      permission declined is a no-op, API failure does not throw into the sign-in path.
- [ ] 2.3 Wire it into the real auth flow: register after a successful sign-in, revoke before clearing the
      session on sign-out. Tested through the auth flow, not just the service — a service nobody calls is
      the failure mode this whole change exists to fix.
- [ ] 2.4 Foreground and background message handlers. Tapping opens the notification's subject; a subject
      that is gone or no longer theirs opens the list instead of an error.
- [ ] 2.5 `flutter analyze` clean and `flutter test` green in both apps.
- [ ] 2.6 **CANNOT BE VERIFIED HERE.** `flutter build apk` fails in this environment because Google Maven
      404s (see the Android build note in the team's notes). A real end-to-end push must be confirmed on a
      machine with working Maven access before this slice is called done. Park it rather than claim it.

## 3. The inbox becomes readable (all four clients)

- [ ] 3.1 Fix the wrong endpoint constants in `booksy-frontend/src/core/api/config/api-config.ts`:
      `list` is `/Notifications/inbox`, `unread` is `/Notifications/unread-count`. Add a caller in the same
      commit, because an unexercised constant is how they became wrong in the first place.
- [ ] 3.2 A notification service + store per web client: list (paged, newest first), unread count,
      mark-read, mark-all-read. Against the real API shape — responses are wrapped in a `data` envelope.
- [ ] 3.3 `booksy-frontend`: a notifications screen and an unread badge. Empty state says there is nothing;
      it does not show a spinner forever or a blank panel.
- [ ] 3.4 `booksy-admin`: the same, plus the `/notifications` ROUTE that `AdminUserMenu.vue` already links
      to and which does not exist — a dead link in a shipped menu.
- [ ] 3.5 Both Flutter apps: a notifications list, unread badge and mark-read. The provider app's home
      screen already has a bell whose own comment calls it "a placeholder until notifications ship"; this is
      what it was waiting for.
- [ ] 3.6 Assert the badge and the list agree. They disagreed once already on the server side (the inbox
      returned undelivered rows while the count did not) and the symptom was invisible until asserted.
- [ ] 3.7 Tests per client: the list shows only the caller's notifications, reading one decrements the
      count, mark-all-read twice is harmless.

## 4. Verification

- [ ] 4.1 `scripts/verify.ps1 -Tier fast` green.
- [ ] 4.2 `scripts/verify.ps1 -Tier full` green — it already runs both Vue apps' type-check and lint and
      both Flutter apps' analyze and test, so all four clients are covered by the existing gate.
- [ ] 4.3 On a device: sign in, receive a push, tap it, land on the right screen. Blocked on 2.6.

## Log
- 2026-09-21 Scoped after the user asked whether the clients were done. They were not, and nothing had been
  written down about them — `notification-system` was backend-only from its first line, which should have
  been stated in ITS proposal rather than discovered here.
  Established by reading the clients rather than assuming: zero references to device tokens or Firebase in
  any of the four; the frontend's `notification.store.ts` is a TOAST store, unrelated to the server inbox;
  the declared notification endpoint constants are wrong and have never been called; the admin's
  notifications menu entry has no route; and `ProfilePreferences.vue` renders six toggles that
  `handleSubmit` does not send.
  Also established, and the reason this is not a total gap: the 13 SMS-carrying notifications reach people
  today with no client involvement, and they are the critical set.
- 2026-09-21 Slice 1 rewritten before any code, because the first draft was wrong about WHERE the lie was.
  I had blamed `ProfilePreferences.vue`. Reading the router: it is rendered only by
  `modules/provider/views/ProviderProfileView.vue`, which nothing routes or imports — the routed profile
  view is `views/dashboard/ProviderProfileView.vue`, which does not render it. Dead code. (It also saved to
  `/users/profile/preferences`, served by a `ProfileController` that is commented out in its entirety.)
  The screens people CAN open are worse: the customer screen saves successfully to UserManagement fields the
  dispatcher never reads, and the provider screen saves to a route that does not exist. Meanwhile the one
  store the dispatcher does read has no client.
  And a trap: `NotificationPreference.Default` omits push. Absent preferences mean "send everything", but a
  row created from Default disables push — so wiring any screen to the real store would have silently turned
  push off for every user who saved a setting. Fixed first, test-first, as 1.1.
- 2026-09-21 1.1 done, test-first — and the trap had a second door. `NotificationPreferenceDefaultsTests`
  (5): three write-side tests were RED on the real endpoint — saving only `marketingOptIn` left the row at
  `Email|SMS|InApp`, push gone. The fourth, switching push off on purpose, was green before and after: the fix
  must not make push impossible to turn off.
  Then the READ side: `GET /notifications/preferences` answers a person with no row from a HARDCODED list,
  and that list omitted push too. A client that loads the screen and saves what it loaded would have switched
  push off by round-tripping — which is exactly what the screens in 1.3/1.4 are about to do. Fifth test, red,
  then the fallback was rewritten to DERIVE from `NotificationPreference.Default` so the two can never drift
  apart again. Both halves had to go before any screen was connected, or connecting a screen would have been
  the thing that broke push.
- 2026-09-21 WHAT A PREFERENCE SCREEN MAY PROMISE — parked as a decision, because wiring the screens as
  written would only have built a more elaborate version of the lie this slice exists to remove.
  Measured against the catalogue and the policy, not assumed:
  * SMS toggle: NO EFFECT, by design. The user decided 2026-09-19 that SMS is reserved for critical
    notifications, the catalogue test enforces it, and critical means unsuppressible — so every SMS the
    product sends ignores preferences. A customer can never turn SMS off.
  * Email toggle: NO EFFECT. After InvoiceGenerated was removed, no catalogued notification uses email.
  * Reminder timing (customer 1h/24h/3d; provider hours/minutes): NO EFFECT. Offsets are fixed in
    `BookingReminderScheduler.Offsets` (T-24h, T-2h customer; T-30m salon).
  * Quiet hours (provider): stored by the backend, never consulted — only `ShouldSendNotification` reads
    them, and it has no caller.
  * The provider's per-event × per-channel matrix: the backend has one GLOBAL channel set per person; there
    is no per-event model. And the events it lists (new booking request, cancellation by customer) are
    critical, so the salon could not switch them off anyway.
  * What DOES have an effect: the Push and In-app channels, on the Standard (suppressible) notifications.
    Neither screen shows a push toggle.
  Also a false warning in the customer modal: "disabling all notifications means no booking reminders". The
  2h reminder is critical SMS and arrives regardless.
  Unblocked meanwhile: 1.2 (the client service — every option needs it), 1.7 (dead code), and slice 3.
- 2026-09-21 1.2 and 1.7 done.
  1.2 `notification-preferences.service.ts`, 13 vitest tests written first (red: module absent). The rule
  they hold: a save edits the mask it LOADED and changes only the toggled channels, so in-app — which no
  screen offers — and any channel without a screen survive. Mutated: rebuilding the mask from the toggles
  alone turned 4 of the 13 red, including "never touches in-app". Also pinned: the GET bypasses the HTTP
  client's five-minute cache (the client caches every GET by default — a preferences screen read through it
  shows the value from before the last save), and a failed save THROWS rather than handing back the value it
  tried to save.
  1.7 Deleted `ProfilePreferences.vue` and the unrouted `views/ProviderProfileView.vue` after re-confirming
  nothing imports either; type-check clean. Three siblings of that view — `ProfilePersonalInfo`,
  `ProfilePrivacy`, `ProfileSecurity` — were reachable ONLY through it and are therefore also dead. Not
  deleted: they are profile components, not notification ones, and removing them is outside this change.
  Worth someone's cleanup.
