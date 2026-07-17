## 1. Data

- [x] 1.1 `ProviderStaffMember` + `InsightsSummary` entities
- [x] 1.2 `HomeRepository.fetchStaff/fetchServices/fetchInsights` + impl + repository tests

## 2. State & UI

- [x] 2.1 `InsightsCubit`/`ServicesCubit`/`StaffCubit` + DI + cubit tests
- [x] 2.2 `MoreHubPage` (business + account sections, share link, logout) + `NavTab.more`
- [x] 2.3 Sub-pages: Insights (stat tiles), Services (read-only list), Staff (read-only list) with standard treatments
- [x] 2.4 Routes `/more`, `/more/insights`, `/more/services`, `/more/staff`; `AppStrings` section

## 3. Verification

- [x] 3.1 Widget tests: hub rows + logout event + nav active, sub-page render/empty/error, real theme + RTL
- [x] 3.2 `flutter analyze` + full suite green
