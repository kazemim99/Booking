# Booksy Provider — Design System Specification (v2, normative)

**Every new Booksy Provider screen MUST follow this spec.** It supersedes the v1
observational audit.

Sources, in order of authority:
1. **ColiRide production Flutter source** (`C:\Repos\Coliride\FrontendClient`) — exact
   component implementations (`core/config/constants/{colors,dimens}.dart`,
   `core/presentation/widgets/*`, `features/personal/*`).
2. **ColiRide Figma**, Profile node `11694-35866` (file `7B8fFnJGVK2Dr65ymZQyVw`) —
   screen anatomy + pixel-sampled colors/geometry from exported PNGs.
3. **Booksy provider app** current theme (`lib/config/theme/app_tokens.dart`,
   `app_theme.dart`) — the implementation target these specs bind to.

ColiRide is a visual-language donor only: reuse tokens/rhythm/components, never its
flows, icons, branding, or content. §9 lists every uncertainty and every place the
Figma and production code disagree, with the ruling for Booksy.

---

## 1. Color Tokens (normative)

### 1.1 Brand & semantic

| Token | Value | Usage (exhaustive — do not improvise) |
|---|---|---|
| `primary` | `#3777BF` | Filled buttons, links, text-button accents, focused interactive tint |
| `appBar` / chrome | `#3777C0` | App bar, blue chrome header, bottom nav pill, action-sheet accent |
| `ink` | `#4D5E80` | ALL headings, values, body text, unselected tab labels, text buttons. Text is never black. |
| `success` | `#0AC075` | Active tab label+indicator, completeness ring & % pill, camera badge, selected checkbox/radio/switch, selected chips, dialog-header accent divider, map pin |
| `danger` | `#FF6171` | Count badges, destructive button fill, logout icon, error toasts |
| `inputError` | `#E74A3B` | Input error border ONLY (production uses this, not `#FF6171`) |
| `warning` | `#FFCB33` | Star ratings, warning accents |

### 1.2 Greys & structure

| Token | Value | Usage |
|---|---|---|
| `border` | `#EBEEF3` | Resting input border, tab divider, card border (Booksy), price container fill |
| `menuBorder` | `#E5E8EB` | Hub menu card border, its internal dividers, sticky-footer top border |
| `dividerSoft` | `#E8EDF4` | Read-only detail-row dividers |
| `divider` | `#E5E9F2` | General dividers, disabled input border |
| `borderFocus` / `outline` | `#C3CAD9` | Focused input border, hint text, unchecked checkbox border, disabled button fill, disabled secondary border/text |
| `icon` | `#D2DBEB` | ALL decorative icons: row leading icons, chevrons, field icons, sheet close disc, inactive page indicator |
| `readLabel` | `#B8C1D1` | Read-only detail-row labels (15 w500) |
| `muted` | `#96A0B3` | Secondary/description text, disabled action-button icon, scrollbar thumb |
| `subtitle` | `#7F8696` | Menu-row subtitles, helper sentences under section titles |
| `surfaceSoft` | `#FAFAFA` | Soft item fill (profile menu item fill in production) |
| `surface` | `#F5F5F5` | M3 surface |
| `avatarBorder` | `#F0F0F0` | Avatar rim on white |
| `backDisc` | `#F1F5F9` | Auth back-button disc fill |

### 1.3 Overlays & feedback surfaces

| Token | Value | Usage |
|---|---|---|
| `loadingOverlay` | `#000000` 13% (`0x22`) | Blocking loading toast background |
| `dialogBarrier` | `#000000` 14% (`0x24`) | Dialog barrier (Booksy token, keep) |
| `sheetBarrier` | `#000000` 28% (`0x47`) | Bottom-sheet barrier (Booksy token, keep) |
| Success toast | fill `success`, white icon+text | EasyLoading-style transient |
| Error toast | fill `danger`, white icon+text | idem |

### 1.4 Choice-chip pastel family (Figma-only; not in production code)

Tile fill pastels with ink text; icon tile in a saturated sibling of the fill:
pink `#FFE0FC`, periwinkle `#D3DCFF`, mint `#CBFAEA`, rose `#FFD3D4`,
butter `#FBECBB`. Selected chip (production): fill `success`, white icon+text.

