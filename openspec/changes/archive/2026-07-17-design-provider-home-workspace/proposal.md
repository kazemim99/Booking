## Why

The Flutter provider app currently lands a freshly-onboarded provider on a placeholder dashboard (`ProviderDashboardPage` — a centered "welcome" string). The approved Provider App Information Architecture (`booksy-provider-app/PROVIDER_APP_INFORMATION_ARCHITECTURE.md`) establishes that the app's home must be an **operational "Today" workspace** that answers *"What do I need to do right now?"* — not a static welcome screen and not an analytics dashboard. We now need a design specification for that Home before any screen is built.

The Home is not one screen; it is a screen that must **adapt to the provider's business maturity and daily context** (a day-one pending business, a growing business with no traction yet, and a fully-booked established salon all need a different top-of-screen). Without a specification enumerating every Home state and the rules that compose them, implementation would harden a single static layout that fails most of these situations.

## What Changes

- Define the **Provider Home (Today Workspace)** as a distinct, adaptive capability for the Flutter provider app — separate from the existing Vue web `provider-dashboard`.
- Enumerate **every Home state** and the required behavior for each: first login after onboarding, empty business, no appointments, pending verification, active business day, fully booked day, closed business, offline, error, and vacation mode.
- For each state, specify: **user goals, information priority, available actions, empty states, success states, error states, and visual hierarchy.**
- Define a **three-layer state model** (system → lifecycle/availability → maturity/day-context) and a **precedence/resolution rule** for composing the Home when multiple conditions apply simultaneously (e.g. offline *and* pending verification).
- Define a **business-maturity adaptation model** (Setup → Growth → Traction → Operational) with the signals that classify a provider and the rule for how the Home's emphasis shifts — the scaffold/nudge region recedes as the operational agenda region grows.
- Establish the Home's **zone composition** (app bar, status/banner rail, action queue, now/next, agenda, coming-up peek, activation/nudges) and how each zone appears, adapts, or collapses per state.
- Reuse the **Coliride visual language** (design tokens, spacing, typography, components, interaction/motion patterns) at the specification level; no business flows are copied.

This change produces a **design specification and user-flow definition only**. It does **NOT** implement Flutter code, produce high-fidelity mockups, or design the other bottom-nav tabs (Calendar, Clients, More). Those are separate, subsequent changes.

## Capabilities

### New Capabilities
- `provider-home-workspace`: The adaptive Flutter provider-app Home ("Today") screen — its state model, per-state behavior (goals, information priority, actions, empty/success/error treatments, visual hierarchy), zone composition, state-resolution precedence, and business-maturity adaptation rules.

### Modified Capabilities
<!-- None. The existing `provider-dashboard` spec covers the Vue web admin dashboard (sidebar + tabbed Figma redesign) on a different platform with a deliberately different IA; it is not modified by this mobile Home design. -->

## Impact

- **New spec**: `openspec/specs/provider-home-workspace/spec.md` (created via this change's delta).
- **Design doc**: a companion Home design document under `booksy-provider-app/` (states catalog + adaptive composition + zone spec), building on the approved `PROVIDER_APP_INFORMATION_ARCHITECTURE.md`.
- **Downstream code (future changes, not this one)**: `booksy-provider-app/lib/features/auth/presentation/pages/provider_dashboard_page.dart` (placeholder to be replaced by the Home feature), a new `home` feature module, and app-router landing behavior (`app_router.dart` already routes authenticated + `PendingVerification` providers to the dashboard route).
- **Backend dependency check**: Home surfaces map to shipped endpoints (provider bookings, confirm/cancel/complete/no-show/reschedule, booking statistics, provider status). Flagged gaps (provider-scoped client list, push infrastructure, reviews) are non-blocking for the design and are recorded for scoping.
- **No breaking changes**; no runtime behavior changes in this artifact-only change.
