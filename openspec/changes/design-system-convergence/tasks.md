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

- [ ] 3.1 Create `core/widgets/app_card.dart` (`AppCard` — white, r15, 1px `AppColors.border`, elevation 0, 12dp interior) with widget tests (flat, border, radius, padding)
- [ ] 3.2 Create `core/widgets/app_info_card.dart` (`AppInfoCard` — top tag strip + 40×40 r8 tinted icon container + muted-label/navy-value column on an `AppCard` body) with widget tests
- [ ] 3.3 Create `core/widgets/app_list_row.dart` (`AppListRow` — `surfaceSoft` fill, r10, min-height 48, `AppColors.icon` leading, navy title, muted subtitle, chevron-when-tappable, bounded ripple) with widget tests incl. ≥48dp hit-target assertion
- [ ] 3.4 Create `core/widgets/app_section_header.dart` + `core/widgets/app_inline_add_button.dart` (bold navy title, trailing green icon+label add action, shrink-wrapped target) with widget tests
- [ ] 3.5 Create `core/widgets/app_status_badge.dart` (`AppStatusBadge` — r6, 12×6 padding, 12sp w600, success/warning/danger/neutral variants; neutral = border bg + navy text) with widget tests per variant
- [ ] 3.6 Create `core/widgets/app_icon_button.dart` (`AppIconButton` — 44×44 visual, r12, 20dp glyph, primary-tinted 4–5% splash, optional badge slot, ≥48dp gesture target) with widget tests (badge layout stability, target size)
- [ ] 3.7 Create `core/widgets/app_dashed_divider.dart` (5px dash segments, `AppColors.divider`) with a widget test
- [ ] 3.8 First consumers: preview step sections → `AppInfoCard`; preview price-summary separators → `AppDashedDivider`; services step list rows → `AppListRow`; services step header + add-service affordance → `AppSectionHeader` + `AppInlineAddButton`; app-bar logout actions (wizard, dashboard) → `AppIconButton`. Update affected step tests (presentation assertions only — keep existing Keys/behavior green)
- [ ] 3.9 Run the quality gate

## 4. Button system extension + overlays

- [ ] 4.1 Extend `core/widgets/app_button.dart` with `ButtonSize {big, dialog, small}` (46/17, 40/15.5, 30/14) and variants primary/secondary(outlined)/destructive/text; keep full-width default, loading-contrast rule, and backward-compatible existing constructors; extend its widget tests (all sizes × variants, destructive fill, loading contrast per variant)
- [ ] 4.2 Create `core/widgets/app_dialog.dart` (`AppDialog` + `showAppDialog()` — r16 flat white panel, `0x24000000` barrier, optional header with centered navy title + close + divider, `ButtonSize.dialog` action row, max-width constraint) with widget tests (barrier color, dismissal paths, action sizing, wide-viewport width cap)
- [ ] 4.3 Create `core/widgets/app_bottom_sheet.dart` (`showAppBottomSheet()` — r14 top, `0x47000000` barrier, slide-up on `AppMotion.curve`/`reverseCurve`, keyboard-aware padding within 200ms, optional heightFactor) with widget tests (motion curves via token assertions, keyboard inset behavior)
- [ ] 4.4 Wire the first dialog consumer: logout confirmation (wizard + dashboard logout actions) via `showAppDialog` with a destructive confirm — presentation wrapper only, same `LogoutRequested` dispatch; add widget test
- [ ] 4.5 Run the quality gate

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
