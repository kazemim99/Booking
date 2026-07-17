## Why

More → خدمات is read-only: providers cannot rename a service, change its price or duration, or retire it — prices are the most frequently edited business data. The backend service CRUD (`POST/PUT/DELETE /providers/{id}/services[...]` on `ProviderSettingsController`) has shipped since the Vue era and was ownership-guarded by the hours-change sweep.

## What Changes

- **Services list becomes managed**: an app-bar add action and per-row edit (tap, pre-filled form) and delete (confirm-guarded). The form: name (required), duration in minutes, price, optional description — currency defaults server-side (IRR); category/mobile flags stay at their defaults for MVP.
- `ComposerService` gains a `description` field so updates round-trip it instead of erasing it (the PUT takes the full field set).

## Capabilities

### New Capabilities
- `provider-service-crud`: adding, editing, and deleting services from the app.

### Modified Capabilities
<!-- None. -->

## Impact

- Flutter only (backend shipped + guarded): api `addService/updateService/deleteService`, repository methods, `ServicesCubit` mutations, `_ServiceFormSheet` + list actions on the Services page, `AppStrings`, tests. Queued: category picker, mobile-service flag, currency selection.
