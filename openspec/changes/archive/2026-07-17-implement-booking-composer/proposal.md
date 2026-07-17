## Why

The Home's center-docked ⊕ ("نوبت جدید") currently shows "coming soon" — yet adding a walk-in/phone booking is the #1 proactive provider action in the approved IA (two-tap promise). Feasibility work also uncovered a **product-level blocker**: services created via onboarding carry no `BookingPolicy`, so `CreateBookingCommandHandler` falls back to `BookingPolicy.Default`, which hardcodes `requireDeposit: true (20%)` — in a product with **no payment flow**. Every booking in the system is therefore born permanently unconfirmable (`BOOKING_DEPOSIT_NOT_PAID`, verified live 2026-07-15), which kills both the Request-mode approval queue and any booking the composer would create.

## What Changes

- **Backend fix**: `BookingPolicy.Default` stops requiring a deposit (`requireDeposit: false, depositPercentage: 0`) so bookings are confirmable while no payment system exists. Domain unit test updated accordingly. (Services that explicitly set a deposit-requiring policy keep it — `Strict` is unchanged.)
- **Flutter booking composer**: full-screen flow from ⊕ / empty-agenda / GetDiscovered walk-in CTA: pick service → pick staff → pick date → pick an available slot (live `GET /Bookings/available-slots`) → optional walk-in client name/phone + notes → `POST /Bookings` → Home refreshes.
- **Walk-in identity (MVP convention)**: the booking is created under the provider's own account with the walk-in client's name/phone carried in `customerNotes`. A first-class walk-in customer concept is flagged as future backend work (same gap as customerName enrichment).

## Capabilities

### New Capabilities
- `provider-booking-composer`: the provider-side booking creation flow — inputs, slot selection, walk-in convention, confirmability, and failure handling.

### Modified Capabilities
<!-- None. provider-home-workspace already specifies the ⊕ create action opening "New appointment"; this implements its target. -->

## Impact

- Backend: `BookingPolicy.cs` (Default), `BookingPolicyTests.cs`; behavior verified live (create → confirm now succeeds).
- Flutter: new composer page/cubit under the home feature, new endpoints in `ApiConstants` (staff, available-slots, create), `HomeApiService`/`HomeRepository` additions, ⊕ wiring in `home_page.dart`, `AppStrings` composer section, cubit + widget + parser tests.
- No breaking changes; deposit-requiring services (explicit policy) unaffected.
