Status: DONE
Verify: FULL

Found 2026-09-26 by the Playwright keystone suite, running in CI for the first time (PR kazemim99/AsanRezerve#35,
carried on that PR at the user's request). Both provider-registration specs fail: `POST /api/v1/providers/draft` →
400 `Invalid category: Salon`, for every category.

## Findings

- The category step emits the canonical slug (`hair-salon`, `barbershop`; CategorySelectionStep, "Emits the canonical
  category slug"). `ProviderRegistrationService.mapCategoryToProviderType` still maps the old underscore taxonomy
  (`hair_salon` → `Salon`) to the pre-enum ProviderType names and defaults anything else to `'Salon'`. Every slug
  misses the table, so every draft is sent as `Salon`, which `ServiceCategoryResolver` rightly rejects: **web salon
  registration is broken for every category** (createProviderDraft and saveStep3Location).
- The reverse, `mapProviderTypeToCategory`, turns the draft's category (the enum name, e.g. `Barbershop`) into
  `hair_salon` for anything but `Salon`/`Spa`/…, so resuming a barbershop's draft re-selects the women's-salon card.
- Neither mapping is needed: the backend accepts the slug, the enum name, the numeric id and the old ids
  (ServiceCategoryResolver), and the step normalises every one of those (`parseCategory`).

## Tasks

- [x] 1 Unit tests: createProviderDraft and saveStep3Location send the category unchanged; getDraftProvider and
  getRegistrationProgress return it unchanged; a resumed barbershop draft resolves to Barbershop.
- [x] 2 Delete both mappings.
- [x] 3 Frontend type-check, lint, unit tests; FULL verify. (The Playwright keystone run on the PR is the last
  check; it is followed by the PR's scheduled check-in, not by this list.)

## Log

- 2026-09-26 Red first: the 7 new service specs failed for the reason in Findings (sent `Salon`; a barbershop draft
  came back as `hair_salon`). Both mappings deleted; the backend's slug table confirmed to resolve `hair-salon` and
  `barbershop` (ServiceCategoryExtensions.TryParseSlug, covered by ServiceCategoryTests and CategoriesControllerTests).
  Frontend: type-check clean, lint 0 errors (283 warnings, unchanged), unit 215/215. FULL verify PASS (17 steps,
  integration 901/901). Playwright on c741673, before this fix: 5 passed, 2 failed — only the two registration specs,
  both on `POST providers/draft` → 400 `Invalid category: Salon`.
