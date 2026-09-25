Status: ACTIVE
Verify: FAST

Change: add-discounts-and-campaigns. User request 2026-09-25: a full discount system — admins run campaigns,
salons run their own discounts — built to best practice in architecture and UI/UX. Business decisions taken
by the user the same day (see `## Decisions`). Test-first: within each slice the test task precedes the
implementation task. `scripts/verify.sh fast` after each task, `full` to finish.

## Acceptance scenarios

- Given a salon with automatic 10% and 20% offers both eligible, when a customer books, then 20% applies and
  one redemption is recorded against the booking.
- Given a code worth less than the automatic offer, when quoted, then the automatic offer applies and the code
  reports «تخفیف بهتری روی این نوبت اعمال شده است».
- Given an off-peak offer Sat–Wed 10:00–14:00 (salon time), when the appointment is Thu 11:00, then no discount.
- Given a 500,000 fixed discount on a 300,000 service, then discount 270,000 and total 30,000 (90% cap).
- Given a single-use code, when the customer books then cancels, then the code is usable again; a no-show keeps it.
- Given a promotion with one use left and two concurrent bookings, then at most one gets it (other → 409).
- Given a discounted booking, when rescheduled, then the successor keeps total, discount and redemption.
- Given a salon-entered walk-in during an automatic offer, then list price and no redemption.
- Given an admin campaign, when a salon has not joined, then it never applies there; after joining it does.
- Given a promotion edited from 20% to 10% after a booking, then that booking still shows 20%.
- Unchanged: bookings with no promotion price exactly as before; deposit/cancellation fee use TotalPrice.

## Tasks

## 1. Domain
- [x] 1.1 Unit tests: Promotion create/update validation (percent 1–90, fixed>0, cap, window, code format, time window, limits).
- [x] 1.2 Promotion aggregate, DiscountRule, PromotionSchedule, enums; lifecycle pause/resume/end; derived state.
- [x] 1.3 Unit tests: eligibility per condition (window, weekday/time salon-local, min subtotal, services, new-customer, limits, status).
- [x] 1.4 Unit tests: PromotionPricing selection (best wins, tie-break, code outcomes, 90% cap, floor to Toman, targeted lines).
- [x] 1.5 PromotionPricing policy + PriceQuote/AppliedDiscount types; Promotion.RecordRedemption/ReleaseRedemption with limits.
- [x] 1.6 Unit tests + impl: CampaignEnrollment join/leave/rejoin; PromotionRedemption apply/release/transfer.
- [x] 1.7 Unit tests + impl: Booking carries AppliedDiscount snapshot (total = subtotal − discount), Reschedule copies it.

## 2. Persistence
- [x] 2.1 EF configurations (promotions, campaign_enrollments, promotion_redemptions, booking discount columns), repos, DI.
- [x] 2.2 Migration AddDiscountsAndCampaigns (additive) + model snapshot; architecture tests stay green.

## 3. Application
- [x] 3.1 Unit tests: PromotionPricingService loading (provider promos + joined campaigns, new-customer, prior uses).
- [x] 3.2 IPromotionPricingService + QuoteBookingPriceQuery.
- [x] 3.3 Unit tests: request parsing (PromotionTermsInput.ToTerms) and handler guards; domain is the single validator.
- [x] 3.4 Provider commands/queries: create, update, pause, resume, end, list with stats; campaigns list, join, leave.
- [x] 3.5 Admin commands/queries: platform campaign CRUD + lifecycle, list all with filters, details with enrollments/stats.
- [x] 3.6 CreateBooking applies the best discount + redemption (customer bookings only); invalid code → 400 with reason.
- [x] 3.7 CancelBooking releases the redemption; RescheduleBooking transfers it to the successor.
- [x] 3.8 Public offers query (automatic, applicable now, no codes); booking DTOs expose subtotal/discount.

## 4. API
- [x] 4.1 Controller unit tests: authorization outcomes and request→command mapping for the three controllers.
- [x] 4.2 ProviderPromotionsController, AdminPromotionsController, offers + POST Bookings/quote (auth, rate limited).
- [x] 4.3 Integration tests (Host, Testcontainers): end-to-end discount booking, cancel release, concurrency, authz.
- [x] 4.4 API_ENDPOINTS.md + DTO docs updated.

## 5. Admin panel (asan-rezerve-admin)
- [x] 5.1 promotions.api.ts + types + unit tests (URLs, payload mapping).
- [x] 5.2 Campaign form composable (validation, payload build) + unit tests.
- [x] 5.3 Promotions page: platform campaigns / salon promotions tabs, create/edit drawer, lifecycle actions, details.
- [x] 5.4 Route, sidebar entry, fa/en locale keys; type-check + unit tests green.

