Status: DONE
Verify: FULL

Continues `reduce-integration-baseline-um-payments`, which closed at ad963e27 with 10 entries left in
`tests/known-failures.txt`. This change takes the ones that are not blocked on FOLLOW-UPS #48.

## Acceptance scenarios
- S1 Every line removed from `tests/known-failures.txt` passes for a root cause — a defect fixed or a test corrected against the real contract — never by weakening an assertion
- S2 The list only shrinks; FULL verify stays green with no off-list failure

## Tasks
- [x] 1 ProviderManagementTests.RegisterProvider_WithValidData: authenticated as a fabricated guid with no users row; registration mints a provider token, which loads the owner. Uses CreateAndAuthenticateAsRealUserAsync now — class 31/31
- [x] 2 WorkingHoursManagementTests (3): the tests sent times as "09:00" strings to an endpoint that takes { hours, minutes }, and typed a response field as TimeOnly? that arrives as "10:00". Break labels, which the domain has always carried, are now settable
- [x] 3 BookingsControllerTests (2 of 3): a past start time is a 400 rather than a 409; the cancel test read the envelope's message instead of the action's payload and its own tracked booking instead of the row. RescheduleBooking is #48 — measured, its new time reads back +3:30
- [x] 4 The #48-blocked lines stay listed with their reason: 2 Availability, 1 Notifications, and now RescheduleBooking. All four go together when #48 is fixed
- [x] 5 FULL verify GREEN at 9506bf6e: 17/17 steps in 999s. ServiceCatalog integration 497 passed with exactly the 4 baseline failures and nothing off-list; UserManagement 37/37; both Vue apps and both Flutter apps pass

## Decisions
- 2026-09-10 A booking whose start time is in the past is refused with 400, not 409. Every failure from `ValidateBookingConstraintsAsync` was reported as Conflict, so "you asked for yesterday" and "someone else has that slot" were indistinguishable to a client — and only one of them is a conflict. Tier 2.
- 2026-09-10 `BreakTimeDto` gains an optional `Label`, passed through to `BreakPeriod.Create`. The domain value object has always had the field; no request shape exposed it, so every break saved through the API was unlabelled. Additive and backward compatible. Tier 2.

## Log
- 2026-09-10 Opened. Remaining baseline at ad963e27: 3 WorkingHours, 3 Bookings, 2 Availability, 1 Notifications, 1 ProviderManagement.
- 2026-09-10 Six lines removed; 4 remain and all four are FOLLOW-UPS #48. Measured per class: WorkingHours + ProviderManagement 31/31, BookingsControllerTests all green except RescheduleBooking, whose only remaining failure is the new time reading back exactly +3:30 — the server's UTC offset.
