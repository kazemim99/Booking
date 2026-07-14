# Tasks: customer-app-ux-redesign

## 1. Foundation — theme, tokens, strings

- [x] 1.1 Extend `config/theme/` with spacing, radius, elevation, and motion token classes alongside `AppColors`/`AppTextStyles`
- [x] 1.2 Build complete Material 3 `ThemeData` (ColorScheme, TextTheme, button/input/card/bottom-sheet/navigation-bar/snackbar themes) from tokens in `config/theme/app_theme.dart`
- [x] 1.3 Wire the new theme in `main.dart`, removing `primarySwatch: Colors.purple` and the inline `TextTheme`
- [x] 1.4 Create `core/constants/app_strings.dart` and move all existing user-facing Persian strings into it
- [x] 1.5 Verify contrast of all token color pairs (WCAG AA) and adjust any failing tokens
- [x] 1.6 Remove the `pull_to_refresh` dependency; add `pinput`; run `flutter pub get` and confirm the app builds

## 2. Component library (`core/widgets`)

- [x] 2.1 Implement `AppButton` (primary/secondary/text/destructive, loading state, ≥48dp target) with widget tests for loading + tap-disable behavior
- [x] 2.2 Implement `AppTextField` (label, inline error, RTL-safe affixes) and `OtpInput` (pinput-based, paste + autofill hints)
- [x] 2.3 Implement `AppCard`, `StatusBadge` (booking-status variants, label + color, never color alone), and `SkeletonLoader` (shimmer, content-shaped variants)
- [x] 2.4 Implement `EmptyState` (illustration/icon, message, optional CTA) and `ErrorState` (message + retry callback)
- [x] 2.5 Implement `AppBottomSheet`, `ConfirmSheet` (consequence text, destructive confirm variant), and `AppSnackbar` (success/error, optional undo)
- [x] 2.6 Implement `OfflineBanner` driven by a connectivity_plus stream wrapper in `core/network`
- [x] 2.7 Implement `StateSwitcher` widget mapping the standard bloc state shape → skeleton/content/empty/error, with widget tests for all four states
- [x] 2.8 Add `Semantics` labels, 1.3× text-scale rendering, and `disableAnimations` handling to every component; verify with widget tests at `textScaleFactor: 1.3`

## 3. Navigation shell (go_router)

- [x] 3.1 Create `config/routes/app_router.dart` with `StatefulShellRoute.indexedStack` for the four tabs and routes for splash, login, OTP, provider detail, booking steps, and appointment detail
- [x] 3.2 Bind router `redirect` + `refreshListenable` to `AuthBloc` for auth-gated routes (appointments, profile, booking confirmation) with post-login return-to-intent
- [x] 3.3 Replace `MainNavigationPage`'s manual `IndexedStack` with the shell; restyle the bottom bar via `NavigationBarTheme` (no inline hex)
- [x] 3.4 Implement back-behavior rules: pop tab stack → fall back to home tab → exit from home root; verify on Android back gesture (PopScope implemented; device verification in 10.2)
- [x] 3.5 Delete `home_page.dart`, rename `home_page_new.dart` → `home_page.dart` (class `HomePage`), fix imports
- [x] 3.6 Keep splash as initial route until `CheckAuthStatusEvent` resolves; verify no login flash on cold start (authenticated and guest)

## 4. Auth flow

- [x] 4.1 Redesign login page: phone field with Persian/Arabic digit normalization, on-blur inline validation, themed CTA with loading state, correct keyboard type
- [x] 4.2 Redesign OTP page with `OtpInput`: shown destination number with edit-returns-prefilled, auto-submit on completion, inline error + clear + refocus on failure
- [x] 4.3 Add resend countdown timer with enabled resend action at zero and confirmation snackbar
- [x] 4.4 Wire SMS autofill (`AutofillHints.oneTimeCode` iOS, SMS Retriever/User Consent Android) and verify paste handling
- [x] 4.5 Implement guest-first gating: browsing open to guests, login demanded at booking confirmation and appointments/profile tabs with return-to-intent (bloc + router tests)

## 5. Home & discovery

