Status: PROPOSED
Verify: FAST

<!-- Not ACTIVE. Awaiting approval; do not start implementing. -->

Change: add-notification-clients. Three slices, each shippable on its own, ordered so the cheapest fix to
the worst lie goes first and the riskiest work is last.

Every slice is written test-first, as the repository standard requires. What "test" means differs per client
and is stated per task, because a Vue component test and a Flutter widget test protect different things.

## 1. Preferences stop lying (booksy-frontend)

The smallest slice and the only one fixing something actively false: six toggles exist, none is sent.

- [ ] 1.1 Characterise today's behaviour first: a component test asserting `handleSubmit` emits an update
      WITHOUT any notification field. It passes now and fails when 1.2 lands — that is the diff.
- [ ] 1.2 Extend `UpdatePreferencesRequest` and the submit path to carry channel preferences
      (`EnabledChannels`) and category preferences (`EnabledTypes`), and wire them to
      `PUT /api/v1/notifications/preferences`.
- [ ] 1.3 Map the UI's six toggles onto the backend's model honestly. Email/SMS/push are CHANNELS; booking
      reminders/promotions/marketing are CATEGORIES (`NotificationPreferenceCategory`). They are not the
      same axis and must not be flattened into one list.
- [ ] 1.4 **DECISION NEEDED — see proposal "Risks".** Category toggles persist but change nothing today,
      because `ShouldSend` consults channels only. Either mark them as not yet in effect, or open a separate
      change to make the backend honour types. Do not ship a control that silently does nothing; that is the
      defect this slice exists to remove.
- [ ] 1.5 Round-trip test: save preferences, re-open the screen, see what was saved.
- [ ] 1.6 A failed save shows an error and does not leave the screen displaying unsaved values as saved.

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
