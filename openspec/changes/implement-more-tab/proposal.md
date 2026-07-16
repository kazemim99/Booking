## Why

More (بیشتر) is the last stubbed bottom-nav destination. The IA defines it as the configuration-and-reflection hub: Business Profile, Services, Staff, Insights, Account, Logout — "configuration lives one level down; analytics is never the front door." Completing it makes the entire IA bottom nav real.

## What Changes

- **More hub** (`/more`): grouped list — کسب‌وکار (services, staff, insights, share booking link, + coming-soon rows for profile/hours/gallery edit flows) and حساب کاربری (name/phone/status, logout).
- **Services** (`/more/services`): read-only list of the provider's services (name, duration, price) from the live-verified catalog endpoint. Editing is a future change (registration flows own creation today).
- **Staff** (`/more/staff`): read-only team list (name, role, active) from the live-verified staff endpoint. CRUD is a future change.
- **Insights** (`/more/insights`): booking statistics — all-time totals (completed/cancelled/no-show, revenue) + trailing-30-days count — from the live-verified statistics endpoint. Deliberately two taps down from a non-default tab, per the IA's "workflows over statistics".
- `ProviderNavBar`'s fourth destination goes live; the "coming soon" fallback disappears from the bar.
- No backend changes — all three sub-screens ride on endpoints the Home/composer already consume.

## Capabilities

### New Capabilities
- `provider-more-hub`: the More tab — hub structure, the three read surfaces (services, staff, insights), account section, and logout.

### Modified Capabilities
<!-- None. -->

## Impact

- Flutter only: `more_page.dart` (hub) + `more_sub_pages.dart` (insights/services/staff), `more_cubits.dart`, entities (`ProviderStaffMember`, `InsightsSummary`), repository additions (`fetchStaff`/`fetchServices`/`fetchInsights`), routes `/more/*`, nav, `AppStrings`, DI, tests.
- Deferred (each needs its own change): business-profile/hours/gallery editing, service/staff CRUD, notification & language settings.
