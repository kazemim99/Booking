## 0. Test-infrastructure prerequisites (discovered during §1)

- [x] 0.1 Reqnroll codegen is disabled in this project (`Directory.Build.props`/`.targets`, per reqnroll#2043), so `.feature.cs` files are committed artifacts and a new `.feature` alone runs **nothing** — it fails silently by being invisible. Generation does work on SDK 10.0.103 with the overrides temporarily moved aside; see design.md for the procedure and its pitfalls
- [x] 0.2 Wrote the missing reschedule step definitions — the existing `BookingLifecycle.feature` reschedule scenario was **inconclusive, not passing**, because `When I send a POST request to reschedule the booking with:` and its two `Then`s were never implemented
- [x] 0.3 Added `Given the provider is open every day` — reschedule scenarios move a booking "3 days from now", which lands on a different weekday per run; the existing Mon-Fri hours step made the outcome depend on the calendar

## 1. Pin current behavior (before touching handlers)

- [x] 1.1 Added `Features/Bookings/RescheduleBooking.feature` covering all three bookable-resource kinds plus an unresolvable-resource case, with per-kind `Given` seeding steps (incl. a real `OrganizationMembership.CreateUnclaimed` member)
- [x] 1.2 Baseline captured — **legacy sub-provider is the only kind that resolves**; membership and organization-direct both 404, confirming the diagnosis:
  - membership → `RESOURCE_NOT_FOUND — Individual provider with ID … not found`
  - organization-direct → `RESOURCE_NOT_FOUND — Individual provider … does not belong to organization` (the `ParentProviderId` check, as predicted in the proposal)
  - unresolvable resource → already passes (404 is the intended outcome)
- [x] 1.3 ~~Confirm the legacy sub-provider scenario passes~~ — **it does not.** With the calendar flakiness removed it gets *past* resolution and then fails persisting the successor booking: `The property 'Booking.Policy#BookingPolicy.BookingId' is part of a key and so cannot be modified`. **There is no working reschedule path**, so §2 has no green scenario to guard it (see §1.5)
- [x] 1.4 Confirmed end-to-end, then confirmed fixed — see §4.3
- [x] 1.5 **Resolved**: took option (a) — fixed the owned-entity defect first (§2a), which turned the legacy path green and gave §2's refactor the guard it needed

## 2a. Fix the successor booking's owned state (unblocks a green baseline)

- [x] 2a.1 `Booking.Reschedule` now clones the successor's owned value objects. It was aliasing **three**, not two — `TotalPrice` (an owned `Price`) as well as `PaymentInfo` and `Policy`; ADR-005 already lists `Booking.TotalPrice` as a prior instance of this exact bug class. Added the missing typed `Clone()` to `Price`, `PaymentInfo` (deep — its four nested `Money` values are themselves owned) and `BookingPolicy`, following the `Money.Clone()` precedent. `Duration` is a scalar conversion, not owned, so it is left shared
- [x] 2a.2 Legacy sub-provider scenario **passes** — green baseline established (unresolvable-resource scenario also green; membership + organization-direct still 404, as expected before §3)
- [x] 2a.3 Membership scenario now asserts the original keeps its own policy/payment/price and the successor carries the same *values* — the assertion that detects the aliasing being reintroduced

- [x] 2a.4 **Third instance of the same defect family**, found when the atomicity scenario seeded a second booking for one service: `Booking.CreateBookingRequest` stored the caller's `totalPrice`/`policy` instances directly, and callers legitimately pass another aggregate's owned entities (`service.BasePrice`, `service.BookingPolicy`). Two bookings for the same service then share one owned `Price`/`BookingPolicy` → the same re-parenting rejection. The factory now takes defensive copies, matching what `PaymentInfo`'s constructor already did for `Money`
- [x] 2a.5 Test helper fix: `BookingsControllerTests.CreateTestProviderWithServicesAsync` created its service but never activated it, so every booking against it failed `ValidateBookingConstraints` with "این خدمت فعال نیست" (409). Now qualifies the organization for its own service and activates it

## 2. Extract the shared resolver (no behavior change)

- [x] 2.1 Introduced `BookableResource` + `IBookableResourceResolver` covering membership | organization-direct | legacy sub-provider. Carries `ResourceId`, `Kind`, `SlotOwnerId` and `PersonId` (the person behind the resource, for walk-in detection). **`SlotStaffKey` was dropped**: `FindOverlappingSlotsAsync` does not filter on `ProviderAvailability.StaffId`, so per-member slot narrowing is not expressible today — see §2.5
- [x] 2.2 `CreateBookingCommandHandler` now resolves via the resolver; its `isProviderCreated` walk-in check reads `resource.PersonId` instead of poking at the membership/sub-provider aggregates
- [x] 2.3 Creation's slot-keying ternary replaced with `resource.SlotOwnerId`
- [ ] 2.4 Verify creation's existing tests (unit + Reqnroll + `tests/e2e/keystone-booking-flow.sh`) are still green — this step must be behavior-neutral
- [x] 2.5 **Finding for `booking-slot-integrity`**: `FindOverlappingSlotsAsync`'s 5th argument is `excludeSlotId` (a slot to skip), but creation was passing `staffId` into it. Harmless in practice — a membership id never equals a slot id — but it reads as staff-scoping that is not happening: the query never filters `ProviderAvailability.StaffId`, so an organization's overlapping slots are returned and marked regardless of which member owns them. Corrected to `excludeSlotId: null` in both handlers (behaviour-neutral); the missing staff filter is left for that change

## 3. Fix the reschedule path

- [x] 3.1 Reschedule resolves via the resolver; the sub-provider assumption and its three checks are gone
- [x] 3.2 Availability now validated as creation does — `ValidateBookingConstraintsAsync` + `GetConflictingBookingsAsync(resource.ResourceId, …)` — replacing `IsTimeSlotAvailableAsync`, whose signature demands a staff `Provider` a member does not have
- [x] 3.3 The booking being moved is excluded from its own conflict check
- [x] 3.4 Old-slot release and new-slot marking keyed by `resource.SlotOwnerId` rather than the booking's organization
- [x] 3.5 An explicit `NewStaffId` is resolved with `requireBookable: true`; the resource already on the booking is resolved with `requireBookable: false`, so a member deactivated since booking can still have existing appointments moved

## 4. Verify

**Finding (not fixed here):** a booking seeded at 10:00 reads back from the database as 13:30 — exactly the Tehran +3:30 offset — so `DateTime` values are being round-tripped through a timezone conversion somewhere between `TimeSlot.Create` and materialisation. Same family as the `GET /Bookings/my-bookings` `from`/`to` binding bug that `add-playwright-e2e` already fixed in `BookingsController`. It does not affect reschedule correctness (both sides of every comparison go through the same conversion), so it is recorded rather than chased.

- [x] 4.1 **All four resource-kind scenarios green** — membership and organization-direct included. Both defects fixed
- [x] 4.2 Added "A rejected reschedule leaves the original booking untouched" (409 on a taken slot → original still Requested, still at its original time). **Scope note:** injecting a failure *between* slot release and slot occupy is not reachable through the API, so this covers the user-visible guarantee — rejection is all-or-nothing — rather than mid-transaction rollback. A true fault-injection test would need handler-level unit tests with mocks
- [x] 4.3 **`npm run e2e:pw` → 7 passed / 1 skipped / 0 failed**, `booking-reschedule.spec.ts` included. The two provider-registration specs also had to be un-blocked, but for an unrelated reason: `ProvidersController.RefreshProviderToken` HTTP self-calls the retired standalone UserManagement service, and `appsettings.json` still points it at `:5000` while the host runs on `:5050`. They had been passing locally only because another service happened to occupy `:5000`; once it stopped, they failed. Added the two `Services__*__BaseUrl` overrides to the `frontend-e2e` workflow, which would otherwise have hit this on its first real run (the e2e README already documented them)
- [x] 4.4a `tests/e2e/keystone-booking-flow.sh` **passes — 16/16 checks** (legacy staff + membership chain), including "A customer books that specific member"
- [ ] 4.4b ServiceCatalog integration suite fully green — **not achievable from this change alone.** `CancelBooking_AsCustomer` and `RescheduleBooking_WithValidNewTime` now fail only on response *wording* (`"Request completed successfully"` instead of `"cancelled/rescheduled successfully"`), from the generic-envelope change in the ~70 uncommitted source files already in the tree. `RescheduleBooking_WithValidNewTime` returns **200 OK** — the reschedule itself works. Left for whoever owns that envelope change
- [x] 4.5 `add-playwright-e2e` §6.1 closed and its blocker note updated
