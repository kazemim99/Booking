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

**And one piece of the UI is actively false.** `booksy-frontend`'s `ProfilePreferences.vue` renders six
notification toggles — email, SMS, push, booking reminders, promotions, marketing — with "enable all" and
"disable all" buttons. `handleSubmit` sends `language`, `timezone`, `currency`, `dateFormat` and
`timeFormat`, and none of the toggles. A provider switches SMS off, saves, and keeps receiving SMS. That is
worse than the feature being absent: the product gives a written assurance it does not keep.

Two smaller defects of the same family:

- `booksy-admin`'s `AdminUserMenu.vue` links to `/notifications`. No such route exists — a dead link in a
  shipped menu.
- `booksy-frontend`'s `api-config.ts` declares `notifications.list: '/notifications'` and
  `notifications.unread: '/notifications/unread'`. The real routes are `/Notifications/inbox` and
  `/Notifications/unread-count`. The constants have never been exercised, so nothing has caught it.

## What Changes

Three vertical slices, each one usable on its own.

1. **Preferences actually persist.** Wire the existing toggles to `PUT /notifications/preferences`, mapping
   the UI's channel and category checkboxes onto `EnabledChannels` and `EnabledTypes`. Includes removing any
   toggle the backend cannot honour, rather than leaving a control that does nothing.
2. **Devices register for push.** `firebase_messaging` in both Flutter apps: request permission, obtain the
   FCM token, `POST /DeviceTokens` on sign-in and on token refresh, `DELETE /DeviceTokens` on sign-out.
   Foreground and background message handling that opens the notification's destination.
3. **The inbox is readable.** A notification list with unread count and mark-read in all four clients:
   `booksy-frontend`, `booksy-admin` (which also gets its missing route), and both Flutter apps.

Also: correct the wrong endpoint constants, so the first real caller does not have to discover them.

## Impact

- Affected specs: `notification-clients` (new)
- Affected code: `booksy-frontend/src`, `booksy-admin/src`, `booksy-customer-app/lib`,
  `booksy-provider-app/lib`. **No backend change is required** — every endpoint this needs already exists,
  is scoped to the caller, and is covered by integration tests.
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