### 1.5 Rules

- One accent meaning each: **green = positive/active/selected**, **blue =
  brand/interactive**, **coral = destructive/alert**. Never swap roles.
- Icons are decoration → default `icon` grey; meaning lives in text.
- Dark mode: NOT specified (ColiRide's dark palette is mostly a copy of light —
  treat as unimplemented; Booksy is light-only until a real dark spec exists).

## 2. Typography

Font: **Vazir** (Booksy standard, Persian-first). ColiRide uses commercial
"GT Eesti Pro Display" — do NOT license-copy; keep Vazir and reuse the ramp.
Hierarchy is expressed by **weight + color, almost never by size**. Max ~3 sizes
per screen.

| Style | Size | Weight | Color | Used for |
|---|---|---|---|---|
| Page/dialog/sheet title | 18 | bold | white on chrome; `ink` (dialog); `appBar` (sheet title) | Screen titles, dialog & sheet headers, form section titles |
| Card/row title, tab label | 16 | bold / w700 | `ink` (tab active: `success`) | Menu-row titles, tab labels, medium-button caption |
| Button (big) | 17 | bold | white / role color | 46dp primary buttons |
| Dialog button | 15.5 | bold | role color | 40dp dialog buttons |
| Read row label / value | 15 | w500 / w700 | `readLabel` / `ink` | Read-only detail rows |
| Body / subtitle / small btn | 14 | regular (subtitle), bold (small button), bold (field label) | `subtitle` / `ink` | Row subtitles, field labels, helper text, 30dp buttons |
| Badge label | 12.5 | w600 | white | Count badges on icons |
| Status badge | 12 | w600 | white (or role) | Status pills |
| Hint | 14 | light (w300; production uses w100 — see §9) | `borderFocus` | Input placeholder |

## 3. Spacing & Layout

**Base grid 4pt.** Canonical values: 4, 8, 10, 12, 14, 16, 20, 24, 40.

| Rule | Value |
|---|---|
| Screen gutter | **16** (Figma hub; Booksy `AppSpacing.md`). Production menu uses 20 — ruled: Booksy stays 16. |
| Card interior padding | 12 |
| Menu row padding | H12 / V14 (≈52px row pitch incl. hairline) |
| Icon ↔ text gap in rows/buttons | 12 (rows) / 6 (buttons) |
| Title ↔ subtitle gap | 4 |
| Read row rhythm | 15 above/below each divider |
| Section title ↔ content | 18 |
| Section ↔ section | 24–32 |
| Sticky footer padding | 12 all sides + safe-area bottom |
| Paired buttons gap | 10 (mobile), 12 (desktop) |
| Sheet drag-handle margins | 16 top / 8–16 bottom |

**Screen anatomy (chrome + sheet)** — every full screen:
1. Blue chrome (`appBar`) at top. Hub/identity screens: tall chrome (~292/893 frame)
   holding avatar block. Detail/form screens: slim chrome = back (white, auto-RTL) +
   18-bold white title + trailing 44dp action.
2. White content sheet overlapping the chrome with **top radius 16** (measured
   15–16px; token `AppRadius.panel`). All content scrolls inside the sheet; chrome
   stays fixed.
3. Optional sticky footer (§5.6) outside the scroll view.
4. Hub screens: floating bottom nav pill (§5.11) — content gets ≥88px bottom padding.

Alignment: labels/values flow from the leading edge; chevrons, Edit buttons,
toggles align to the trailing edge ("affordance column"). RTL mirrors everything.

## 4. Radius, Borders, Elevation

| Radius token | Value | Applies to |
|---|---|---|
| `statusBadge` | 6 | Status pills |
| `button` | 10 | All buttons (`itemRadius` 10 shares this) |
| `field` | 12 | Inputs, 44dp action buttons, hub menu card, InkWell on rows |
| `snackbar` | 12 | Snackbars |
| `bottomSheet` | 14 | Sheet top corners |
| `card` | 15 | Content cards |
| `panel` | 16 | Content sheet top corners, dialogs |
| circle | 999 | Avatar, discs, badges, drag handle (r8 on 5px height) |

