## 1. Data & Home

- [x] 1.1 `AvailabilityException` entity; api get/add/delete (+ constants); repository CRUD
- [x] 1.2 `_todayAvailability`: holidays ∥ exceptions (closed-today exception → closedToday; failures → open); tests

## 2. UI

- [x] 2.1 `blockTime` on `HomeCubit`/`CalendarCubit` (refresh-on-success) + tests
- [x] 2.2 Shared `BlockTimeSheet` (date, all-day vs modified hours, reason gating) wired to both ⊕ menus (Calendar pre-dated); `AppStrings`
- [x] 2.3 `ExceptionsCubit` + «ساعات استثنائی» section on the Holidays page with confirm-guarded delete

## 3. Verification

- [x] 3.1 Widget tests (sheet gating/payload/all-day vs hours, exceptions list + delete, ⊕ wiring); `flutter analyze` + full suite green; commit + push
