# Follow-ups carried out of archived changes

> Working register. Every item here was a known, documented loose end inside a change that has since been
> **archived** — archiving must never be where a defect goes to die. Each row names where it came from, what
> it actually is, and which change is expected to absorb it.
>
> Created 2026-08-19 alongside the archive batch in `OPENSPEC-AUDIT-2026.md` §4 Step 1–3.
> Remove a row when its destination change exists and owns it explicitly.

## Open

| # | Item | Came from | Destination |
|---|---|---|---|
| 1 | **`FindOverlappingSlotsAsync` never filters `ProviderAvailability.StaffId`.** An organization's overlapping slots are returned and marked regardless of which member owns them, so per-member slot narrowing is not expressible. Found while extracting `IBookableResourceResolver`; the misused 5th argument (`excludeSlotId` being passed a `staffId`) was corrected as behaviour-neutral, but the missing filter remains. | `fix-reschedule-membership-staff` §2.5, handed to `booking-slot-integrity` | Needs its own change — availability correctness, not covered by any planned step |
| 2 | **Expired-JWT handling on SignalR is untested.** The `Token-Expired` header exists (`JwtAuthenticationExtensions.cs:52-56`) and anonymous rejection is proven, but no test presents a genuinely expired token, and none covers an established connection whose token expires mid-flight. | `harden-resource-authorization` §6.3 | `harden-test-suite-and-dependencies` |
| 3 | **Payment integration/architecture tests.** Commit-failure-after-gateway-success leaves a recoverable `Pending`; ZarinPal happy path + duplicate callback credits once; architecture test asserting no `IPaymentGateway` invocation inside a DB transaction scope. `Booksy.ArchitectureTests` is currently an empty stub with `NetArchTest` referenced and no rule enforced. | `payment-consistency-and-idempotency` §4.4–4.6 | `harden-test-suite-and-dependencies` |
| 4 | **EF owned-`Money` insert quirk.** Constructing + `Add`ing a `Payment` aggregate in a fresh scope throws `Unable to track 'Payment.PaidAmount#Money' … PaymentId is null`. Likely root cause of several pre-existing red payment integration tests; forced `PaymentDedupTests` to validate at the raw-SQL layer. | `payment-consistency-and-idempotency` findings | `harden-test-suite-and-dependencies` |
| 5 | **`ByProvider` server-derivation** (currently inert: defaulted false, controller-unset) and **`AddBookingNotesCommand`** not yet in the resource-ownership set. | `harden-resource-authorization` §2.2b, §2.3 | Small follow-up; fold into `harden-test-suite-and-dependencies` or its own change |
| 6 | **Refund→commission reversal entry.** A refund after commission was recognised at payout debits ProviderPayable (correctly reducing/negativing the balance), but posts no PlatformRevenue reversal entry for the commission portion. | `financial-ledger-and-settlement` residuals | `financial-reporting-and-clawback-policy` |
| 7 | **Payout clawback policy.** Negative balance → real transfer is gated off; safe default (block further payouts) is active. Needs a finance decision before enabling. | `financial-ledger-and-settlement` §4.2, audit P1-3 | `financial-reporting-and-clawback-policy` |
| 8 | **Ledger-backed financial reporting endpoints** (admin/finance scope). Additive; out of customer scope. | `financial-ledger-and-settlement` residuals | `financial-reporting-and-clawback-policy` |
| 9 | **Migrate `Money`/`Price` to EF Core complex types** once nullable complex types land (EF 10+). | `fix-aggregate-persistence-concurrency` §4.1 | `booking-data-and-migration-hygiene` (already assigned there) |
| 10 | **Booking `DateTime` timezone round-trip.** A booking seeded at 10:00 reads back as 13:30 — exactly the Tehran +3:30 offset — between `TimeSlot.Create` and materialisation. Does not affect reschedule correctness (both sides of every comparison go through the same conversion), so it was recorded rather than chased. | `fix-reschedule-membership-staff` §4 finding | Needs an owner — same family as the `GET /Bookings/my-bookings` binding bug already fixed |
| 11 | **Duplicate `ApiResponseMiddleware`** (`Core.Domain/Infrastructure/Middleware` and `Infrastructure.API/Middleware`) emitting a generic `"Request completed successfully"`, breaking two integration assertions that expect per-action wording. | `fix-reschedule-membership-staff` §4.4b | `fix-api-response-envelope` |
| 12 | **Gallery main-image E2E stays skipped**, blocked on the `RefreshProviderToken` HTTP self-call to the retired standalone UserManagement service (`ProvidersController.cs:620`). | `add-playwright-e2e`, `harden-e2e-test-coverage` §4 | `fix-provider-token-refresh-in-process` |
| 13 | **`frontend-e2e` CI workflow has not run with `globalSetup`.** Advisory workflow, not a deploy gate. | `harden-e2e-test-coverage` §7.2 | Verify on next CI run; no change needed |
| 14 | **On-device E2E sweep of the customer app** (guest browse → book with login-at-confirm → cancel → reschedule; cold start authenticated and guest). Blocked: no emulator, no `ios/`, and `flutter build apk` cannot complete while Google Maven 404s. | `customer-app-ux-redesign` §10.2 | Release checklist, not a code change |

## Deployment steps (not code)

| # | Item | Came from |
|---|---|---|
| D1 | **Pre-deploy overlap audit.** `EXC_Bookings_Staff_NoOverlap` fails to create if production holds overlapping active bookings for one staff member. Audit before the migration runs. | `booking-slot-integrity` §1.3 |
| D2 | **Pre-deploy duplicate-payment audit.** `UX_Payments_OneCapturedPerBooking` fails to create if any booking already has >1 captured payment. Query `Payments` grouped by `BookingId` where `Status IN ('Paid','PartiallyPaid')`. | `payment-consistency-and-idempotency` §3.1 |

Both are already in `PRODUCTION_READINESS_AUDIT.md` §Deployment checklist; repeated here so they survive the
archiving of their source changes.
