Status: STOPPED(needs-user: cancel stuck runs 107/109, merge to master, describe the heart symptom)
Verify: FAST

User request (2026-09-25): «سیستم اضافه کردن به علاقه مندی ها مشتری رو دقیق بررسی و پیاده سازی کن الان فقط یه عکس قلب روی
صفخه جزییات ارائه دهنده اپلیکیشن مشتری هست که کار نمیکنه». Tested on customer.nahalkmi.ir. After the findings below the
user chose: fix the CI deploy.

## Findings (investigated 2026-09-25, before any change)

- F1 Favourites are already built end to end and were not missing: the app-bar heart (`_FavoriteToggle`,
  `provider_detail_page.dart`), `ProviderCustomerCubit` (optimistic toggle, rollback, guest → login), `HomeRepository`
  add/remove/ids, and `GET/POST/DELETE /api/v1/Customers/{id}/favorites` in UserManagement. Done and verified under
  `customer-app-ux-review-fixes` task C.5. The JWT carries `customerId` on both login and refresh.
- F2 The last successful production deploy is run 105 (06d3981, 2026-09-24 21:39), and that commit already has the
  heart. So a stale build does not explain the report; the live symptom is not reproduced here (this sandbox cannot
  reach back.nahalkmi.ir or customer.nahalkmi.ir).
- F3 Nothing has deployed since run 105:
  - run 107 (99321df): deploy job asks for `[self-hosted, asan-rezerve-prod]`, which no runner has. Queued since
    08:08 and holding the `production-deploy` concurrency group (cancel-in-progress: false).
  - run 109 (58c750c): same label, pending behind 107.
  - run 110 (4bec8d0, transitional `booksy-prod` label): `Build AsanRezerve API` failed at the GitHub Actions cache
    export (`error writing layer blob: not_found`) after both image tags were already pushed, so deploy never ran.

## Tasks

- [x] 1 `deploy.yml`: a failed buildx cache export no longer fails an image build (`ignore-error=true` on both
      `cache-to`); the image push is what the deploy needs.
- [x] 2 FAST verify: not runnable in this sandbox (no dotnet SDK; every step "dotnet: command not found"). The
      change is workflow YAML only, no .NET code; `yaml.safe_load` parses it. CI's own build jobs are the real check.
- [x] 3 Commit and push to `claude/bold-euler-0a2lah`.

## Needs the user (not doable from here)

- Cancel runs 107 and 109 in GitHub Actions: they can never run and hold the deploy lock.
- Merge task 1 to master (or re-run run 110) so a deploy on `booksy-prod` actually happens.
- For the heart itself: what exactly happens on tap on customer.nahalkmi.ir (signed in? nothing / login page / red
  message / heart flips back?) and, if possible, the browser devtools Network entry for `/favorites`.
