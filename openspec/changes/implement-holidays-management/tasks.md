## 1. Data & Home integration

- [x] 1.1 `ProviderHoliday` entity; api `getHolidays`/`addHoliday`/`deleteHoliday`; repository CRUD
- [x] 1.2 `fetchSnapshot`: best-effort holidays → `availability = closedToday` on match (exact / recurring month-day); failure → open; tests incl. the Home closed state firing

## 2. UI

- [x] 2.1 `HolidaysCubit` (list + add/remove mutations) + tests
- [x] 2.2 `/more/holidays` page: soonest-first list, recurring badge, add sheet (date picker, reason gating, recurring), confirm-guarded remove; hub row; `AppStrings`
- [x] 2.3 Real banner copy for closed-today and vacation kinds

## 3. Verification

- [x] 3.1 Widget tests (list/add/remove/gating) + banner copy test; `flutter analyze` + full suite green; commit + push
