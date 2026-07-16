## 1. Backend

- [x] 1.1 `GetProviderClientsQuery` (+Result) and handler in ServiceCatalog Application
- [x] 1.2 `IProviderClientsReadService` abstraction + Infrastructure SQL implementation (cross-schema join, owner excluded, recency-ordered); DI registration
- [x] 1.3 `GET /Providers/{id}/clients` on ProvidersController with `CanManageProvider`
- [x] 1.4 Rebuild + live-verify: aggregation counts, name/phone resolution, owner exclusion, 403 for non-owner

## 2. Flutter

- [x] 2.1 `ApiConstants.providerClients` + `HomeApiService.getProviderClients` + `ProviderClient` entity + `HomeRepository.fetchClients`
- [x] 2.2 `ClientsCubit`: load, Persian-normalized local search, refresh; unit tests
- [x] 2.3 `ClientsPage`: search field, client rows, empty/error states, client action sheet (call-copy, book-again); nav bar + `/clients` route
- [x] 2.4 Composer prefill via `?client=&phone=` (page seeds the walk-in fields)
- [x] 2.5 Widget tests: list/search/sheet/prefill/nav, real theme + RTL

## 3. Verification

- [x] 3.1 `flutter analyze` + full Flutter suite green; backend unit suite + keystone gate green
