# Design — Design System Convergence Audit

## Context

The brand reskin (2026-07-15) put `booksy-provider-app` on the Coliride palette, radius scale, and button/input theming via `lib/config/theme/app_tokens.dart` + `app_theme.dart`, guarded by `test/config/theme/app_theme_test.dart`. This document goes one level deeper: a component/interaction-level audit of Booking against the Coliride Flutter implementation (`C:\Repos\Coliride\FrontendClient`, read-only reference), producing an **adopt / adapt / keep** decision per pattern.

**Coliride's shared library** (evidence, `lib/core/presentation/widgets/` + `lib/core/widget/`):

- `button.dart` + `button_foundation.dart` — one `Button` with `ButtonSize {small(h30/fs14), dialog(h40/fs15.5), medium(h46/fs16), big(h46/fs17)}`, primary/secondary/destructive variants, prefix/suffix icons, loading spinner replacing the label, `expand` full-width default.
- `loading.dart` (`AppLoadingWidget` — Cupertino spinner + optional message, responsive radius 10–16), `dot_loading_widget.dart` (progressive dots in primary color).
- `error.dart` (`ErrorViewWidget` — grey icon 50 + grey message + optional pill outlined retry).
- `no_result.dart` / `no_result_text_widget.dart` — illustration + navy bold caption + small retry `Button`, r15 container.
- `status_badge.dart` — r6 pill, 12/6 padding, fs12 w600, color-coded background.
- `action_button.dart` — 44×44 icon button, r12, primary-tinted hover/splash at 4–5% opacity, optional count badge.
- `add_inline_button.dart` — green (+) icon+label inline text button for "add another X".
- `toggle_button.dart` — selectable chip-button: grey `#7F8696` idle, blue selected, green + `#E9FFF6` fill when completed.
- `app_tab_bar.dart` — green label + 2px green underline indicator, navy unselected, `#EBEEF3` divider.
- `info_card_with_tag.dart` — card with attached top tag (r12-top `#EBEEF3` tab) + white r12 bordered body + 40×40 r8 tinted icon container + label/value column.
- `separator.dart` — dashed divider (5px dashes) for "receipt" contexts; solid `#E5E9F2` divider elsewhere.
- `modal/base_dialog.dart` — `DialogPage` with light `0x24000000` barrier; `BaseModalAppBar` (title + close action + optional divider).
- `modal/bottom_sheet_page.dart` — bottom sheet route: slide-up `easeOutCubic`/`easeInCubic`, `0x47000000` barrier, keyboard-aware `AnimatedPadding` (200ms), optional `heightFactor`.
- `allert_banner/` — inline alert banner: leading icon, bold title + message, optional action link, trailing chevron.
- `selection_trigger_field.dart` — dropdown trigger with `AnimatedRotation` (180ms) chevron.
- `responsive.dart` / `ResponsiveWidget` — `DeviceScreenType {watch, mobile, tablet, desktop}` with `responsiveValue<T>()` per breakpoint.
- Profile/menu list rows — `#FAFAFA` fill (`profileItemFillColor`), navy title, `#96A0B3` description, `#D2DBEB` icon, r10 items.

**Booking's current state** (`booksy-provider-app/lib/core/widgets/` + screens):

- `AppButton` (filled/text only — no sizes, no icons, no secondary/destructive), `AppTextField`, `OtpInput`, `AppSnackbar`, `StepScaffold`.
- Screens hand-roll everything else: raw `CircularProgressIndicator`s (splash, city loader, map geocoding), inline `Text` errors, ad-hoc `Container(borderRadius: AppRadius.md)` selection tiles (category step), bare `Switch` rows (working hours), no empty-state pattern (gallery step is a plain placeholder), no dialogs/bottom sheets yet, no list-row or section-header component, no responsive layer (mobile-only assumption).

**Constraint**: presentation only — no business logic, flows, or screen hierarchy changes. Coliride domain widgets (seats, vehicle, pick-up point, book date/time) are out of bounds.

## Goals / Non-Goals

**Goals:**

- A complete adopt/adapt/keep decision matrix across composition, rhythm, spacing, hierarchy, cards, lists, forms, section headers, dialogs, bottom sheets, the four feedback states, progress, navigation, motion, elevation, shadows, dividers, icons, density, and responsive behavior.
- A structural token layer (`AppMotion`, icon sizes, density paddings, elevation policy) extending — not replacing — the brand tokens.
- A shared component inventory for Booking whose visuals are pixel-compatible with Coliride's, with contracts defined in the capability specs.

**Non-Goals:**

