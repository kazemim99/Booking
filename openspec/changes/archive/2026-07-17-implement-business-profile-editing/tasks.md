## 1. Backend

- [x] 1.1 `UpdateBusinessInfo`: owner-query fallback when the providerId claim is absent; build + unit suites green

## 2. Flutter

- [x] 2.1 `BusinessProfile` entity; api `getProviderDetails` + `updateBusinessInfo`; repository fetch/update + tests
- [x] 2.2 `BusinessProfileCubit` (load-prefill, gated save, failure-preserves-input) + tests
- [x] 2.3 `/more/business` page (name required, multiline description), hub row activation, `AppStrings`
- [x] 2.4 Widget tests: prefill, gating, save success/failure

## 3. Verification

- [x] 3.1 `flutter analyze` + full suite green; commit + push