| Border | Width | Color |
|---|---|---|
| Hairline (cards, dividers, footer top) | 1 | `menuBorder` / `dividerSoft` / `divider` |
| Input resting / error / disabled | 1.9 | `border` / `inputError` / `divider` |
| Input focused | 2.1 | `borderFocus` |
| Outlined (secondary) button | 2 | `primary` (disabled: `borderFocus`) |
| Checkbox unchecked | 2 | `borderFocus` |
| Tab indicator (bottom) | 2 | `success` |
| Sheet header accent divider | 2.6 | `border` (grey) or `success` (action sheets) |

**Elevation: 0 everywhere.** Buttons, cards, dialogs, sheets, snackbars — flat.
Depth = color contrast + hairline borders + barrier dimming. Single sanctioned
exception (from production): auth-flow card soft shadow blur 24 / y4 / 8% black —
do not use elsewhere.

## 5. Component Specifications

### 5.1 Buttons (`AppButton`)

Four roles × four sizes. Radius 10, elevation 0, bold label, spinner replaces
label when loading (Cupertino spinner r10–11, label-colored).

| Size | Height | Font | Padding H | Use |
|---|---|---|---|---|
| big (default) | 46 | 17 | 16 | Screen-level CTAs |
| medium | 46 min | 16 | 16 | In-card actions |
| dialog | 40 | 15.5 | 18 | Dialog CTAs |
| small | 30 | 14 | 14 | Section-header Edit, inline actions |

| Role | Fill | Label | Border |
|---|---|---|---|
| Primary | `primary` | white | none |
| Secondary | white | `primary` | 2px `primary` |
| Destructive | `danger` | white | none |
| Text | transparent | `ink` | none |

