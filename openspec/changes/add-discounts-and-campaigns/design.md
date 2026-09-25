## Context

Verified 2026-09-25 against `42e0d73`:

- A booking's price is computed in exactly one place: `CreateBookingCommandHandler` sums
  `Service.BasePrice` of the visit's services into `combinedPrice`. `Booking.TotalPrice` feeds
  `PaymentInfo` (deposit), `CalculateCancellationFee`, earnings and the ledger.
- `PaymentInfo` requires `TotalAmount > 0`, and a unit test pins it
  (`PaymentInfoTests.cs:324`). A 100% discount would break booking creation.
- A salon-entered booking's `CustomerId` is the salon owner (walk-in path), not a customer.
- Domain event handlers run in a **new DI scope** (`SimpleDomainEventDispatcher`) on a different
  `DbContext`, so they cannot join the command's transaction. Work that must commit atomically with a
  booking change belongs in the command handler.
- A lost optimistic-concurrency race surfaces as `DbUpdateConcurrencyException` → 409
  (`ExceptionHandlingMiddleware`), the same path slot booking already relies on.
- Online checkout is off in production (`CHECKOUT_ENABLED`); customers pay at the salon.
- Timestamps are UTC instants; salon wall-clock comparisons go through `SalonTime` (+03:30).
- Currency is Toman (`PlatformCurrency.Code = "IRT"`).

## Goals / Non-Goals

Goals: salon and platform promotions with the conditions salons actually use (off-peak, new client,
minimum spend, per-service, limited uses, codes); server-authoritative pricing; exact usage counting;
a price snapshot on the booking; first-class UI in admin, provider and both customer apps.

Non-goals: platform-funded subsidies, stacking, loyalty points, gift cards, dynamic/surge pricing.

## Decisions

### D1. One `Promotion` aggregate with an owner discriminator
Provider promotions and platform campaigns share every rule except ownership and participation, so they
are one aggregate (`Owner = Provider|Platform`, `ProviderId` set only for Provider). Two aggregates would
duplicate the eligibility model and the pricing engine would have to merge them anyway.

### D2. Participation is a separate aggregate (`CampaignEnrollment`)
A campaign can be joined by thousands of salons; holding them as a child collection would load them all
on every redemption. `CampaignEnrollment(PromotionId, ProviderId, IsActive, JoinedAt, LeftAt)` is its own
row with a unique `(PromotionId, ProviderId)`; leaving and re-joining toggles the same row.

### D3. Redemptions are rows; the counter lives on the promotion under a concurrency token
`PromotionRedemption(PromotionId, BookingId, CustomerId, ProviderId, Amount, Status Applied|Released)`.
`Promotion.RedemptionCount` is the aggregate's own count of Applied redemptions and is an EF concurrency
token. Every redemption and release updates the promotion row, so two concurrent redemptions of the same
promotion serialize: the loser gets `DbUpdateConcurrencyException` → 409 and the client retries, at which
point the limit is re-checked. This also closes the per-customer race. A unique index on
`BookingId` (one discount per booking) backs the invariant in the database.

### D4. Pricing is a pure domain policy; loading is an application service
`PromotionPricing.Quote(request, candidates, code)` (Domain/Policies) is deterministic and takes the
clock as input, so every rule is unit-tested without I/O. `IPromotionPricingService` (Application) loads
candidates (the salon's active promotions + active platform campaigns the salon has joined), the
customer's prior uses and whether they are new to the salon, then calls the policy. Both the quote
endpoint and `CreateBooking` go through the same service, so the quote and the booked price cannot
disagree for the same inputs at the same instant.

### D5. Selection: single best discount
All eligible automatic promotions are evaluated; the largest discount wins (tie → provider-owned, then
the one ending soonest, then id for determinism). An entered code is evaluated separately; it replaces
the automatic winner only when strictly larger. Outcomes for the code are explicit:
`Applied`, `NotFound`, `NotEligible(reason)`, `BetterOfferApplied`.

### D6. Money rules
- Discount is computed on the **eligible subtotal** (the lines whose service the promotion targets; all
  lines when it targets none).
- Percentage 1–90; fixed amount > 0; optional cap for percentage.
- The discount is capped at 90% of the eligible subtotal (`PromotionPricing.MaxDiscountShare`) and
  floored to a whole Toman. The booking total therefore stays positive; `PaymentInfo` is untouched.
- `Booking.TotalPrice` is the discounted amount. The snapshot records the discount so the subtotal is
  `TotalPrice + Discount.Amount`. Line items keep their list prices.
- Because the salon funds every discount, nothing in payments, deposits, commission or the ledger changes.

### D7. Time rules
- Validity window (`StartsAt`, optional `EndsAt`) is evaluated at the moment of booking (UTC).
- Days of week and daily time window are evaluated on the appointment start in salon-local time
  (`SalonTime.FromUtc`) — they express off-peak pricing, which is about *when the chair is used*.
- A reschedule keeps the discount the booking was made with (price is locked at booking; the salon can
  cancel if they object). Recorded as a product default; revisit if salons complain.

### D8. Where release and transfer happen
In the command handlers, not in domain event handlers (see Context): `CancelBookingCommandHandler`
releases the booking's redemption before it commits; `RescheduleBookingCommandHandler` re-points the
redemption to the successor booking. `Booking.Reschedule` copies the discount snapshot.

### D9. Salon-entered bookings get no discount
Their `CustomerId` is the salon owner; applying promotions would consume customers' limits on the
owner's account and "new customer" would be meaningless. The salon already controls what it charges.

### D10. Codes
Normalized to upper-case `A–Z 0–9 -`, 4–20 characters. Unique per owner scope: platform codes unique
among platform promotions; provider codes unique within that provider (a partial unique index on
`(scope_key, code)` where `scope_key = provider_id` or the empty GUID for the platform). A code cannot
change once redeemed. The quote endpoint is authenticated and rate-limited so codes cannot be enumerated
anonymously.

### D11. Authorization
- Provider endpoints: admin, the owner (`providerId` claim), or an active member with
  `OrganizationPermission.ManageOrganization` — discounts are pricing, not day-to-day bookings.
- Admin endpoints: `AdminOnly` policy, `api/v1/admin/promotions`.
- `GET providers/{id}/offers` is anonymous (public salon page) and lists automatic offers only; codes
  are never listed.

### D12. Lifecycle
`Status = Active | Paused | Ended`; the displayed state is derived: `Scheduled` (before start),
`Active`, `Paused`, `Expired` (past end), `Exhausted` (limit reached), `Ended`. Ended is terminal.
Editing is allowed until Ended; a booking's snapshot is never affected by edits.

## Risks / Trade-offs

- **Hot promotion row** under a very popular campaign: every redemption updates the same row. At this
  platform's volume that is fine; if it ever is not, move the counter to a sharded counter table.
- **Quote/booking drift**: a promotion can expire between quote and confirm. The booking uses the
  server's evaluation at confirm time and returns the applied discount; clients show the returned total.
  An entered code that became invalid fails the booking with its Persian reason instead of silently
  booking at full price.

## Migration Plan

One additive migration: three new tables and five nullable booking columns. No backfill. Rollback is the
migration's `Down`.

## Open Questions

None blocking. Follow-ups listed in the proposal.
