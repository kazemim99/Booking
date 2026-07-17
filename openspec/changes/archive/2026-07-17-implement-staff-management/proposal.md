## Why

The Setup-phase activation checklist's «افزودن اعضای تیم» — the Home's very first ask of a new provider — still shows "coming soon", and More → تیم is read-only. The backend staff CRUD (`POST/PUT/DELETE /Providers/{id}/staff`) has existed since the keystone flow and is exercised by the deploy gate; only the app-side management is missing.

## What Changes

- **Staff management on More → تیم**: an add action opens a staff form sheet (first/last name, phone, role); tapping a member opens the same form pre-filled for editing, with a confirm-guarded remove.
- **Activation checklist wiring**: the Setup checklist's staff item navigates to the staff screen instead of "coming soon".
- `ProviderStaffMember` gains first/last name + phone (needed for edit prefill; the API already returns them).
- `StaffCubit` gains add/update/remove mutations (Home-style: `Failure?` results, reload on success).
- No backend changes.

## Capabilities

### New Capabilities
- `provider-staff-management`: adding, editing, and removing team members from the provider app, and the activation-checklist path into it.

### Modified Capabilities
<!-- None. -->

## Impact

- Flutter: `more_models.dart`, `home_api_service.dart` (+3 methods), `home_repository(_impl).dart`, `more_cubits.dart` (StaffCubit mutations), `more_sub_pages.dart` (StaffView + form sheet), `home_page.dart` (checklist wiring), `AppStrings`, tests.
- Staff photo upload and per-staff working hours remain future changes.