## 6. Provider app (asan-rezerve-provider-app)
- [x] 6.1 Promotions data layer (api service, models, repository) + repository tests.
- [x] 6.2 PromotionsCubit + CampaignsCubit + bloc tests.
- [x] 6.3 Promotions page (list, state chips, usage), create/edit sheet, campaigns tab with join/leave; widget tests.
- [x] 6.4 More hub row, route, DI, AppStrings; flutter analyze + test green.

## 7. Customer web (asan-rezerve-frontend)
- [x] 7.1 promotion.service.ts (offers, quote) + unit tests.
- [x] 7.2 Salon page: offer banner, per-service discount badge and struck-through price.
- [x] 7.3 Booking confirmation: code field, server quote breakdown (drop fake 9% tax), send code on create; tests.
- [x] 7.4 type-check + lint + unit tests green.

## 8. Customer app (asan-rezerve-customer-app)
- [x] 8.1 Offers + quote datasource/repository, entities; tests.
- [x] 8.2 Salon page badges/discounted price; confirm step code field + breakdown via BookingBloc; bloc + widget tests.
- [x] 8.3 Booking detail shows the discount; flutter analyze + test green.

## 9. Finish
- [x] 9.1 openspec validate --strict; project.md / KNOWLEDGE_MAP touch-ups; FOLLOW-UPS rows for out-of-scope items.
- [ ] 9.2 scripts/verify.sh full; report.

## Decisions

- 2026-09-25 (user, tier 3): platform campaigns are opt-in and salon-funded; no platform subsidy, no ledger change.
- 2026-09-25 (user, tier 3): one discount per booking, the best one; a code replaces the automatic offer only if larger.
- 2026-09-25 (user, tier 3): cancelling (customer or salon) releases the redemption; no-show and completed keep it.
- Tier 2: discount capped at 90% of the eligible subtotal and floored to a whole Toman, so `PaymentInfo`'s positive-total
  invariant (pinned by `PaymentInfoTests`) stays intact and no booking is free.
- Tier 2: salon-entered (walk-in) bookings are never discounted — their CustomerId is the salon owner.
- Tier 1: validity window is evaluated at booking time; weekday/time window on the appointment's salon-local start.
- Tier 1: a reschedule keeps the booking's discount even if the new time is outside an off-peak window (price locked).
- Tier 1: release/transfer happen in the command handlers, not domain event handlers (those run in a separate scope/DbContext).
- Tier 1: usage limits enforced by a concurrency token on `Promotion.RedemptionCount`; lost race → existing 409 path.
- Tier 1: promotion management needs ManageOrganization (owner/manager), not ManageBookings.
- Tier 1: no FluentValidation validators for promotions — the aggregate is the single validator (Persian 400s by field);
  request parsing refuses shape errors. Validators would duplicate every rule in a second place.
- Tier 1: a use lost between pricing and redeeming (limit reached, paused) is a 409 with a Persian retry message, not a
  400 — retried, the visit is priced without it (spec: "fails with a conflict and, when retried, is priced without it").
- Tier 2 (fix): CreateBookingResult now fills DurationMinutes/Currency/PaymentStatus/CreatedAt; they were never set, so
  the 201 body carried an empty currency.
- Tier 2 (fix): the web wizard booked only the first selected service while showing (and now quoting) the sum of all;
  it now sends serviceIds so the booked visit is the one priced. It also dropped a hard-coded 9% tax row the server
  never charged — the card shows the server's quote.
- Tier 1: the web salon page shows a struck-through price only for an offer with no day/time/new-customer condition;
  conditional offers show the badge and the condition, never a price that may not apply to the customer's slot.
- Tier 1: a promotion lost between quote and booking is 409 `PROMOTION_UNAVAILABLE` (PromotionUnavailableException), so the
  customer app keeps the customer on the confirm step with the new price instead of treating it as a taken slot.
- Tier 1: the customer app's confirm step keeps ONE summary card (pinned by an existing layout test); the promotion's name
  sits on its own line because the summary's label column does not wrap (found at 360x640, 1.3x text).
- Tier 2 (test infra): PostgresTestContainerFixture honours ASANREZERVE_TEST_POSTGRES (an existing server instead of a
  container) and scripts/verify treats it as the db source, so FULL can run where no Docker daemon exists. Opt-in;
  unset changes nothing.

## Log

- 2026-09-25 Investigation: no discount system exists anywhere; price computed once in CreateBookingCommandHandler;
  PaymentInfo forbids zero totals; domain events dispatch in a new scope; online checkout off in prod. Toolchain in
  this container: .NET 10 SDK (apt) building net9.0 with DOTNET_ROLL_FORWARD=Major, Flutter 3.47.5, Node 22; no
  Docker daemon, so the Testcontainers integration suites cannot run here. FAST baseline green (9 steps).
