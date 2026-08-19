# Proposal: harden-resource-authorization

## Why

The production-readiness audit confirmed a **broken-access-control (IDOR) blocker**: state-changing customer endpoints authorize only that the caller is *authenticated*, never that they *own the resource*.

- `POST /bookings/{id}/cancel` and `/reschedule` are `[Authorize]`-only; the commands never carry the caller identity and the handlers perform no ownership check (`CancelBookingCommandHandler.cs:31–55`, `RescheduleBookingCommandHandler.cs:54–90`). Any authenticated user can cancel/reschedule **any** booking by GUID — and cancel triggers a gateway refund.
- `POST /payments/{id}/refund` is `[Authorize]`-only with no ownership check (`PaymentsController.cs:178`).
- Several controllers declare **no** `[Authorize]` (`CategoriesController`, `LocationsController`, `PlatformController`) and there is **no global fallback policy**, so they are publicly reachable.

The codebase already demonstrates the correct pattern — `GetBooking` calls `CanViewBooking(...) → Forbid()` (`BookingsController.cs:135–141`) — but applies it inconsistently, proving these are oversights, not design.

## What Changes

- Introduce a reusable **resource-ownership authorization mechanism** applied to every state-changing command that targets a customer-owned or provider-owned resource, so ownership is enforced in one place rather than per-handler.
- Derive the acting user **exclusively from the authenticated principal (JWT)**, never from request-body fields; deprecate spoofable body fields (`CancelledBy`, `ByProvider`).
- Add a **global fallback authorization policy** (authenticated-by-default) and make every genuinely public endpoint **explicitly** `[AllowAnonymous]` after review.
- Ship behind a config switch so enforcement can be validated in non-production first and rolled back instantly.

## Capabilities

### New Capabilities
- `resource-authorization`: the contract that state-changing commands targeting an owned resource must verify the authenticated caller owns it (or holds an authorized role), returning 403 otherwise; the actor identity is always server-derived; public endpoints are explicit.

### Modified Capabilities
- `customer-booking-journey`: cancel and reschedule now REQUIRE the caller to own the booking (or be the provider/admin).

## Impact

- **Code**: new `AuthorizationBehavior` + ownership requirement markers in `Booksy.Core.Application`; `CancelBookingCommand`/`RescheduleBookingCommand`/`RefundPaymentCommand` carry a server-populated `ActingUserId`; controllers stop reading actor fields from the body; global fallback policy in `Program.cs`.
- **API**: unauthorized callers now receive **403** instead of 200; body actor fields deprecated (ignored). No change for legitimate owners/providers/admins.
- **Flutter**: remove any client-sent `cancelledBy`; otherwise no functional change.
- **DB**: none.
- **Security**: closes two confirmed IDORs and one authorization-boundary gap; removes a spoofable audit trail.
- **No impact**: business rules, pricing, data model.