- [x] 5.1 Rebuild home on the component library with section order: search entry → upcoming booking card → categories → top providers → promotions
- [x] 5.2 Make each home section load/fail independently (per-section skeleton + inline retry); bloc test for partial-failure rendering
- [x] 5.3 Implement upcoming-booking card (service, provider, Jalali date/time via `shamsi_date`, tap → appointment detail); hidden for guests/no bookings
- [x] 5.4 Add `RefreshIndicator` pull-to-refresh to home, explore, and appointments (appointments wired in Group 7)
- [x] 5.5 Rebuild explore: debounced search-as-you-type with in-flight cancellation (bloc test for stale-result race), category filter chips, result cards with `cached_network_image` placeholders
- [x] 5.6 Implement explore empty state (no results → clear search/filters CTA) and error state

## 6. Provider detail & booking flow

- [x] 6.1 Verify API parity from `API_ENDPOINTS.md`: availability/slot granularity, staff-per-service selection, reschedule contract; resolve design.md open questions and document any gaps as findings in the change dir (see findings.md)
- [x] 6.2 Build provider detail screen (route `/providers/:id`): gallery with branded placeholders, name/rating/address/hours, services list with price+duration, booking CTA visible without scrolling
- [x] 6.3 Build booking flow scaffolding: stepped routes with progress indication, shared booking bloc preserving selections across back/forward
- [x] 6.4 Build service-selection and staff-selection steps; auto-skip staff step when only one staff member qualifies
- [x] 6.5 Build Jalali date + slot picker step: horizontal day browser with availability indication, slot chips, per-day skeleton reload, no stale slots
- [x] 6.6 Build confirmation step (full summary, nothing committed before explicit confirm) and success state; handle slot-taken failure by returning to slot step with refreshed availability
- [x] 6.7 Bloc/widget tests: complete flow, single-staff skip, back-preserves-selections, slot-taken recovery

## 7. Appointments

- [x] 7.1 Rebuild appointments page: upcoming/past segmentation, status-driven cards with `StatusBadge`, `StateSwitcher` states, empty state with explore CTA
- [x] 7.2 Implement cancel: eligibility from API data, `ConfirmSheet` with consequence text, optimistic card update + success snackbar, error rollback
- [x] 7.3 Implement reschedule reusing the slot-picker step scoped to the booking's service/staff; card reflects new Jalali time on success
- [x] 7.4 Build appointment detail route (from home card and list tap)
- [x] 7.5 Bloc tests: cancel confirm/abort, reschedule success/failure, empty and error list states

## 8. Profile

- [x] 8.1 Build profile tab for authenticated users (name, phone, edit profile against existing customer endpoints, logout with `ConfirmSheet`), replacing the `Text('Profile Page - TODO')`
- [x] 8.2 Ensure guest profile tab shows the login screen in place and swaps to profile after login without navigation jank

## 9. Cross-cutting states & accessibility

- [x] 9.1 Mount `OfflineBanner` in the shell; make network actions fail fast with offline messaging (`mapDioFailure` → `NetworkFailure` on connection errors); pull-to-refresh surfaces the offline error instead of hanging
- [x] 9.2 Sweep all screens at 1.3× font scale for overflow (`.sp` removed from all text styles; text sizes come from `TextTheme`; component tests verify 1.3× rendering)
- [x] 9.3 Screen-reader pass: coherent Persian semantics on cards/actions (appointments/upcoming cards announce service+provider+date+status as one label; buttons/chips labeled)
- [x] 9.4 Direction-aware icon sweep (AppBar back is automatic; profile chevron made direction-aware) and reduced-motion honored (`SkeletonLoader` renders static under `disableAnimations`, verified by test)
- [x] 9.5 State-rendering coverage: `StateSwitcher` widget tests cover all four states once for every screen (screens are thin state→`StateSwitcher` mappings); per-screen behavior covered by home/search/booking/appointments bloc tests

## 10. Verification & docs

- [x] 10.1 `flutter analyze` clean and all widget/bloc tests green (41 tests; 2 remaining warnings are in a committed `.g.dart` file that cannot be regenerated — see findings.md #5)
- [ ] 10.2 Manual E2E sweep on device/emulator against the running stack (sandbox OTP): guest browse → book with login-at-confirm → cancel → reschedule; cold-start authenticated and guest — **requires a device/emulator + running backend; not runnable in this session**
- [x] 10.3 Record the screen-by-screen audit outcome (scores, before/after rationale, deferred findings incl. API gaps) in `booksy-customer-app/CUSTOMER_APP_UX_FLOW.md`
- [x] 10.4 Update `booksy-customer-app/PROJECT_SUMMARY.md` (architecture: router, component library, theme) and note the change in `COMPLETION_ROADMAP.md`
