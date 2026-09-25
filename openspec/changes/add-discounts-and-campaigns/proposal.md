## Why

AsanRezerve has no way to discount anything. A salon cannot run a "20% off weekday mornings" offer to fill
empty chairs, cannot hand a regular a code, and the platform cannot run a seasonal campaign (Nowruz, Yalda,
opening week) that salons join. Every trace of the idea in the code is inert: `CalculatePricingQuery`
takes a client-supplied discount nobody calls, `PriceChangeReason.Promotional` and
`InvalidServicePriceException.PromotionalPricingViolation` are never referenced, the customer app's home
"پیشنهادهای ویژه" carousel is fed by a stub that returns `[]`, and `price_formatter.dart` has two
discount formatters nothing calls. Salon booking marketplaces treat promotions as a core supply-and-demand
lever (off-peak fill, new-client acquisition, retention); we ship none of it.

## What Changes

**One promotion model, two owners.** A `Promotion` aggregate is owned either by a **provider** (a salon's
own discount on its own services) or by the **platform** (an admin campaign). A promotion is either
**automatic** (applied without the customer doing anything, shown on the salon page) or **code-based**
(a coupon the customer types at checkout). Its benefit is a percentage (with an optional cap) or a fixed
amount off. Its conditions are optional and composable: validity window, days of the week and a daily
time window evaluated on the appointment's salon-local time (off-peak pricing), minimum visit subtotal,
specific services, new-customers-only, a total usage limit and a per-customer limit.

**Platform campaigns are opt-in and salon-funded** (decision 2026-09-25). An admin publishes a campaign;
each salon decides to join or leave it from the provider app. A campaign applies only at salons that have
joined. Because every discount is funded by the salon that honours it, the booking's price simply is the
discounted price: deposit, cancellation fee, earnings and the ledger all keep working on `TotalPrice`
with no subsidy account and no platform→salon settlement.

**At most one discount per booking — the best one** (decision 2026-09-25). The server evaluates every
eligible automatic promotion and any code the customer entered, and applies the single largest discount.
A code that is worse than an automatic offer already applied is reported as such, not silently dropped.

**Price is quoted and then snapshotted by the server.** A new quote endpoint returns subtotal, discount
and total (and why a code was refused, in Persian) before the customer confirms. Booking creation
re-evaluates on the server — a client never sends a price or a discount — and the booking records the
discount it received (promotion, title, code, owner, amount). Later edits to the promotion never change
an existing booking. The discount survives a reschedule.

**Usage is counted exactly.** Each applied discount writes a redemption. Limits are enforced under
optimistic concurrency on the promotion, so two customers racing for the last use cannot both get it.
Cancelling a booking (customer or salon) releases its redemption (decision 2026-09-25); a no-show keeps it.

**Clients.**
- Admin panel: a "کمپین‌ها و تخفیف‌ها" section to create, edit, pause, resume and end platform campaigns,
  see which salons joined and how much each campaign has been used, and pause any salon's promotion.
- Provider app (Flutter): a "تخفیف‌ها" entry in the More hub to create and manage the salon's own
  promotions, and a campaigns list to join or leave platform campaigns.
- Customer web app and customer app (Flutter): discount badges and struck-through prices on the salon
  page, a code field and a subtotal/discount/total breakdown at the confirm step, and the discount on the
  booking's detail.

**Guardrails.** A discount can never exceed 90% of the discounted services' price (so a booking is never
free and `PaymentInfo`'s positive-total invariant holds), is rounded down to a whole Toman, and never
applies to bookings a salon enters for its own walk-in clients.

**BREAKING (web client only):** the web wizard's confirmation card stops showing a hard-coded 9% tax the
server never charges; it shows the server's quote instead.

## Impact

- Affected specs: new `discount-pricing`, `provider-promotions`, `platform-campaigns`.
- Affected code: ServiceCatalog Domain (new `PromotionAggregate`, `Booking` discount snapshot), Application
  (pricing service, commands/queries, `CreateBooking`/`CancelBooking`/`RescheduleBooking` handlers),
  Infrastructure (3 tables + 5 booking columns, one migration), Api (3 controllers + quote endpoint);
  `asan-rezerve-admin`, `asan-rezerve-provider-app`, `asan-rezerve-frontend`, `asan-rezerve-customer-app`.
- Out of scope, recorded as follow-ups: platform-funded or co-funded campaigns (would need a subsidy
  ledger account and settlement), stacking, per-provider category targeting of campaigns, bundle/
  package pricing, notifying salons when a campaign is published, the Vue provider dashboard (the live
  provider surface is the Flutter provider app).
