# Copilot Instructions for Booksy Frontend

## Project Overview

- This is a Vue 3 + Vite + TypeScript monorepo for a booking platform frontend.
- Major features are organized in `src/modules/` (e.g., `auth`, `provider`, `booking`, `admin`).
- Shared logic and UI are in `src/core/` and `src/shared/`.
- API communication is handled via a custom `HttpClient` in `src/core/api/client/http-client.ts`.
- State management uses Pinia stores (see `src/modules/*/stores/`).

## Developer Workflows

- **Install dependencies:** `npm install`
- **Start dev server:** `npm run dev`
- **Type check:** `npm run type-check` (uses `vue-tsc` for `.vue` files)
- **Build for production:** `npm run build`
- **Unit tests:** `npm run test:unit` (Vitest)
- **E2E tests:** `npm run test:e2e:dev` (Cypress, dev server) or `npm run test:e2e` (Cypress, production build)
- **Lint:** `npm run lint`

## Key Architectural Patterns

- **Modules:** Each feature (e.g., provider, auth) is self-contained with its own API, components, composables, store, and types.
- **API Layer:** All HTTP requests go through `HttpClient`, which manages auth tokens, error handling, and interceptors. Always use this client for API calls.
- **State:** Pinia stores are used for state. Stores expose state, getters, and actions. Example: `useProviderStore` in `src/modules/provider/stores/provider.store.ts`.
- **Error Handling:** API errors are normalized in the service layer and surfaced to stores/components. Validation errors are exposed as objects for form components.
- **Type Safety:** All API responses and store state are strongly typed. Use types from `src/core/types/` and `src/modules/*/types/`.
- **RTL/LTR Styles:** Use mixins from `src/assets/styles/_rtl.scss` for direction-aware styling. Do not use parent selector `&` at top-level in SCSS.

## Project-Specific Conventions

- **Environment Variables:** Access via `import.meta.env` in script, not directly in templates. Expose as computed or data property for template use.
- **Component Structure:** Use `<script setup lang="ts">` for new components. Organize logic in composables (`src/core/composables/`).
- **Testing:** Unit tests in `__tests__` folders next to components. E2E tests in `cypress/e2e/`.
- **API Error Handling:** Always catch and normalize errors in service classes before updating store/component state.

## Integration Points

- **External APIs:** All backend communication is via REST endpoints, configured in `src/core/api/config/api-config.ts`.
- **Authentication:** JWT tokens are stored in `localStorage` and managed by `HttpClient`.
- **Feature Flags:** Controlled via `src/config/feature-flags.ts`.

## Examples

- To fetch providers: Use `providerService.searchProviders()` and update Pinia store.
- To handle validation errors: Pass `validationErrors` from store to form components.
- To add RTL support: Use `@include rtl { ... }` in SCSS blocks inside components.

---

If any conventions or workflows are unclear, please provide feedback so this guide can be improved.
