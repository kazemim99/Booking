# Tasks — Design System Convergence

All work is presentation-only inside `booksy-provider-app/`. Every numbered group ends with the quality gate: `flutter analyze` clean + `flutter test --exclude-tags live` fully green. Each group is independently shippable/revertible.

## 1. Structural foundations (tokens + theme)

- [x] 1.1 Add `AppMotion` (fast=180ms, medium=250ms, curve=easeOutCubic, reverseCurve=easeInCubic), `AppIconSize` (sm=16, md=24, action=20, hero=72), intra-card padding token (12), and overlay barrier colors (dialog `0x24000000`, sheet `0x47000000`) to `lib/config/theme/app_tokens.dart`
- [x] 1.2 Add global white back icon via `actionIconTheme` to `lib/config/theme/app_theme.dart` (spec: app bar chrome)
- [x] 1.3 Extend `test/config/theme/app_theme_test.dart` with assertions for all new structural tokens and the `actionIconTheme` back-icon treatment
- [x] 1.4 Run the quality gate (analyze + full suite)

## 2. Feedback-state components + call-site migration

- [x] 2.1 Create `core/widgets/app_loading.dart` (`AppLoading` — Material spinner in brand primary, optional muted message, inline/centered) with widget tests (rendering, message, RTL)
- [x] 2.2 Create `core/widgets/app_empty_state.dart` (`AppEmptyState` — hero icon slot at 72dp `AppColors.icon`, bold navy 16sp caption, optional muted description, optional small action button) with widget tests (with/without action, custom icon widget slot)
- [x] 2.3 Create `core/widgets/app_error_state.dart` (`AppErrorState` — 50dp muted warning icon, muted message, optional r10 outlined retry) with widget tests (retry tap fires callback, no-retry variant)
- [x] 2.4 Migrate raw `CircularProgressIndicator` call sites to `AppLoading`: splash page, location step city loader, location step geocoding overlay (AppButton's internal spinner stays as-is)
- [x] 2.5 Migrate the location step cities-failed inline error to `AppErrorState` with a retry that re-triggers `_loadCities`; extend `location_step_test.dart` to cover loading, failure+retry, and loaded states (spec: screen state coverage). Note: also fixed a latent race in the test harness (unawaited async `getIt.reset()` in setUp)
- [x] 2.6 Migrate the gallery step placeholder to `AppEmptyState` (gallery icon + caption; NO upload action — image upload is an unimplemented tracked follow-up, so the spec's upload-action scenario applies once that feature lands); added `gallery_step_test.dart`
- [x] 2.7 Run the quality gate

## 3. Structure components (cards, rows, headers, badges, icon buttons)

> **2026-09-05 finding**: every widget in this group already existed (commit `df1433c5`,
> 2026-07-15 — predates this change), with full widget-test coverage in
> `test/core/widgets/design_system_components_test.dart`/`feedback_states_test.dart`, and each
> one verified against the spec values below (radius/padding/color/size all matched exactly on
> inspection — nothing needed correcting). Only §3.8 (wiring them into real screens) was
> outstanding; the rest of this group is verification, not new work.

- [x] 3.1 `core/widgets/app_card.dart` (`AppCard` — white, r15, 1px `AppColors.border`, elevation 0, 12dp interior) — verified against spec, pre-existing
- [x] 3.2 `core/widgets/app_info_card.dart` (`AppInfoCard` — top tag strip + 40×40 r8 tinted icon container + muted-label/navy-value column on an `AppCard` body) — verified against spec, pre-existing
- [x] 3.3 `core/widgets/app_list_row.dart` (`AppListRow` — `surfaceSoft` fill, r10, min-height 48, `AppColors.icon` leading, navy title, muted subtitle, chevron-when-tappable, bounded ripple) — verified against spec, pre-existing
- [x] 3.4 `core/widgets/app_section_header.dart` (`AppSectionHeader` + `AppInlineAddButton`, bold navy title, trailing green icon+label add action, shrink-wrapped target) — verified against spec, pre-existing
- [x] 3.5 `core/widgets/app_status_badge.dart` (`AppStatusBadge` — r6, 12×6 padding, 12sp w600, success/warning/danger/neutral variants) — verified against spec, pre-existing
- [x] 3.6 `core/widgets/app_icon_button.dart` (`AppIconButton` — 44×44 visual, r12, 20dp glyph, primary-tinted press feedback, optional badge slot, ≥48dp gesture target) — verified against spec, pre-existing
- [x] 3.7 `core/widgets/app_dashed_divider.dart` (5px dash segments, `AppColors.divider`) — verified against spec, pre-existing
- [x] 3.8 **DONE THIS TURN.** First consumers: preview step's 5 sections (`preview_step.dart`) → `AppInfoCard` (one icon per section: storefront/category/location/design_services/access_time; the edit `TextButton` moved to `AppInfoCard.trailing`); services step (`services_step.dart`) list rows → `AppListRow` (leading icon + delete trailing), header + add-service affordance → `AppSectionHeader` + `AppInlineAddButton` (replacing the standalone `OutlinedButton.icon`, same `onboarding-add-service` Key preserved); app-bar logout actions in `onboarding_wizard_page.dart` and `provider_dashboard_page.dart` → `AppIconButton`. Added `test/features/onboarding/services_step_test.dart` (3 tests: empty state, rows render name+subtitle, delete removes from cubit state) — no prior test file existed for this step. **Not migrated**: "preview price-summary separators → `AppDashedDivider`" — `preview_step.dart` has no price-summary/separator content today (each service row already shows its own price inline); this half of §3.8 has no current target and is not a defect, just inapplicable until such a summary section is built.
- [x] 3.9 **DONE.** `flutter analyze`: no issues (whole project). Targeted test run (services/gallery/location/working-hours steps + both design-system test files): 57/57 passed.

## 4. Button system extension + overlays

> **2026-09-05 finding**: §4.1 was also already done pre-existing (verified below). §4.2/§4.3 are
> functionally satisfied by two *differently-named* pre-existing widgets — `AppDialogHeader`
> (a header component, composed into a call site's own `AlertDialog`) and `AppSheetScaffold` +
> `showAppSheet()` (a full sheet scaffold + presenter, keyboard-aware, r14 top, matching barrier
> token) — rather than the spec's literal `AppDialog`/`showAppDialog()`/`app_bottom_sheet.dart`/
> `showAppBottomSheet()` names. Recommendation: keep the existing names (they're already covered
> by `design_system_components_test.dart` and would require touching every call site to rename)
> and treat this as the convergence's actual outcome rather than adding parallel same-purpose
> widgets under new names — that would fragment the design system into two competing dialog/sheet
> conventions, which is the opposite of what this change is for. Flagging rather than deciding
> unilaterally: this is a naming/documentation call, not a behavior change, so low risk either way,
> but it's the kind of decision worth a explicit nod before more call sites build on one name.

- [x] 4.1 `core/widgets/app_button.dart` already has `AppButtonSize {big, dialog, small}` (+ an extra `medium`) at the exact spec heights/sizes (46/17 big, 40/15.5 dialog, 30/14 small) and all four role variants (primary/secondary/destructive/text); full test coverage in `design_system_components_test.dart` (`AppButton roles` + `AppButton size ramp` groups, 8 tests) — verified against spec, pre-existing
- [x] 4.2 Satisfied by `AppDialogHeader` (40px band, centered 18-bold navy title, trailing close disc, optional divider) — see naming note above. Not a literal `AppDialog`/`showAppDialog()` wrapper.
- [x] 4.3 Satisfied by `AppSheetScaffold` + `showAppSheet()` (r14 top via theme, `AppColors.sheetBarrier` barrier, keyboard-inset-aware padding, 90%-height cap, modal + picker variants) — see naming note above. Not a literal `app_bottom_sheet.dart`/`showAppBottomSheet()`.
- [x] 4.4 **DONE THIS TURN.** Added `core/widgets/app_confirm_dialog.dart` (`showAppConfirmDialog` — the actual "first dialog consumer": zero prior call sites used `AppDialogHeader` before this) composing `AppDialogHeader` + a two-`AppButton(size: dialog)` action row (secondary cancel / destructive confirm by default, `destructive: false` for a primary-role variant), barrier explicitly set to `AppColors.dialogBarrier` (`showDialog`'s own default is `Colors.black54`, not the token). Added `features/auth/presentation/widgets/confirm_logout.dart` (`confirmAndLogout`) as the one shared helper both app-bar logout sites now call, so the dialog only dispatches `LogoutRequested` when the user actually confirms — replacing the previous immediate-fire behavior in `onboarding_wizard_page.dart` and `provider_dashboard_page.dart`. New strings: `logoutConfirmTitle`, `logoutConfirmBody`, generic `confirm`. **Scope note**: two *other* existing logout entry points (`home_page.dart`'s account-sheet `ListTile`, `more_page.dart`'s account-section row) still fire immediately, unchanged — the spec's §4.4 text names only "wizard + dashboard logout actions," so unifying those two as well would be scope creep beyond this task; flagging as a candidate follow-up rather than doing it silently.
- [x] 4.5 **DONE.** `flutter analyze`: no issues. Full suite (`flutter test --exclude-tags live`): 409/409 passed, including 4 new tests in `app_confirm_dialog_test.dart` and 3 new tests in `provider_dashboard_page_test.dart` (dialog appears without logging out, cancel does not log out, confirm dispatches `LogoutRequested` exactly once).

## 5. Selection states + motion polish

- [ ] 5.1 Apply the three-state selection colors to the category step tiles (idle grey `#7F8696` / selected blue border+text) with an `AppMotion.fast` animated transition; keep existing selection behavior and Keys; update category step tests
- [ ] 5.2 Add the `AnimatedRotation` (180ms) chevron to the location step's city selector field, rotating with inline-list open/close; extend `location_step_test.dart`
- [ ] 5.3 Sweep shared components for literal `Duration`/curve/spacing values that must reference tokens (specs: structural tokens, spacing scale); fix any stragglers
- [ ] 5.4 Run the quality gate

## 6. Verification & documentation

- [ ] 6.1 Verify every spec scenario has a corresponding passing test (map scenario → test name per capability); add any missing coverage
- [ ] 6.2 Confirm no feature screen contains raw `CircularProgressIndicator`, `BoxShadow`, or ad-hoc hex colors (grep audit per the foundations spec guards)
- [ ] 6.3 Run the app on an emulator and eyeball the migrated screens (wizard steps, dialogs, sheets) — visual QA of what tests can't see
- [ ] 6.4 Update the `coliride-design-reference` memory note with the convergence outcome; note the deferred items (AppTabBar on first use, illustrations, golden tests, dark mode)
- [ ] 6.5 Final full gate: `flutter analyze` + `flutter test --exclude-tags live` green across the suite
