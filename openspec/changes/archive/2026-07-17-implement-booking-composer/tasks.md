## 1. Backend — make bookings confirmable

- [x] 1.1 `BookingPolicy.Default`: `requireDeposit: false, depositPercentage: 0`; update `BookingPolicyTests.Default_...`
- [x] 1.2 Run ServiceCatalog domain unit tests; rebuild + restart local host

## 2. Flutter data layer

- [x] 2.1 `ApiConstants`: providerStaff, availableSlots, createBooking endpoints
- [x] 2.2 `HomeApiService`: `getProviderStaff`, `getAvailableSlots` (envelope-tolerant, local times), `createBooking`
- [x] 2.3 `HomeRepository(+Impl)`: staff list, slots, create (walk-in notes convention); Failure mapping
- [x] 2.4 Parser/repository unit tests (envelopes, notes convention, error mapping)

## 3. Composer state & UI

- [x] 3.1 `ComposerCubit`: parallel load (services+staff), selection state, slots re-fetch with stale-result guard, submit; unit tests incl. race + failure-preserves-input
- [x] 3.2 `BookingComposerPage`: one-screen sections, bottom-sheet pickers, slots grid (local wall-clock), gated submit, error surface; Persian `AppStrings`
- [x] 3.3 Wire ⊕ "نوبت جدید", empty-agenda action, GetDiscovered walk-in CTA → composer; success → pop + Home refresh + snackbar
- [x] 3.4 Widget tests: gating, slot selection, real-theme action rows (footgun), RTL

## 4. Verification

- [x] 4.1 `flutter analyze` + full Flutter suite green
- [x] 4.2 Live E2E against running host: compose → create (walk-in notes present) → provider **confirm succeeds** → Home agenda shows the booking
