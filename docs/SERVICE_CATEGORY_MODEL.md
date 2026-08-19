# Service Category Model

How Booksy categorises providers and services, end to end: the enum, the database columns, the
API contract, and the frontend mapping.

Supersedes the older `ProviderType` enum and the `ServiceCategory` value object, both removed by
the `refactor-provider-category-model` change.

## The model in one paragraph

Every provider has **exactly one** `PrimaryCategory`, drawn from the `ServiceCategory` enum. A
service also carries a `ServiceCategory`. Category answers *what kind of business is this*;
`ProviderHierarchyType` (Organization vs Individual) separately answers *what shape is this
business*. The two were previously conflated in a single `ProviderType` enum, which is why a solo
barber had no clear representation.

## The enum

`Booksy.ServiceCatalog.Domain.Enums.ServiceCategory`

| Id | Member | Persian | Slug |
|----|--------|---------|------|
| 1 | `HairSalon` | آرایشگاه زنانه | `hair-salon` |
| 2 | `Barbershop` | آرایشگاه مردانه | `barbershop` |
| 3 | `BeautySalon` | سالن زیبایی | `beauty-salon` |
| 4 | `NailSalon` | آرایش ناخن | `nail-salon` |
| 5 | `Spa` | اسپا | `spa` |
| 6 | `Massage` | ماساژ | `massage` |
| 7 | `Gym` | باشگاه ورزشی | `gym` |
| 8 | `Yoga` | یوگا و مدیتیشن | `yoga` |
| 9 | `MedicalClinic` | کلینیک پزشکی | `medical-clinic` |
| 10 | `Dental` | دندانپزشکی | `dental` |
| 11 | `Physiotherapy` | فیزیوتراپی | `physiotherapy` |
| 12 | `Tutoring` | آموزش خصوصی | `tutoring` |
| 13 | `Automotive` | تعمیرات خودرو | `automotive` |
| 14 | `HomeServices` | خدمات منزل | `home-services` |
| 15 | `PetCare` | مراقبت حیوانات | `pet-care` |

**The integer ids are a persisted contract.** They are the values in
`ServiceCatalog.Providers.PrimaryCategory` and `ServiceCatalog.Services.Category`, and they are
mirrored by the frontend `ProviderCategory` enum. Renumbering a member silently re-labels every
existing row. Add new categories at the end; never reuse or reorder an id.

**`0` is deliberately not a member.** That is what makes an unset category detectable — see
[Migration history](#migration-history).

### Display metadata

Metadata lives in code, not the database — `ServiceCategoryExtensions`:

| Method | Example (`HairSalon`) |
|--------|------------------------|
| `ToPersianName()` | `آرایشگاه زنانه` |
| `ToEnglishName()` | `Women's Hair Salon` |
| `ToIcon()` | 💇‍♀️ |
| `ToColorHex()` | `#8B5CF6` |
| `ToGradient()` | `linear-gradient(135deg, #8B5CF6 0%, #A78BFA 100%)` |
| `ToSlug()` | `hair-salon` |
| `ToDescription()` | Persian one-liner |

All of them **throw `ArgumentOutOfRangeException` on an undefined value**. That is intentional: a
category with no metadata is a data defect, and failing loudly beats rendering a blank badge.
Use `IsDefinedCategory()` to test a value before trusting it.

The frontend keeps a parallel table in
[`booksy-frontend/src/core/constants/provider-categories.ts`](../booksy-frontend/src/core/constants/provider-categories.ts).
**The two must be kept in sync by hand.** Both sides have tests that pin the ids and the
spot-checked names.

## Database

| Table | Column | Type | Notes |
|-------|--------|------|-------|
| `ServiceCatalog.Providers` | `PrimaryCategory` | `integer NOT NULL` | Indexed by `IX_Providers_PrimaryCategory`; `CK_Providers_PrimaryCategory_Assigned` enforces `>= 1` |
| `ServiceCatalog.Services` | `Category` | `integer NOT NULL` | `CK_Services_Category_Assigned` enforces `>= 1` |

Mapped with `HasConversion<int>()` in `ProviderConfiguration` / `ServiceConfiguration`.

The CHECK constraints are intentionally open-ended (`>= 1`, no upper bound) so adding a category
does not require a migration. Exact enum membership is enforced one layer up, in the aggregates.

> **Never filter on `Category.ToString()` in a LINQ query.** The column is an int, so EF has no
> translation for it — the query either throws or silently evaluates client-side after loading the
> table. Compare the enum directly: `.Where(s => s.Category == category)`.

## Validation layers

Three layers, each catching what the one above cannot:

1. **API / application** — `ServiceCategoryResolver.TryResolve` parses whatever a client sent and
   rejects anything that is not a declared category.
2. **Domain** — `Provider.EnsureCategoryIsValid` and `Service.EnsureCategoryIsValid` throw
   `InvalidProviderException` / `InvalidServiceException` on an undefined value. Every write path
   goes through a factory or a mutator, so nothing reaches the database unchecked.
3. **Database** — the CHECK constraints backstop raw SQL and future migrations.

Category is captured at registration and is effectively immutable afterwards: `UpdateDraftInfo`
only works while the provider is still `Drafted`. Changing an established provider's category is
an admin-approval concern and is not yet implemented.

## Parsing a category from a client

`ServiceCategoryResolver` (Application layer) is the **only** place that turns a category string
into a `ServiceCategory`. It accepts, in order:

1. numeric id — `"1"`
2. enum member name, any casing — `"HairSalon"`, `"hairSalon"`
3. category slug — `"hair-salon"`, `"hair_salon"`, `"barber"`
4. legacy wizard taxonomy ids — `"brows_lashes"`, `"health_fitness"`, …

and rejects everything else, including out-of-range numbers.

> Do not reintroduce a local `switch` on category strings in a handler. Three of those existed and
> they disagreed: none listed the wizard's `barber` id, so every men's barbershop registered as
> `BeautySalon`.

## API contract

### Serialisation — this is the sharp edge

The host registers `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`, so **categories go over
the wire as camelCase strings, not integers**:

```jsonc
{ "primaryCategory": "hairSalon" }   // NOT 1
```

The draft/registration-progress endpoints are the exception: they return
`PrimaryCategory.ToString()` explicitly, giving PascalCase `"HairSalon"`.

Requests may send either form — `JsonStringEnumConverter` accepts integers on read, and string
payloads go through `ServiceCategoryResolver`.

Frontend code must therefore normalise before use:

```ts
import { parseCategory } from '@/core/constants/provider-categories'

// Accepts 1, "1", "hairSalon", "HairSalon", "hair-salon", "barber" → ProviderCategory
const category = parseCategory(response.primaryCategory) ?? ProviderCategory.HairSalon
```

Reading `response.primaryCategory` as a number is the defect this function exists to prevent.

### Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /api/v1/categories` | Whole taxonomy with provider counts. Empty categories are **included** and flagged `isComingSoon`. |
| `GET /api/v1/categories/popular?limit=8` | Only categories that have providers, ranked by count. |
| `GET /api/v1/categories/{idOrSlug}/providers` | Providers in one category, paginated. Accepts `1` or `hair-salon`. 404s on an unknown category. |
| `GET /api/v1/providers/search?category=…` | Full search with the category filter among the others. |

Each category row carries `id`, `key`, `name` (Persian), `englishName`, `slug`, `description`,
`icon`, `color`, `gradient`, `displayOrder`, `providerCount`, `isComingSoon`.

Counts are aggregated with a `GROUP BY` in the database
(`IProviderReadRepository.CountByCategoryAsync`) — do not reintroduce a version that materialises
every active provider to count them in memory.

## Migration history

| Migration | What it did |
|-----------|-------------|
| `20251223143438_RemoveStaff` | Added `Providers.PrimaryCategory` and `Services.Category` as `INT NOT NULL DEFAULT 0`; dropped `Providers.ProviderType`, `Services.CategoryName/CategoryDescription/CategoryIconUrl`, and the `Staff` table. |
| `20260815222542_BackfillProviderPrimaryCategory` | Remediated the rows that migration stranded, then added the CHECK constraints and dropped the `DEFAULT 0`. |

### Why a second migration was needed

`RemoveStaff` added the category columns with `DEFAULT 0` **and** dropped the legacy
`ProviderType` / `CategoryName` columns in the same step. Zero is not a `ServiceCategory`, so every
row that predated it was left holding a category that no metadata lookup can render, and the
source data needed to infer a better value was already gone.

The remediation does what is still possible:

1. Infer each stranded provider's category from the services it offers (most frequent valid
   category; ties broken on the lowest id so the result is deterministic).