States: **disabled** primary/destructive → fill `borderFocus` (#C3CAD9), white
label; **disabled** secondary → white fill, 2px `borderFocus` border + label;
**loading** → disabled + spinner; icon (optional) 20px, 6px gap, label side.
Full-width by default (`Size.fromHeight`) — **never place an unconstrained themed
button in a Row** (known crash footgun); wrap in `Expanded`.

### 5.2 Icon action button (`AppIconButton`)

44×44 container, radius **12**, icon 20. Fill transparent (on white) or white disc
(on chrome). Icon color: `borderFocus` on white, white on chrome, `danger` for
logout. Disabled icon `muted`. Hover/press: primary at 4–5% opacity. Badge:
`danger` disc, white 12.5 w600 label, offset ≈ (±3.5, −3.5) — (±4.5, −4.5) with
label; badge count caps at "99+".

### 5.3 Hub menu card (`AppListRow` group)

Container: white, radius 12, 1px `menuBorder` border, zero outer elevation.
Rows: `InkWell` (radius 12) → padding H12/V14 → `[icon 24 (icon grey)] – 12 –
[title 16 bold ink (+4 gap, subtitle 14 subtitle-grey)] – spacer – [chevron 24
icon-grey, auto-mirrors]`. Divider between rows: 1px `menuBorder`, indent 12 both
sides. Entire row is the tap target (≥48dp). One card per group — never stack
one-row cards.

### 5.4 Read-only detail rows + section header

Section header: title 18 bold `ink`, trailing small secondary button "ویرایش"
(Edit, 30dp). Rows: label 15 w500 `readLabel` (leading icon optional, icon grey)
… value 15 w700 `ink` trailing; long values wrap under the label. Row separation:
15px gap – 1px `dividerSoft` – 15px gap. No card border required inside the sheet;
whitespace separates sections (24–32).

### 5.5 Inputs (`AppTextField`)

Outlined, radius 12. Floating label 14 bold `ink` (floats into border gap);
hint 14 light `borderFocus`; field icons `icon` grey.
Borders: resting/enabled 1.9 `border` → focused 2.1 `borderFocus` → error 1.9 and
focused-error 2.1 `inputError` (#E74A3B) → disabled 1.9 `divider`.
Error/helper text: max 2 lines below field, 6px gap. Prefix segments (country
code / units): text `ink`, divider chip inside field, `icon`-grey affordance.
Vertical rhythm between fields: 12–16.

### 5.6 Sticky action footer (`StickyActionBar`)

Container pinned under the scroll view: white, **1px `menuBorder` top border**,
padding 12 + safe-area bottom. Contents — two patterns, both sanctioned:
- **Paired (production default)**: `Row[Expanded(primary "ذخیره"), 10, Expanded(secondary "انصراف")]`.
- **Stacked (Figma variant)**: primary full-width above secondary full-width, 10 gap.
Rule: primary action is always the filled blue button; the safe/back-out action is
always secondary-outlined; destructive confirmation uses destructive fill instead.

### 5.7 Tabs (`AppTextTabs`)

TabBar: label 16 w700; active `success`, inactive `ink`; indicator = 2px bottom
border `success`, `TabBarIndicatorSize.tab` (indicator padding H12 when inset look
is wanted); divider under whole bar 1px `border`. Scrollable when >3 tabs.
Content padding: H14 (mobile).

### 5.8 Bottom sheets

Top radius 14, white, respects keyboard insets, max height 90% screen.
Two headers:
- **Modal task sheet**: 8px top gap → AppBar-style header: title 18 bold
  (`appBar` blue for action sheets, `ink` otherwise) leading-aligned (titleSpacing
  20), close = 21px `icon`-grey disc with white 15px ✕, trailing padding 16;
  optional accent divider 2.6 (12 gap above).
- **Picker sheet**: drag handle 40×5 grey r8 (16 top margin) → centered title 18
  bold → 2.6 divider → content.

### 5.9 Dialogs

Radius 16, white, elevation 0, barrier `dialogBarrier`. Header: 40px stack —
centered title 18 bold `ink`, trailing 24px grey disc close (white 15 ✕); 20px
gap below header (divider optional, 2px). Body: content or radio list. Footer:
dialog-size buttons; single full-width primary for confirmations, muted/outlined
Cancel below or beside per §5.6 rules. Use dialogs for confirmation/decision,
sheets for tasks/pickers, full screens for multi-field editing.

### 5.10 Selection controls

- **Checkbox**: r5, unchecked 2px `borderFocus` border white fill; checked
  `success` fill white check.
- **Radio**: selected dot+ring `success`; unselected ring `borderFocus`.
- **Switch**: track `success` (on) / `border` (off), white thumb, no outline.
- **Choice chips (multi-select)**: 2-col grid; unselected = pastel tile (§1.4),
  ink text, saturated icon tile; selected = `success` fill, white icon+text.
- **Toggle check rows** (checklist style): leading check icon `success` (on) /
  `#C7CFDE` (off), 10 gap, label 14–15 w600 `ink`, 10 vertical rhythm.

### 5.11 Bottom navigation pill

Floating: 16px side gutters, ~20px above bottom edge, 343×68 (fills width minus
gutters), radius ≈16, fill `appBar` blue. Icons white ~24; active item shows dot
or filled state; count badges per §5.2. Booksy tabs: Today / Calendar / Clients /
Profile.

### 5.12 Identity header (`ProfileHeader`)

Hub variant (tall chrome): avatar 124×124 centered, y≈53 from safe top; white
inner avatar disc on `#F0F0F0` rim; **completeness ring**: green `success` arc,
sweep = completion %, ~3px stroke, with green pill "NN%" above the ring when <100.
Camera/edit badge: 25px `success` disc, white glyph, bottom-trailing on avatar.
Below: name line 16–18 bold white (`Owner | BusinessName` with thin divider bar),
email/subtitle 14 white 80%. Logout: white 44dp disc, `danger` icon, chrome
top-trailing. Detail variant (slim chrome): 18-bold white centered title, back
leading, bell+badge trailing, avatar 96–124 overlapping the sheet edge.

### 5.13 Status badge (`AppStatusBadge`)

Radius 6, padding H12/V6, label 12 w600. Role fills: success/`success`,
pending/`warning` (ink text), danger/`danger`, neutral/`border` fill with `ink`
text. Count-badge variant per §5.2.

### 5.14 Feedback (toasts, snackbar, loading)

- Blocking load: dimmed overlay (13% black), white spinner+text.
- Success toast: `success` fill, white icon+text; error: `danger` fill. Transient
  ~2s, top or center overlay.
- Snackbar: floating, radius 12, `ink` fill, white text, single action.
- Empty/error states: existing `AppEmptyState`/`AppErrorState` (hero icon 72
  `icon`-grey, title 16 bold ink, body 14 subtitle, optional medium button).

### 5.15 Scrollbar (web/desktop builds)

Thumb `muted` → `ink` on hover/drag; thickness 8 → 10 (hover); radius 10; track
`#5B606B` 8% ( → `ink` 12% dragged); min thumb 56.

## 6. Interaction Patterns

- **Tap targets ≥44dp** (44 icon buttons, ≥48 rows, 46 buttons). Whole row taps,
  never just the chevron.
- **Press feedback**: InkWell ripple on rows; RawMaterialButton highlight on
  buttons (primary ~5% overlay; secondary: blue-tinted 50/100/200 shades).
- **Loading**: in-button spinner for submits (button stays sized); blocking
  overlay only for full-screen transitions.
- **Destructive/consequential actions** always confirm (dialog §5.9); destructive
  CTA uses `danger` fill; safe option visually quieter, never hidden.
- **Success feedback** after mutations: green toast + navigate/refresh;
  optimistic updates roll back with error toast on failure.
- **Edit-in-context**: read screens expose per-section small Edit buttons →
  tabbed edit screen opens on that section's tab. No global edit mode.
- **Navigation**: back is chrome-leading (auto-mirrors in RTL); sheets/dialogs
  close via disc-✕, barrier tap, or Cancel; system back pops sheets first.
- **Motion** (Booksy tokens, not from ColiRide — see §9): fast 180ms / medium
  250ms, easeOutCubic in / easeInCubic out; sheets slide up; respect
  reduced-motion (`disableAnimations`).
- **Accessibility**: semantics labels on every actionable; contrast — never
  `readLabel`/`icon` grey for essential text on white below 14; survives 1.3×
  font scale and small widths without overflow; RTL-first, LTR must not break.

## 7. Screen Composition Checklist (gate for every new screen)

1. Chrome + sheet anatomy (§3), radius-16 sheet top.
2. 16px gutters; spacing only from §3 scale.
3. Text styles only from §2 ramp; text never black; no new font sizes.
4. Colors only from §1 tokens (no inline hex in feature code — extend
   `app_tokens.dart` first if a token is missing).
5. Elevation 0; borders/dim for depth.
6. Components from `core/widgets` (§5) — extend the shared widget, don't fork.
7. Buttons: role/size from §5.1; paired buttons wrapped in `Expanded`.
8. All states designed: loading / content / empty / error (+ offline where
   relevant) via `StateSwitcher` pattern.
9. RTL + 1.3× font scale + small width verified.
10. Widget tests for any new/changed shared component (repo testing policy).

## 8. Implementation Deltas — IMPLEMENTED 2026-07-19

All deltas below are in the codebase, guarded by
`test/config/theme/app_theme_test.dart` and
`test/core/widgets/design_system_components_test.dart`:

1. ✅ Tokens added to `app_tokens.dart`: `menuBorder`, `dividerSoft`,
   `readLabel`, `subtitle`, `inputError`, `surface`, `avatarBorder`,
   `checkOff`, pastel chip family, `loadingOverlay`.
2. ✅ Input error border → `inputError #E74A3B`.
3. ✅ `AppButton` size ramp (`AppButtonSize`: small/dialog/medium/big) and
   roles (`.secondary`, `.destructive` added).
4. ✅ Secondary button: 2px `primary` outline + `primary` text (disabled grey).
5. ✅ New components in `core/widgets/`: `profile_header.dart`,
   `app_text_tabs.dart`, `app_choice_chip.dart`, `app_bottom_bar.dart`,
   `sticky_action_bar.dart`, `app_sheet.dart` (§5.8), `app_dialog_header.dart`
   (§5.9).
6. ✅ `tabBarTheme` added to `AppTheme`.

7. ✅ `ProviderNavBar` migrated onto the `AppBottomBar` pill (icons-only with
   semantic labels). The old center-docked ⊕ FAB became a white mid-pill
   create disc: pages pass `onCreate`/`createKey` (Home hides it on system
   error, exactly as the FAB did). `AppBottomBar` gained a `center` slot that
   splits destinations around it.
8. ✅ The **More hub** (`more_page.dart`) was reskinned into the Profile-hub
   pattern (§5.12): `ProfileHeader` chrome carrying the identity, a white
   sheet with a rounded top over the blue, and rows grouped into cards with
   internal hairline dividers plus chevron discs on the trailing edge.

8. ✅ **Every screen now wears the blue chrome** (2026-07-20). The theme
   always specified it; 15 pages overrode it to white. Extracted
   `core/widgets/app_page_scaffold.dart` — `AppPageScaffold` (blue chrome +
   white sheet with a 16px rounded top edge, `chromeFooter` slot for headers
   that stay on the blue) — and moved Home, Calendar, Clients, Gallery,
   Composer and all six More sub-pages onto it. Add affordances switched to
   the green accent (brand blue is invisible on blue); `AppEmptyState.add()`
   renders the ColiRide green circle-plus + green label. Calendar's week strip
   moved into the chrome and inverted (selected day = white pill).
   Verified against Figma node `13461-48334` by pixel-sampling: chrome
   `#3777C0`, active tab/underline `#0AC075`, ink `#4D5E80`, badge `#FF6171`,
   sheet radius ~16 — all matching the tokens.

**Functional gaps** (design elements with no working backing — notably the
notification bell and its unread count) are tracked in
[FUNCTIONAL_GAPS.md](FUNCTIONAL_GAPS.md). Style them only once they do
something real; never ship a placeholder that mimics live state.

**Domain adaptations** (deliberate divergences from the Figma, per the brief's
"adapt, don't clone" rule):

- **Logout is a labelled row, not a chrome disc.** The Figma hub has no
  account section so it hides logout behind a bare icon; ours has one, and a
  labelled row is more discoverable and accessible in an RTL/Persian UI.
  Duplicating it in both places would be worse than either alone.
- **The sheet needs its own blue backing.** The scaffold stays white so the
  gutter around the floating nav pill reads as "floating"; a `ColoredBox`
  behind the sheet supplies the blue that makes its rounded top legible.
- **Sheet content needs bottom padding.** The nav pill overlays the body
  rather than sitting below it, so the last row must clear it.

## 9. Uncertainties & Source Divergences

**Figma MCP limits hit during audit** (Starter-plan call cap, then bridge
offline). Could not fetch: Figma variable definitions, per-node design context
(exact text styles/auto-layout), other sections of the file (Home, Search,
Rides, desktop). Mitigation: production source code supplied exact values for
everything it implements; PNG pixel-sampling covered the rest. Confidence is
HIGH on colors/dimensions listed above; remaining unknowns:

1. **Exact Figma type sizes for chrome identity text** (name/email on hub) —
   estimated 16–18/14 from render; production hub screen was a placeholder
   (`Colors.pinkAccent` stub), so no code confirmation. Verify when MCP quota
   resets.
2. **Motion**: ColiRide defines no motion tokens; Figma prototype transitions
   unreadable via MCP. Booksy's `AppMotion` (180/250 easeOutCubic) is our own
   convention — normative for Booksy, unverified against ColiRide.
3. **Bottom nav pill**: geometry measured from PNG (343×68, r≈16, gutters 16);
   active-state treatment (dot vs fill) not fully legible at export resolution.
4. **Completeness ring**: stroke ≈3px and pill placement estimated from render;
   sweep-vs-% mapping assumed linear.
5. **Pastel chip family** exists only in Figma (biography chips); production uses
   solid-green selected chips. Both are sanctioned (§5.10); selected-state pastel
   deepening is our extrapolation.
6. **Dark mode**: ColiRide dark palette is largely a copy of light (unfinished) —
   no authoritative dark spec exists.
7. **Divergences ruled for Booksy**:
   - Save/Cancel: Figma stacks; production pairs in a Row → both allowed (§5.6).
   - Screen gutter: Figma 16 vs production menu 20 → Booksy uses 16.
   - Hint weight: production `w100` (thin) — Vazir lacks w100; use w300.
   - Font: GT Eesti (commercial) → Vazir stays.
   - Card radius: hub menu card 12 (production) vs content card 15 (Dimens) —
     both kept, per-component (§4).
