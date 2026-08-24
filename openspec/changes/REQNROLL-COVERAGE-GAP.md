# Reqnroll specification-coverage gap

> Generated 2026-08-24 during the feature audit. **Not a defect list.** These scenarios do not fail
> because the product is broken — they fail because their step definitions were never implemented,
> so Reqnroll reports them inconclusive. Treat this as a specification-coverage backlog.

**707 of 739 scenarios (95%) are blocked by at least one unbound step.**
Only 5 features are fully runnable, and they are the booking/payment core.

Implementation status: `Runnable` = every step bound · `Partial` = <50% unbound ·
`Mostly spec` = 50-90% unbound · `Spec only` = >=90% unbound (effectively a written spec with no harness).

| Feature | Business area | Scenarios | Blocked | Unbound steps | Status | Effort |
|---|---|---:|---:|---:|---|---|
| `CQRS/Commands/ProgressiveRegistrationCommands.feature` | CQRS command/query coverage | 45 | 45 | 102/128 | Mostly spec | XL (>10d) |
| `CQRS/Commands/ServiceCommands.feature` | CQRS command/query coverage | 44 | 44 | 77/105 | Mostly spec | L (3-10d) |
| `API/ProviderRegistrationController_API.feature` | API contract | 41 | 41 | 92/101 | Spec only | XL (>10d) |
| `CQRS/Commands/NotificationCommands.feature` | CQRS command/query coverage | 41 | 41 | 85/113 | Mostly spec | XL (>10d) |
| `CQRS/Queries/AllQueries.feature` | CQRS command/query coverage | 37 | 37 | 120/123 | Spec only | XL (>10d) |
| `CQRS/Commands/SaveStep3LocationCommand_ExtendedTests.feature` | CQRS command/query coverage | 34 | 34 | 60/74 | Mostly spec | L (3-10d) |
| `CQRS/Commands/PaymentCommands.feature` | CQRS command/query coverage | 31 | 31 | 74/94 | Mostly spec | L (3-10d) |
| `CQRS/Queries/GetRegistrationProgressQuery.feature` | CQRS command/query coverage | 28 | 28 | 106/110 | Spec only | XL (>10d) |
| `CQRS/Commands/BookingCommands.feature` | CQRS command/query coverage | 28 | 28 | 60/88 | Mostly spec | L (3-10d) |
| `CQRS/Commands/ProviderCommands.feature` | CQRS command/query coverage | 27 | 27 | 62/77 | Mostly spec | L (3-10d) |
| `Payments/ZarinPal/ZarinPalReconciliation.feature` | Payments & gateways | 24 | 24 | 72/75 | Spec only | L (3-10d) |
| `CQRS/Commands/PayoutCommands.feature` | CQRS command/query coverage | 22 | 22 | 50/68 | Mostly spec | L (3-10d) |
| `Notifications/NotificationManagement.feature` | Notifications | 21 | 21 | 57/69 | Mostly spec | L (3-10d) |
| `Payments/ZarinPal/ZarinPalProviderRevenue.feature` | Payments & gateways | 20 | 20 | 42/47 | Mostly spec | L (3-10d) |
| `EdgeCases/DataValidationAndConstraints.feature` | Cross-cutting edge cases | 19 | 19 | 46/50 | Spec only | L (3-10d) |
| `Providers/Gallery.feature` | Provider management | 17 | 17 | 49/56 | Mostly spec | L (3-10d) |
| `Payments/ZarinPal/ZarinPalCustomerHistory.feature` | Payments & gateways | 17 | 17 | 38/42 | Spec only | L (3-10d) |
| `Providers/ProviderManagement.feature` | Provider management | 17 | 17 | 29/48 | Mostly spec | M (1-3d) |
| `Payments/ZarinPal/ZarinPalRefunds.feature` | Payments & gateways | 17 | 17 | 27/46 | Mostly spec | M (1-3d) |
| `Providers/WorkingHours.feature` | Provider management | 15 | 15 | 33/40 | Mostly spec | L (3-10d) |
| `Services/ServiceManagement.feature` | Service catalog | 16 | 15 | 29/44 | Mostly spec | M (1-3d) |
| `Providers/StaffManagement.feature` | Provider management | 14 | 14 | 37/45 | Mostly spec | L (3-10d) |
| `Payments/ZarinPal/ZarinPalPaymentVerification.feature` | Payments & gateways | 13 | 13 | 21/41 | Mostly spec | M (1-3d) |
| `Payments/Behpardakht/BehpardakhtSettlementAndReversal.feature` | Payments & gateways | 13 | 13 | 7/43 | Partial | S (<1d) |
| `Payments/Behpardakht/BehpardakhtPaymentCreation.feature` | Payments & gateways | 11 | 11 | 4/18 | Partial | S (<1d) |
| `EdgeCases/ConcurrencyAndRaceConditions.feature` | Cross-cutting edge cases | 10 | 10 | 41/45 | Spec only | L (3-10d) |
| `EdgeCases/AuthorizationAndSecurity.feature` | Cross-cutting edge cases | 10 | 10 | 27/31 | Mostly spec | M (1-3d) |
| `Availability/Availability.feature` | Availability | 13 | 10 | 14/44 | Partial | M (1-3d) |
| `Payments/Behpardakht/BehpardakhtRefunds.feature` | Payments & gateways | 10 | 10 | 8/32 | Partial | S (<1d) |
| `Payments/Behpardakht/BehpardakhtPaymentVerification.feature` | Payments & gateways | 10 | 10 | 6/38 | Partial | S (<1d) |
| `Payments/ZarinPal/ZarinPalPaymentCreation.feature` | Payments & gateways | 10 | 10 | 3/16 | Partial | S (<1d) |
| `Payments/PaymentAdvanced.feature` | Payments & gateways | 8 | 8 | 15/24 | Mostly spec | M (1-3d) |
| `Bookings/BookingLifecycle.feature` | Booking lifecycle | 9 | 8 | 14/34 | Partial | M (1-3d) |
| `Payments/Financial.feature` | Payments & gateways | 7 | 7 | 23/30 | Mostly spec | M (1-3d) |
| `Payments/Payouts.feature` | Payments & gateways | 7 | 7 | 17/28 | Mostly spec | M (1-3d) |
| `Providers/ProviderRegistration.feature` | Provider management | 7 | 6 | 15/25 | Mostly spec | M (1-3d) |
| `Bookings/CancelBooking.feature` | Booking lifecycle | 4 | 0 | 0/15 | Runnable\* | S (<1d) |
| `Bookings/CreateBooking.feature` | Booking lifecycle | 7 | 0 | 0/18 | Runnable\* | S (<1d) |
| `Bookings/RescheduleBooking.feature` | Booking lifecycle | 5 | 0 | 0/20 | Runnable\* | S (<1d) |
| `Payments/ProcessPayment.feature` | Payments & gateways | 5 | 0 | 0/11 | Credentials-blocked | S (<1d) |
| `Payments/RefundPayment.feature` | Payments & gateways | 5 | 0 | 0/17 | Credentials-blocked | S (<1d) |

