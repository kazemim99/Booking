## 1. Backend

- [x] 1.1 `CanManageProvider` guard on all 11 `ProviderSettingsController` writes (fallback pattern, no user-id short-circuit); build green
- [x] 1.2 Live E2E: owner GET/PUT round-trip (Tuesday closed, Monday 10:00 + preserved break) incl. first-session fallback; stranger 403 on hours and business-info

## 2. Flutter

- [x] 2.1 api `getBusinessHours` (parse "HH:mm" → `DayHours`) + `updateBusinessHours` (step-6 wire shape); repository methods + round-trip tests
- [x] 2.2 `BusinessHoursCubit` (load-prefill, day/time edits, save, failure-preserves-edits) + tests
- [x] 2.3 `/more/hours` page: Saturday-first day rows, open switch, time pickers, read-only break chips; hub row activation; `AppStrings`
- [x] 2.4 Widget tests: prefill, toggle-day payload, break chip render + preservation, failure keeps edits

## 3. Verification

- [x] 3.1 `flutter analyze` + full suite green; commit + push