2. Fall back to `HairSalon` for providers with no services to learn from.
3. Give stranded services their provider's category.
4. Add the CHECK constraints — which also *verifies* steps 1–3, since Postgres validates every
   existing row and the migration fails if any are left.

Mapping the old `ProviderType` values across, as the original plan called for, is **not possible**:
`RemoveStaff` always runs first and the column no longer exists.

`Down()` drops the constraints and restores the defaults. It does **not** un-backfill the
categories — the values it replaced were the unrenderable `0`.

### Categories assigned by the backfill are best-effort

Providers that fell through to the `HairSalon` default are guesses. There is no admin UI for
correcting a provider's category yet; until there is, corrections are a manual database task.

## Adding a new category

1. Append the member to `ServiceCategory` with the **next unused** integer.
2. Add its arm to every `switch` in `ServiceCategoryExtensions` — they are exhaustive and throw on
   an unhandled value, so the domain unit tests fail until all seven are filled in.
3. Mirror it in the frontend `ProviderCategory` enum and `CATEGORY_METADATA`.
4. Add it to `ENABLED_CATEGORIES` in `CategorySelectionStep.vue` if it should be offered at
   registration (the wizard deliberately exposes a subset).
5. No migration is needed — the CHECK constraint has no upper bound.

## Searching by category

Two parameters reach the same filter, and both are honoured:

| Parameter | Type | Accepts |
|---|---|---|
| `SearchProvidersQuery.Category` | `ServiceCategory?` | the enum, used by `/categories/{id}/providers` |
| `SearchProvidersRequest.ServiceCategory` | `string?` | enum member name **or** slug, used by the search UI |

`SearchProvidersSpecification` **fails closed**: an unrecognised category matches nothing rather
than falling through to "no filter". Returning the whole catalogue reads as a successful search
while hiding that the filter never applied.

There is no `Type` parameter on provider search — the old `ProviderType` filter is gone.

## Known gaps

- **Service/provider category alignment is not enforced.** A spa can list a haircut. Deferred
  deliberately (design.md, Decision 7); a compatibility matrix would need defining first.
- **No admin UI** for reviewing category distribution or correcting a provider's category. This
  matters most for the providers the backfill defaulted to `HairSalon`.
- **Registration exposes only 2 of the 15 categories** (`HairSalon`, `Barbershop`) — a product
  decision, controlled by `ENABLED_CATEGORIES` in `CategorySelectionStep.vue`.
