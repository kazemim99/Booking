## Why

More → «مشخصات کسب‌وکار» is the most prominent remaining coming-soon row: providers cannot correct their business name or description after onboarding. The backend `PUT /Providers/business` exists — but it resolves the provider **only from the `providerId` JWT claim** (`GetCurrentUserProviderId().Value`), the exact defect class fixed twice already: for first-session-after-onboarding tokens the claim is absent and the endpoint 500s.

## What Changes

- **Backend fix**: `UpdateBusinessInfo` falls back to the owner-ID query when the claim is missing (mirrors `UpdateProfile`'s existing working pattern in the same controller).
- **Flutter — business profile editing** (`/more/business`): a form pre-filled from `GET /Providers/{id}` (business name required, description multiline) saving via `PUT /Providers/business`; the More hub row activates.
- **Deferred, queued next**: working-hours editing (the domain supports `SetBusinessHoursWithBreaks` but only registration commands call it — needs a new Application command + endpoint); logo upload (needs the image-upload flow).

## Capabilities

### New Capabilities
- `provider-business-profile-editing`: editing the business's public name and description from the app, and the claim-fallback fix that makes the endpoint reachable in the first session.

### Modified Capabilities
<!-- None. -->

## Impact

- Backend: `ProvidersController.UpdateBusinessInfo` (fallback only, no shape change).
- Flutter: entity + api (`getProviderDetails`, `updateBusinessInfo`) + repository + `BusinessProfileCubit` + page + route + hub row + `AppStrings` + tests.