- No changes to blocs/cubits, routing, domain, or API code; no flow or screen-hierarchy changes.
- No Coliride package import or code sharing — patterns are re-implemented against Booking's tokens (Coliride hardcodes hex inline; Booking routes everything through `AppColors`).
- No dark theme (Coliride's dark palette is effectively a copy of light; deferred until Booking ships dark mode).
- No Vue app changes (rebranded separately) and no desktop-class layouts (see D7).

## Decisions

### D1 — Layering: structural tokens extend the existing theme, components consume both

Add structural constants to `app_tokens.dart` (`AppMotion` durations/curves, `AppIconSize`, density paddings, barrier colors) and keep `app_theme.dart` the single `ThemeData` authority. New components live flat in `core/widgets/` beside the existing five, one file per component, prefixed `App*` (Booking's existing convention — not Coliride's mixed naming).
*Alternative rejected*: a separate `design_system/` package/folder — overkill for one app, and splitting tokens across two locations invites drift.

### D2 — Audit decision matrix

Legend: **ADOPT** = take the Coliride pattern unchanged (values and behavior); **ADAPT** = same visual family, adjusted for the booking domain; **KEEP** = intentionally different, with the business reason.

| # | Dimension | Coliride pattern (evidence) | Booking today | Decision & rationale |
|---|-----------|-----------------------------|---------------|----------------------|
| 1 | Component composition | Small `App*`/foundation widgets composed per screen; variants via params, not subclasses | 5 widgets, screens hand-roll the rest | **ADOPT** the philosophy: every pattern used ≥2× becomes a `core/widgets` component with variant params |
| 2 | Layout rhythm | 16 side padding on mobile content, 12 intra-card padding, 24 between form groups | `AppSpacing.lg=24` page padding, `md=16` between fields (StepScaffold) | **KEEP** Booking's 24 page padding (wizard forms breathe better for data-entry-heavy screens); **ADOPT** 12 intra-card padding |
| 3 | Spacing system | Ad-hoc numerics via `.gap` extension (2,5,6,10,12,30,80…) | Disciplined 4/8/16/24/40 scale | **KEEP** Booking's stricter scale — it is the better system; Coliride values snap to it (5→4, 6→8, 10→8, 12→md-intra=12 allowed as card-internal exception) |
| 4 | Visual hierarchy | Navy bold titles, `#96A0B3` secondary text, fs12 metadata, bold-label-over-value pairs | Theme headlineSmall/bodyMedium only; no secondary-text token in use | **ADOPT**: `AppColors.muted` for secondary text everywhere; title/subtitle/metadata triple as the standard hierarchy |
| 5 | Cards | White, r12–15, 1px `#E5E7EB≈border` border, elevation 0; `InfoCardWithTag` adds top tag + 40×40 r8 tinted icon container | No card component; theme `cardTheme` (r15, border) exists but unused | **ADOPT** `AppCard` (r15, border, flat, 12 padding); **ADAPT** `InfoCardWithTag` → `AppInfoCard` for preview-step summaries (tag = section name, icon container = section icon) — booking wizard preview is the natural consumer |
| 6 | Lists | Menu rows: `#FAFAFA` fill, r10, navy title, muted subtitle, `#D2DBEB` leading icon, chevron trailing | Ad-hoc `ListTile`s / hand-built rows (services step) | **ADOPT** `AppListRow` with those exact values; services/staff/working-hours rows become its consumers |
| 7 | Form layouts | Label-above outline fields, helper below, 55px field zone, dropdown triggers with animated chevron | Matches (theme reskin); city dropdown chevron is static | **ADOPT** `AnimatedRotation` 180ms chevron on the city selector; otherwise already converged |
| 8 | Section headers | Bold navy fs14–16 label, optional green inline "add" action on the trailing edge (`AddInlineButton`) | Plain `Text(headlineSmall)` inside steps | **ADAPT** `AppSectionHeader(title, action?)` — trailing action slot takes an `AppInlineAddButton`; services step ("add service") is the first consumer |
| 9 | Dialogs | `DialogPage`, light `0x24000000` barrier, `BaseModalAppBar` (centered navy title + close + optional divider), r16 panel, `ButtonSize.dialog` (h40) actions | None yet (logout confirm et al. will need them) | **ADOPT** wholesale: `AppDialog` shell + barrier + h40 action row; theme `dialogTheme` (r16) already matches |
| 10 | Bottom sheets | Slide-up route: `easeOutCubic` in / `easeInCubic` out, `0x47000000` barrier, keyboard-aware 200ms `AnimatedPadding`, r14 top, optional heightFactor | Theme r14 top exists; no sheet helper, no usage yet | **ADOPT** `showAppBottomSheet()` wrapping `showModalBottomSheet` with those curves/barrier; skip Coliride's custom `Page`-based routing (Booking's go_router setup doesn't route modals — routing architecture is out of scope) |
| 11 | Empty states | Illustration + navy bold fs16 caption + small retry button, r15 surface | None (gallery step ad-hoc placeholder) | **ADAPT** `AppEmptyState(icon, message, action?)` — icon-based (72, `AppColors.icon`) instead of Coliride's proprietary illustration asset we can't ship; same composition and caption style |
| 12 | Loading states | `AppLoadingWidget` (Cupertino spinner r13–16 + optional grey message), `DotLoadingWidget` for inline/button contexts | Raw Material `CircularProgressIndicator` ×4 call sites | **ADAPT**: one `AppLoading(message?, size)` component, but **Material spinner, not Cupertino** — Booking is Android-first (Iran market) and the M3 spinner matches its Material chrome; centralizing the component is what buys consistency |
| 13 | Error states | `ErrorViewWidget`: grey icon 50 + grey message + outlined pill retry | Inline `Text` + ad-hoc retry (cities load failure) | **ADAPT** `AppErrorState(message, onRetry?)` — same composition, but retry uses Booking's r10 outlined button (Coliride's r30 pill is off-scale even in its own system) and `AppColors.muted` instead of raw greys |
| 14 | Success states | Green EasyLoading toast (green bg, white icon/text); green check emphasis | Green `AppSnackbar.success` (reskin), green completion icon | **ADOPT** (already converged); snackbar stays the toast mechanism — no EasyLoading dependency |
| 15 | Progress indicators | Dot loaders; no linear step bar (Coliride has no wizard) | Linear green-on-blue step bar in wizard app bar | **KEEP** the wizard's linear step progress — booking onboarding is an 8-step flow with no Coliride analogue; green-on-blue already speaks the brand |
| 16 | Navigation patterns | Blue app bar, white foreground, centered title; white back-icon builder via `actionIconTheme`; bottom nav for main shell | Blue app bar (reskin); default back icon; no bottom nav (single-flow app) | **ADOPT** centered-title + white back treatment (add `actionIconTheme` parity); **KEEP** no bottom nav — provider app is a wizard + dashboard, a tab shell would be invented structure |
| 17 | Motion & animations | 180ms chevron rotation, 200ms keyboard padding, ~250–300ms sheet slide `easeOutCubic`/`easeInCubic`; no gratuitous motion | None defined | **ADOPT** as `AppMotion` tokens: `fast=180ms`, `medium=250ms`, `curve=easeOutCubic`, `reverseCurve=easeInCubic`; all new animated affordances draw from these |
| 18 | Elevation | 0 everywhere; `RawMaterialButton` with all elevations pinned to 0 | Theme sets elevation 0 on buttons/cards/dialogs (reskin) | **ADOPT** as an explicit policy: elevation 0 across all components; the only permitted "depth" is the modal barrier |
| 19 | Shadows | None; depth via borders + barrier dim | None post-reskin | **ADOPT** borders-over-shadows as a written rule (spec-level guard so future components don't regress) |
| 20 | Dividers | Solid `#E5E9F2` 1px standard; dashed `Separator` only for receipt-like summaries | Theme dividerTheme `#E5E9F2` (reskin); no dashed variant | **ADOPT** solid divider (done); **ADAPT** dashed `AppDashedDivider` reserved for the preview step's price-summary block — the one receipt-like context in booking |
| 21 | Icon usage | Custom ColiRide icon font; 16 inline / 20–24 functional / 44 touch container; `#D2DBEB` decorative, navy functional | Material rounded icons; sizes ad-hoc (18/40/50/72/88) | **KEEP** Material rounded icons (ColiRide font is proprietary + domain glyphs); **ADOPT** the size ramp as `AppIconSize {sm=16, md=24, action=44 container/20 glyph, hero=72}` and the decorative-vs-functional color rule |
| 22 | Density | Compact rows (menu ~48–56px), 12px intra-card, 44×44 icon targets | Airier forms (46 buttons, 55 fields), default ListTile density | **ADAPT**: forms keep Booking's data-entry density; lists/rows adopt Coliride's compact 48–56px rows with 44×44 targets (≥48dp a11y floor respected via row min-height 48) |
| 23 | Responsive behavior | Full `DeviceScreenType` watch→desktop framework, per-breakpoint values, desktop layouts | None; mobile-only | **KEEP** mobile-only for the provider app (target market is Android phones; no desktop requirement in the roadmap) — but components must not hardcode widths so a future tablet pass stays cheap. Revisit if a tablet/web provider surface lands |
| 24 | Status/selection chips | `StatusBadge` (r6, fs12 w600, tinted bg); `ToggleButton` (grey idle → blue selected → green `#E9FFF6` completed) | Category step hand-rolls selection tiles; no status badge | **ADOPT** `AppStatusBadge` verbatim (provider/service/booking statuses map perfectly); **ADAPT** the toggle's three-state color logic into the category-step selection tiles (idle/selected; "completed" state reserved for step indicators later) |

### D3 — Component inventory (what gets built)

New in `core/widgets/`, each with widget tests: `AppCard`, `AppInfoCard`, `AppListRow`, `AppSectionHeader`, `AppInlineAddButton`, `AppStatusBadge`, `AppIconButton` (44×44, r12, tinted splash, badge slot), `AppLoading`, `AppEmptyState`, `AppErrorState`, `AppDialog` + `showAppDialog()`, `showAppBottomSheet()`, `AppDashedDivider`. Extended: `AppButton` gains `ButtonSize {small, dialog, big}` + `secondary`/`destructive` variants + optional icon (subsuming Coliride's `Button` API at Booking's scale — `medium` dropped, it duplicates `big` at h46 and only differs fs16 vs fs17). `AppSnackbar` and `OtpInput` unchanged. `StepScaffold` stays but its title/subtitle block becomes `AppSectionHeader` internally.

*Alternative rejected*: porting Coliride files 1:1 — they hardcode hex, mix naming conventions, and carry ride-sharing coupling (`context.strings`, responsive shell deps) that Booking must not inherit.

### D4 — Screens migrate opportunistically, primitives land first

This change lands tokens + components + tests, and migrates the existing call sites that already violate the system (4 raw spinners → `AppLoading`; cities-failed inline error → `AppErrorState`; gallery placeholder → `AppEmptyState`; category tiles → toggle-state colors; city chevron → animated). Dashboard and future screens consume the system as they're built — no big-bang rewrite of working screens.

### D5 — Testing strategy

Per component: rendering, variant/state coverage (disabled/loading/selected/error), a11y (semantics label, ≥48dp effective target), RTL rendering. The theme guard test grows assertions for `AppMotion`/`AppIconSize`/density constants. Migrated call sites keep their existing behavior tests green (location_step_test, provider_login_page_test untouched in behavior). No golden tests yet — no baseline infrastructure exists; noted as a follow-up.

## Risks / Trade-offs

- [Values transcribed from Coliride code may not match the Figma exactly] → Code is the shipped truth and the memory notes Figma≈code for Coliride; where they conflict, code wins. Spot-check contested components (dialog, badge) against Figma before spec sign-off.
- [Component sprawl — building widgets nothing uses] → Every component in D3 has a named first consumer in the current app (see matrix); anything without one (e.g., `AppTabBar`) is deferred to the spec as "define contract only, build when the dashboard needs tabs."
- [Migrating category/services steps could break existing widget tests] → Presentation-only edits behind the same Keys/semantics; run the full suite per migration; tests updated only where they assert old ad-hoc visuals.
- [Coliride evolves and the systems drift again] → The capability specs become the contract; the memory note records that Coliride is mined per-pattern, not tracked continuously. Divergence after this change is intentional unless a new audit is commissioned.
- [Compact list density vs. accessibility] → Row min-height floors at 48dp even in compact mode; the 44×44 icon-button ships a ≥48dp gesture target via `tapTargetSize` padding.

## Migration Plan

1. Structural tokens (`AppMotion`, `AppIconSize`, density/barrier constants) + theme additions (`actionIconTheme`) — theme guard test extended.
2. Feedback-state components (`AppLoading`, `AppEmptyState`, `AppErrorState`) + migrate the 4 spinner call sites, cities-failed error, gallery placeholder.
3. Structure components (`AppCard`, `AppInfoCard`, `AppListRow`, `AppSectionHeader`, `AppInlineAddButton`, `AppStatusBadge`, `AppIconButton`, `AppDashedDivider`) + first consumers (preview step cards, services rows/header).
4. Overlays (`AppDialog`, `showAppBottomSheet`) + `AppButton` size/variant extension (dialog actions consume `ButtonSize.dialog`).
5. Selection-state adaptation in category step; animated chevron on city selector.

Each step is independently shippable and revertible (pure presentation); `flutter analyze` + full test suite gate every step. Rollback = revert the step's commit; no data/API surface.

## Open Questions

- Empty-state artwork: icon-only now, or commission a Booking illustration set to match Coliride's illustrated empty states? (Ships icon-only; slot accepts a widget so art can drop in later.)
- Should the Vue apps eventually consume this same matrix? (Out of scope here; the matrix format is reusable — flag to the design-system memory.)
- `AppTabBar` (green underline) — contract is specced from Coliride, but no Booking consumer exists until the dashboard epic. Build now or on first use? (Design says: on first use.)
