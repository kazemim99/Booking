## 1. Consolidate the design deliverable

- [x] 1.1 Author the companion Home design doc `booksy-provider-app/PROVIDER_HOME_TODAY_DESIGN.md` synthesizing the IA (`PROVIDER_APP_INFORMATION_ARCHITECTURE.md`), the state catalog (`specs/provider-home-workspace/spec.md`), and the architecture (`design.md`) into one reviewable document
- [x] 1.2 Cross-link the companion doc from `PROVIDER_APP_INFORMATION_ARCHITECTURE.md` (Related design documents section)
- [x] 1.3 Verify no contradictions between the OpenSpec artifacts and the companion doc (terms aligned: zones, 12 widgets, 4 maturity phases, 10 state names, resolved decisions)

## 2. Complete the per-state composition design

- [x] 2.1 Produce a composition spec for each of the 10 states — done in `booksy-provider-app/PROVIDER_HOME_SCREEN_DESIGNS.md` (anchors §1–§2 in depth; Growth §3, No-Appts §4, Vacation §5, Closed/Fully-booked §6; coverage map §7) with zone/widget order + priority
- [x] 2.2 Annotate each composition with the resolved `HomeContext` values that produce it (each screen section opens with its resolved context)
- [x] 2.3 Document the visual hierarchy per state (hero / muted / elevated zones called out per screen)

## 3. Finalize the widget catalog & contract

- [ ] 3.1 Write the `HomeWidget` contract reference sheet (the common interface fields from design.md D2) as the authoring guide for future widget implementers
- [ ] 3.2 For each of the 12 catalog widgets (design.md D3), produce a one-page spec sheet covering all contract fields plus its four state treatments (loading / empty / error / offline)
- [ ] 3.3 Define the widget registry model: descriptor shape (id, priority, factory, visibility rule), the named priority constants, and how add/remove/reorder/replace is expressed without touching the Home page
- [ ] 3.4 Specify the banner-rail precedence + stacking rules (design.md D6) as a standalone reference for the BannerRail widget

## 4. Define the state-resolution & maturity models precisely

- [x] 4.1 Document the `resolveHomeContext` algorithm + `HomeInputs`/`HomeContext` value-object shapes — `booksy-provider-app/PROVIDER_HOME_RESOLVER_SPEC.md` §2–§3
- [x] 4.2 Enumerate a `HomeContext` fixture matrix (16 rows: 10 states + orthogonal combos incl. Offline+Pending, Operational+NoAppts+Offline, Instant/Request reordering) — resolver spec §7
- [x] 4.3 Document `classifyMaturity` (config-driven thresholds, decoupled from providerStatus) + the registry visibility/priority expressions (see-saw) — resolver spec §4, §6

## 5. Resolve open questions (product/backend sign-off)

- [x] 5.1 Confirm the booking model — **RESOLVED: hybrid, provider-selectable (Instant / Request & Approval); Home adapts via `bookingMode`**
- [x] 5.2 Confirm whether push infrastructure is in MVP scope — **RESOLVED: polling + pull-to-refresh for MVP, architected push-ready**
- [x] 5.3 Confirm maturity thresholds — **RESOLVED: all thresholds are backend/remote-config values, never hardcoded**
- [x] 5.4 Confirm client-list source, reviews scope, vacation/closed-day model — **RESOLVED: clients derived from completed bookings (repo-abstracted); reviews out of MVP; vacation/closed are backend-managed availability the Home consumes**
- [x] 5.5 Fold the resolved answers back into design.md (Open Questions → Resolved Decisions) and adjust affected widget specs + add the Booking-Mode Adaptation spec requirement

## 6. Design-quality checks

- [ ] 6.1 RTL & Persian pass: verify every composition and widget spec is authored RTL-native (reading order, banner/badge placement, back-gesture direction)
- [ ] 6.2 Accessibility pass: confirm each widget spec declares touch-target sizing, semantic labels, 1.3× font-scale behavior, and reduced-motion treatment for success micro-celebrations
- [ ] 6.3 Coliride token-mapping pass: confirm each widget references semantic token roles (info/neutral/warning/danger/success, spacing, type, card/banner/row components) and copies no Coliride business flow
- [ ] 6.4 Record the infinite-width button footgun as an explicit constraint in the widget action-row specs (test with the real theme, never in a bare `Row`)

## 7. Review & handoff

- [ ] 7.1 Walk the stakeholder through the full design (states → composition → widgets → maturity) and capture approval or change requests
- [ ] 7.2 Incorporate review feedback into the artifacts and re-validate the change (`openspec validate`)
- [ ] 7.3 Scope the follow-on implementation change(s): the `HomeContext` resolver + `HomeWidget` contract + registry + orchestrator, then incremental widgets, then replacing `ProviderDashboardPage` behind the existing `/dashboard` route
- [ ] 7.4 Note the testing obligations for the implementation change (per repo Testing Policy): cubit/resolver unit tests, widget tests per zone incl. all four state treatments, orchestrator ordering snapshot tests, RTL/accessibility widget tests
