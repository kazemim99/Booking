## 1. Data layer

- [x] 1.1 `HomeRepository.fetchBookings({from,to})` + impl (reuse enrichment); `fetchSnapshot` delegates to it
- [x] 1.2 Repository tests for the range fetch

## 2. Calendar state

- [x] 2.1 `CalendarCubit`: week window fetch, day selection, week paging with stale-result guard, refresh, booking quick actions (offline-refused), `bookingsByDay`
- [x] 2.2 Cubit unit tests: load, day switch (no refetch), week paging race, action-success-refresh, failure mapping

## 3. UI & navigation

- [x] 3.1 `ProviderNavBar` extracted from `HomeView` (keys preserved); Home uses it
- [x] 3.2 `CalendarPage`: app-bar period + امروز jump, RTL week strip with count badges, day timeline with time gutter, empty/error/stale treatments, ⊕ + empty-day CTA → composer with selected date
- [x] 3.3 Booking action sheet (details + status-appropriate actions + call)
- [x] 3.4 Router `/calendar`; composer `?date=` initial date (cubit + page + route)
- [x] 3.5 `AppStrings` calendar section (Persian)

## 4. Verification

- [x] 4.1 Widget tests: strip/timeline render, day switch, action sheet actions, empty day, nav active states, real theme + RTL
- [x] 4.2 `flutter analyze` + full suite green (Home tests unaffected by nav extraction)
