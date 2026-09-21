# Notification clients — the last mile

## Why

The notification engine is finished and tested, and two thirds of it reaches nobody.

`notification-system` built the raising, scheduling, de-duplication, retry, copy, delivery log, inbox API,
device-token registry and a real Firebase sender. All of it is server-side, all of it is covered by tests,
and none of it was ever connected to a client. The result is a system that works exactly as far as the
gateway and then stops.

**Thirteen notifications do arrive.** Everything that carries SMS reaches people today, because SMS goes to
a gateway and needs no app: `BookingConfirmed`, `BookingRejected`, `BookingRescheduled`,
`BookingCancelledByProvider`, `BookingReminder2h`, `PaymentReceived`, `PaymentFailed`, `RefundProcessed`,
`PhoneVerification`, `PasswordReset`, `SecurityAlert`, `InvitationSent`, `PayoutCompleted`. That is the
critical set, and it is genuinely live.

**The other twenty-four reach nobody at all.** They are push and in-app only, and:

- **No client registers a device token.** Zero references to `device-tokens`, `firebase_messaging` or
  `FirebaseMessaging` across `booksy-frontend`, `booksy-admin`, `booksy-customer-app` and
  `booksy-provider-app`. Every push therefore resolves to `PushUnavailable.NoDevice` and is skipped. The
  channel is built, correct, and pointed at an empty address book.
- **No client reads the inbox.** `GET /Notifications/inbox`, `/unread-count`, `POST /{id}/read` and
  `/read-all` have no callers. In-app notifications accumulate in the database and are never shown.

**And the preference screens that people actually use save to places nothing reads.** This is the
worst part, and the first draft of this proposal located it wrongly — corrected after reading the code:

| Screen | Saves to | Read by the dispatcher? |
|---|---|---|
| Customer notification preferences (routed, live) | `PATCH /customers/{id}/preferences` — `SmsEnabled`, `EmailEnabled`, `ReminderTiming` on the UserManagement customer | **No.** Saves succeed; nothing consults them |
| Provider `NotificationSettings.vue` (via `ProviderSettingsView`) | `PUT /provider-settings/{id}/notification-settings` | **The route does not exist** — every save 404s |
| `ProfilePreferences.vue` | nothing — `handleSubmit` omits the toggles | Dead code: rendered only by an unrouted, unimported view |
| — | `PUT /notifications/preferences` → `UserNotificationPreferences` | **Yes — and no client calls it** |

So a customer switches SMS off in the real app, gets a success message, and keeps receiving SMS — because
the dispatcher reads a different table. The first draft blamed `ProfilePreferences.vue`; that component is
unreachable, and the real lie is in the two screens people can open.

`ReminderTiming` is the same defect in miniature: the customer picks it, and reminders are fixed at T-24h and
T-2h in `BookingReminderScheduler.Offsets` regardless.

**One trap the fix would have sprung.** `NotificationPreference.Default` is `Email | SMS | InApp` — without
`PushNotification`, which was added to the product later. A user with no preferences row is sent everything
(absent preferences never read as "disabled"), but the moment a row is CREATED it is built from `Default`, so
the first time anyone saves any preference, push is silently switched off for them. Wiring the screens to the
real store would have made that happen to every user who touched their settings.

Two smaller defects of the same family:

- `booksy-admin`'s `AdminUserMenu.vue` links to `/notifications`. No such route exists — a dead link in a
  shipped menu.
- `booksy-frontend`'s `api-config.ts` declares `notifications.list: '/notifications'` and
  `notifications.unread: '/notifications/unread'`. The real routes are `/Notifications/inbox` and
  `/Notifications/unread-count`. The constants have never been exercised, so nothing has caught it.

## What Changes

Three vertical slices, each one usable on its own.

1. **Preferences take effect.** Both live screens — customer and provider — read and write the one store the
   dispatcher consults, `/notifications/preferences`. A save flips only the channels the screen shows and
   preserves the rest (there is no in-app toggle, so a save must not switch in-app off by omission). Any
   control the backend cannot honour is removed or marked as not in effect. Fix `Default` to include push
   first, test-first, so wiring the screens cannot silently disable it.
2. **Devices register for push.** `firebase_messaging` in both Flutter apps: request permission, obtain the
   FCM token, `POST /DeviceTokens` on sign-in and on token refresh, `DELETE /DeviceTokens` on sign-out.
   Foreground and background message handling that opens the notification's destination.
3. **The inbox is readable.** A notification list with unread count and mark-read in all four clients:
   `booksy-frontend`, `booksy-admin` (which also gets its missing route), and both Flutter apps.

Also: correct the wrong endpoint constants, so the first real caller does not have to discover them.

## Impact

- Affected specs: `notification-clients` (new)
- Affected code: `booksy-frontend/src`, `booksy-admin/src`, `booksy-customer-app/lib`,
  `booksy-provider-app/lib`, and ONE small backend fix: `NotificationPreference.Default` must include push.
  The first draft said no backend change was needed; that was true of the endpoints and false of the
  defaults behind them.
- Existing customer opt-outs saved through the old screen live in UserManagement and have never had any
  effect. Switching the screen to the real store shows those customers their ACTUAL settings (all on). Their
  earlier choice is not carried across unless someone runs a backfill on the production database — a
  protected operation, raised at the end rather than done here.
- Twenty-four notifications go from built-but-unreachable to delivered.

## Risks and constraints

- **Android cannot be built in this environment.** Google Maven 404s here, so `flutter build apk` fails for
  reasons unrelated to the code. Dart logic will be unit-tested and `flutter analyze` kept clean, but the
  push integration cannot be verified on a real device from here. Someone has to run it on a machine with
  working Maven access before it can be called done. This is called out now rather than discovered at the
  end.
- **Firebase credentials per environment** are already documented in `docs/DEPLOYMENT_RUNBOOK.md` for the
  server; the clients additionally need `google-services.json` / `GoogleService-Info.plist`, which are
  account-specific and not in the repository.
- **Preferences are not consulted for type filtering today.** `NotificationSuppressionPolicy.ShouldSend`
  asks only about channels; `EnabledTypes` is stored but never read (pinned by
  `NotificationTypeCharacterisationTests`). So the category toggles will persist truthfully but will not yet
  change what is sent. Either the UI says so, or the backend starts honouring types — a decision this
  proposal raises rather than settles, because it changes what people receive.