## Risk notes

- **Effort is for the step definitions only** — it assumes the scenarios themselves are correct.
  The audit found stale content in several files (invalid `Salon` category, missing provider
  prerequisites), so each feature needs a domain review alongside the harness work.
- **The `Spec only` tier is the decision point.** `AllQueries`, `GetRegistrationProgressQuery`,
  `ProgressiveRegistrationCommands` and `ZarinPalReconciliation` are ~700 lines of Gherkin with
  almost no harness. Implementing them is weeks of work; quarantining them (e.g. an `@wip` tag
  filtered out of CI) makes the suite honest immediately. That is a scope decision, not a technical one.
- **Do not read `Blocked` as "broken product".** A blocked scenario has never executed, so it has
  never made any claim about production behaviour — in either direction.
- **"Runnable" measures structural bindability, not pass rate — read it as "every step exists",
  not "every scenario passes".** `ProcessPayment.feature` and `RefundPayment.feature` were
  originally marked plain `Runnable`; both are actually blocked, because `ProcessPaymentCommand`/
  `RefundPaymentCommand` route through the real, unmocked ZarinPal/Behpardakht gateway regardless
  of which feature file exercises them (FOLLOW-UPS #31) — corrected to `Credentials-blocked`.
  The three `Bookings/*.feature` rows are marked `Runnable*`: every step in those three files
  individually resolves to a real binding, but a combined test run mixing them with
  `CQRS/Commands/BookingCommands.feature` (whose "RescheduleBookingCommand - ..." scenarios share
  enough of a name to complicate filtering) surfaced additional failures — at least one confirmed
  as FOLLOW-UPS #32 (the duplicate `ApiResponseMiddleware`, pre-existing, not caused by this audit)
  — that were not individually re-isolated per file before this document was finalized. Treat the
  three booking rows' `Blocked: 0` as "structurally sound, content-audited, last verified
  in isolation" rather than "currently green in every run configuration."
