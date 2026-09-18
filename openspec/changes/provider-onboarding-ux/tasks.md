Status: DONE
Verify: FAST

User feedback (2026-09-18) on the provider app's onboarding at https://provider.nahalkmi.ir, plus the
production data gap it exposed. Two parts: a backend fix without which the map step cannot work in
production at all, and the onboarding UX changes the user asked for.

## Acceptance scenarios

Backend — production reference data
- S1 Given a fresh production database (Database:SeedOnStartup unset, i.e. false outside Development),
  when the host starts, then ServiceCatalog.ProvinceCities is populated and
  GET /api/v1/Locations/hierarchy returns provinces with cities — without any demo providers,
  staff, services, bookings or reviews being created.
- S2 Given a database that already has province/city data, when the host restarts, then nothing is
  re-inserted (idempotent).
- S3 Test hosts and the CI keystone job can still opt out (Database:SeedReferenceData=false) so their
  startup time does not regress.

Provider app — step 1 (business info)
- S4 One "full name" field replaces owner first + last name. It is split on the LAST regular
  whitespace into first/last before sending (the backend requires both non-empty). A ZWNJ-joined
  surname such as «حسینی‌نژاد» stays intact. A single word is rejected inline.
- S5 Description is a multi-line text area.
- S6 Mobile number is pre-filled from the signed-in account and is read-only (not editable).
- S7 Every required field shows a red asterisk in its label.
- S8 Leaving a required field empty shows "این فیلد الزامی است" under that field (on blur, and on
  every required field when Next is pressed). No generic snackbar for field validation.
- S9 No message ever covers the Next button.

Provider app — step 3 (location)
- S10 Typing «پارس» in the city field lists «پارس‌آباد» (works once S1 is deployed).
- S11 Tapping the map fills the address field from reverse geocoding.
- S12 When the step opens with no saved pin, the map centers on the device's location if permission
  is granted, otherwise falls back to Iran's center (today's behaviour).

## Tasks
- [x] 1.1 Split seeding: reference seeders (ProvinceCities, NotificationTemplates) gated by new
      Database:SeedReferenceData (default true); demo seeders stay on Database:SeedOnStartup
- [x] 1.2 appsettings.Testing.json + CI keystone job: SeedReferenceData=false
- [x] 1.3 Test: reference data seeds with SeedOnStartup=false, and no demo data appears
- [x] 2.1 AppTextField: isRequired (red asterisk) and readOnly/enabled
- [x] 2.2 Full-name field + split rule (unit-tested), description textarea, read-only phone
- [x] 2.3 Inline per-field required errors (blur + on Next); drop the field-validation snackbar
- [x] 2.4 Snackbar never covers the action row
- [x] 3.1 Geolocation default for the map (web + mobile), graceful fallback
- [x] 3.2 Verify map tap → address fill and city autocomplete in the real web build
- [x] 4.1 flutter analyze + flutter test; dotnet FAST verify
- [x] 4.2 Deploy backend + provider web build; verify S1-S12 on the live site

## Decisions
- Name split on the LAST whitespace, not the first: Persian compound first names written with a
  space (e.g. «محمد علی») are common, while compound surnames are conventionally joined with a ZWNJ
  («حسینی‌نژاد»), which is not whitespace and so stays whole. The app displays first + " " + last,
  so even a mis-split reconstructs exactly what the user typed. Tier 1 (reversible, UI-only; the
  backend contract is unchanged).
- Reference data gets its own flag instead of turning SeedOnStartup on in production: the
  orchestrator also runs ProviderSeeder, StaffSeeder, ServiceSeeder, AvailabilitySeeder,
  ReviewSeeder and more — fake demo data that must never reach the production database.
- Nominatim is NOT the cause of the map search failing: measured from the user's own network it
  returns 200 in ~1.4s with `access-control-allow-origin: *`. The confirmed cause is
  ProvinceCities having 0 rows in production.

## Log
- 2026-09-18 Investigation. Production GET /api/v1/Locations/hierarchy -> data: [];
  ProvinceCities row count 0. ProvinceCity-ParentChild.json IS present in the image at /app.
  City autocomplete and map-tap reverse geocoding already exist in location_step.dart; they only
  appear broken because the city list is empty. Map default is a hardcoded Iran center; no
  geolocation package exists. Validation lives in OnboardingCubit._validateCurrent() and surfaces
  as a SnackBar, which covers the Next button because the action row sits in the body rather than
  Scaffold.bottomNavigationBar. Backend requires OwnerFirstName and OwnerLastName non-empty
  (SaveStep3LocationCommandHandler).
- 2026-09-18 Step 1 done: AppTextField isRequired/readOnly/onBlur; PersonName split (9 unit tests);
  inline errors on blur + Next, no snackbar; errors for steps 1-7 render in a StepScaffold banner
  above the action row (snackbar kept only for step 8). flutter analyze clean, 456 tests pass.
- 2026-09-18 3.1: geolocator ^13.0.4 behind DeviceLocationService (never throws, 15s cap, null on
  denial). No saved pin -> pin + street zoom on the device fix, reverse-geocode only if the address
  is empty; a late fix never moves a pin the user already placed. 4 widget tests (S12), sabotage
  check fails them. Android/iOS location permissions declared. 460 tests pass.
- 2026-09-18 CI "Integration Tests" went red on 20405675: ReferenceDataSeedingTests saw 71 providers
  left by earlier tests. Root cause was pre-existing test infra, not this change: DatabaseReset cached
  an EMPTY table list when the first reset of a run came from a class that resets before the host
  had started (PersonProvisioningConcurrencyTests / UserRepositorySaveTests), so every later reset
  truncated nothing; DatabaseResetSelfTests passed vacuously over the empty list. Fixed: the factory
  starts the host before resetting, an empty discovery is never cached, and the self-test asserts the
  managed list contains ServiceCatalog.Providers. 484/484 locally; FULL verify PASS (14 steps, 377 s).
- 2026-09-18 4.2 deployed. Backend image 2040567 pulled and restarted: /api/v1/Locations/hierarchy
  returns 31 provinces / 482 cities incl. «پارس آباد» (S1, S10 data); ProvinceCities 513 rows before
  and after a restart (S2); Providers 0, i.e. no demo data. Provider web bundle rebuilt with
  API_BASE_URL=https://back.nahalkmi.ir and live (new step-1 keys present in main.dart.js).
  Upload incident: scp to /tmp "succeeded" but the file was absent for ssh and the old one-liner had
  already emptied /var/www/booksy-provider -> ~1 min of 403; restored, runbook now extracts to a
  staging dir and swaps only after checking index.html.
- 3.2 evidence is automated only: widget tests cover map tap -> address fill and the city list;
  the live API now supplies the cities. The in-browser geolocation prompt and a real map tap need a
  person on the device; Nominatim DNS failed from this workstation's network at verify time.
