Status: ACTIVE
Verify: FAST

QA 2026-09-23: both apps run in production as Flutter WEB on Android Chrome (customer.nahalkmi.ir,
provider.nahalkmi.ir). The salon expects a phone notification when a customer books; the customer when the
salon confirms. Push was a deliberate no-op on web. User decision: web push NOW.

## Findings (measured, before any code)
- Backend sends through FCM HTTP v1 via FirebaseAdmin 3.1.0 (`FirebaseMessagingGateway`). Credential:
  `Notifications:Firebase:CredentialsJson` or `...:CredentialsPath` (service-account JSON). Neither appears
  in docker-compose.prod.yml; the API reads `/opt/booksy/.env` (env_file), which is not in the repo.
- `DeviceToken.Platform` exists (`Unknown|Android|Ios|Web`, stored as a string) and the API binds it from a
  string. Nothing about web needed a schema change.
- The gateway built `Message { Token, Notification, Data }` only — no `webpush` block, so a browser would
  show an LTR notification and a tap would do nothing (Firebase's SW opens nothing without a link).
- Both apps already carry the same push stack (firebase_core 4.15 / firebase_messaging 16.7): token source,
  registration, tap router, route mapping. Web was short-circuited in `FirebasePushTokenSource.isAvailable`.
- firebase_core_web 3.12.0 loads the Firebase JS SDK 12.19.0 from www.gstatic.com at runtime, with no
  timeout: an unreachable gstatic hangs `Firebase.initializeApp` forever.
- Flutter 3.44's loader re-registers flutter_service_worker.js at scope `/` whenever a registration already
  covers the page — a push service worker at `/` would be replaced on the next load.
- firebase_messaging_web: `getInitialMessage` is always null and `onMessageOpenedApp` never fires, so a tap
  on web must be routed by our own service worker.
- Both routers send every cold-start location to splash and then to home/dashboard: a notification tap that
  opens the app would have lost its target.
- Provider push mapped a booking to the plain calendar, while the inbox (QA 2026-09-22) opens the calendar
  ON the booking; the push comment says it makes the inbox's choice.

## Acceptance scenarios
- Given a web build WITHOUT the Firebase dart-defines, the app behaves exactly as before: no Firebase load,
  no prompt, no row, no registration.
- Given a configured web build, signing in never shows a browser permission prompt; the prompt appears only
  after the person taps «فعال‌سازی اعلان‌ها» (profile/More row, or the one-time card).
- Given permission was granted earlier, signing in registers this browser's token with platform `Web`.
- Given the person enables notifications while signed in, the token is registered at once.
- Given a declined/blocked permission, nothing is registered and the row says how to unblock it.
- Given the soft card was dismissed once, it never shows again; the row stays available.
- Given a push arrives while the app is on screen, it shows as an in-app snackbar with «مشاهده».
- Given the app is closed, tapping the notification opens the app on the booking (customer: appointment
  detail; salon: calendar on that booking), through sign-in if needed.
- Given the app is open in a tab, tapping focuses it and routes there.
- Every FCM message carries a webpush block (RTL, Persian, app icon, tag = notification id, urgency high)
  that Firebase accepts; Android/iOS sections are untouched.

## Tasks
- [x] Backend: FcmMessageFactory with webpush block; gateway uses it; unit tests red first
- [ ] Customer: WebPushConfig from dart-defines (unit tests)
- [ ] Customer: PushRegistration never prompts on web at sign-in; enable()/status() (unit tests)
- [ ] Customer: web token source (init with options + timeout, VAPID, SW path, platform Web)
- [ ] Customer: service worker web/push/firebase-messaging-sw.js (config from query, tap routing)
- [ ] Customer: /push-open route survives cold start via splash (router tests)
- [ ] Customer: tap while open (SW message bridge) + foreground snackbar (tests)
- [ ] Customer: profile row + one-time card on booking success (cubit + widget tests)
- [ ] Provider: mirror all of the above (More row, Home card); push opens the calendar ON the booking
- [ ] CI: pass FIREBASE_WEB_* dart-defines from vars/secrets; builds without them unchanged
- [ ] Runbook: what the user must create (Firebase Web app, VAPID key, GitHub vars) + reachability
- [ ] flutter analyze + flutter test in both apps; dotnet build + affected tests
- [ ] flutter build web --release of each app outside the repo, with and without the defines
- [-] BLOCKED: a real push on a phone needs the Firebase Web app config + VAPID key (user's console)

## Decisions
## Log
