Status: DONE
Verify: FAST

Onboarding step 4 makes every provider type each service by hand, one dialog at a time: name,
duration, price. A barbershop has ~30 usual services and a women's salon ~60, so the step is the
longest stretch of typing in the whole wizard, and most of it is the same list every time.

The user supplied that list for both categories with market prices (2026-09), and asked for it to be
selectable in the UI, editable per row.

## UX decision (asked for as a recommendation)

Checkbox list, as the user proposed, with the price edited inline **only for what is selected**:

- The catalogue is grouped («اصلاح و مو», «رنگ و خدمات تخصصی مو», «پوست و مراقبت», …), each group a
  collapsible section, so ~60 services stay scannable on a phone.
- Each row: checkbox + service name + its suggested price in muted text. Tapping anywhere toggles it.
- Selecting expands that row's price and duration fields inline, pre-filled with the suggestion, so
  adjusting a price never leaves the list and unselected rows stay quiet. (An always-editable field
  on every row would put ~60 text fields on one screen: slow, and it invites typos in rows the
  provider does not even offer.)
- A search box filters by name across groups (Persian-aware, so «کراتینه» finds it whatever the
  keyboard's kaf/ye).
- Prices are labelled as suggestions; they are a starting point, not a claim about this salon.
- "خدمت دلخواه" stays for anything not in the list, listed alongside the selected ones.
- The catalogue follows the category picked in step 2: men's services for a barbershop, women's for a
  hair salon.

## Acceptance scenarios
- C1 A barbershop sees the men's catalogue; a women's salon sees the women's one.
- C2 Ticking a service adds it with its suggested price and duration; unticking removes it.
- C3 Editing the price or duration of a ticked row updates that service, and nothing else.
- C4 Searching «کراتینه» shows the matching rows from any group, ignoring kaf/ye spelling variants.
- C5 A custom service can still be added, and survives alongside catalogue picks.
- C6 Prices are typed and displayed with thousands separators (۲۵۰,۰۰۰), and are sent as a plain
  number.
- C7 Re-entering the step (draft restore) shows the previously chosen services still ticked.

Required fields — a rule for EVERY form in the app (user, 2026-09-19)
- R1 A required field carries a red asterisk in its label.
- R2 An empty required field shows «این فیلد الزامی است» UNDER that field — on blur, and on every
  empty required field when the form is submitted.
- R3 No form answers with a generic «لطفاً تمام فیلدها را درست وارد کنید»: the user must be able to
  see WHICH field is missing without hunting.
- R4 The dialog/sheet action rows read as one row: outlined «انصراف» beside filled «ذخیره», equal
  widths, matching the wizard's footer (the cancel link floating above a full-width save button is
  the app's infinite-width button theme leaking through).

## Tasks
- [x] 1.1 Thousands-separator price input (typing and display), with tests
- [x] 1.2 Service catalogue data for both categories + a test that the data is well formed
- [x] 1.3 Services step: grouped, searchable checkbox catalogue with inline price/duration for the
      selected rows; custom service preserved (C1-C5, C7)
- [x] 1.4 Required-field rule in the onboarding forms still missing it (services dialog, location)
      and the dialog action rows (R1-R4)
- [x] 1.5 Required-field rule in the remaining forms: login, booking composer, clients, block time,
      business profile and the other More forms, invitation register-and-accept (R1-R3)
- [x] 1.6 A test that fails if a generic "fill in all fields" message comes back anywhere (R3)
- [x] 1.7 Verify FAST + flutter, deploy, confirm on the live site

## Decisions
- Prices are **suggestions**, stored per preset, and the provider edits them. Not fetched from the
  backend: this is seed content for a form, and a network dependency would make the step fail exactly
  where it is meant to save time.
- Toman, matching what the step already displays.

## Log
- 2026-09-19 Map: ctrl+wheel did nothing on the web build because a browser reports it as a SCALE
  (pinch) event, which flutter_map ignores; the step now handles that event itself (plain wheel
  scrolling stays flutter_map's). Zoom levels moved one step in (country 5->6, city 12->13,
  street 16->17) as asked. MapZoom.afterScale is unit-tested (doubling the scale = one level,
  clamped to the map's range) and the opening-zoom test fails if the level drifts back.
- 2026-09-19 Working hours: a day could close before it opened and only the Next button complained,
  by which point the offending day had scrolled away (user, with a screenshot). The API already
  refuses such a schedule — now pinned by three tests (reversed day, reversed break, break outside
  opening hours all answer 400). The step now flags the day under its own row, the moment it is
  wrong. Sabotage-checked: removing the banner fails two tests.
- 2026-09-19 Required-field rule applied to: the custom-service dialog, login (phone), block time
  (reason), invitation register-and-accept (first/last name — was a generic snackbar), and the More
  forms (service name/duration/price, holiday reason, business profile name, staff phone). The
  booking composer's client fields are NOT required (it gates on service+staff+slot), so they carry
  no asterisk. Step banners now name the missing fields instead of "fill in all the fields", and a
  source-scanning test fails if that phrasing returns.
- 2026-09-19 Prices group in threes while typing, everywhere a price is entered; parsing goes
  through PriceText, so the separators never reach the API.
- 2026-09-19 User supplied the catalogue (men + women, market prices شهریور ۱۴۰۵) and asked for
  checkbox selection with inline editing, plus thousands separators in the price field.
- 2026-09-19 Closed. verify FULL PASS (14 steps, 303 s) on da08d30a; both production deploys green
  (4b824d42 catalogue + required-field rule, da08d30a map zoom). Live checks: the published bundle
  carries the catalogue (search box and preset rows present), provider.nahalkmi.ir and the API health
  endpoint both 200, and the bundle changed again with the map deploy. The ctrl+wheel gesture itself
  needs a human at a browser — the handler is unit-tested and the wiring is pinned by a widget test,
  but nothing here drives a real browser.
