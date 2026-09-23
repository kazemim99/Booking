Status: DONE
Verify: FAST

User report (production QA recording 2026-09-23): the customer app's booking CONFIRM step names the person
«ارائه‌دهنده 9123135143», and the salon app's header/profile shows «09123135143» as the owner's name. "The
provider's first and last name must be shown; I said this last time and it isn't fixed; it must be fixed
everywhere; the number must never be written anywhere." The 2026-09-22 fix (qa-walkthrough-2026-09-22 item 3.4,
`PersonName.RealOrNull`) is deployed and did not reach this path.

## Findings
_Root causes, with evidence (file:line at 6912ca13)._

- **Why 3.4 missed it — the fix's null fell through to an empty string and the name was rebuilt from the raw
  parts, twice.** `GetProviderStaffQueryHandler.cs:74-76`: `FullName = RealOrNull(first, last) ??
  StaffProfile.DisplayName ?? ""`. The owner's membership is created by `OrganizationMembership.CreateOwner`
  with no display name, so FullName = "" — while `FirstName`/`LastName` still carry «ارائه‌دهنده» / «9123135143»
  (lines 80-81). Then `ProvidersController.cs:1260-1262` (GET /Providers/{id}?includeStaff=true) rebuilds
  `FullName = $"{FirstName} {LastName}"` whenever FullName is blank — the placeholder, verbatim. The customer
  app's `parseStaff` (`booking_repository_impl.dart:201-211`) would have done the same join client-side.
- **Why the confirm step shows it and not the (fixed) slot name.** A salon with one bookable member skips the
  staff step and auto-selects `provider.activeStaff.first` (`booking_bloc.dart:457-462`); the confirm row reads
  `state.staff?.name ?? slot.staffName` (`booking_flow_page.dart:424`), so the provider-detail staff name wins.
  `AvailabilityService.cs:525-529` (the slot's `availableStaffName`) was fixed in 3.4 and was never on this path.
- **Other routes by which a placeholder/phone reaches a name field (all fixed here):**
  `GetQualifiedStaffQueryHandler.cs:92-97` and `GetOrganizationMembershipsQueryHandler.cs:56-60` join
  first+last raw (no RealOrNull); `ProviderClientsReadService.cs:30` concatenates first+last in SQL (a customer
  signed up by OTP shows as «مشتری 9384444636» in the salon's client book); `ProcessNotificationOutboxJob.cs:
  139-143` stores the raw placeholder as a notification's RecipientName; `AvailabilityService.cs:525` trusts a
  salon-typed display name even when it is a phone.
- **Salon app header shows the phone.** `ProviderUser.displayName` (`provider_session.dart:22`) falls back to
  `phoneNumber` when the name is empty — and a restored session (`auth_repository_impl.dart:199-203`, every cold
  start) never carries a name, so the Home account sheet and the More header print «09123135143». After OTP the
  name is the token's placeholder, printed as is. The team list (`more_sub_pages.dart:857-859`) also falls back
  to the phone, a pending invitation without a name is titled with its phone (`more_models.dart:88-89`), and the
  booking composer's staff picker offers a nameless member by their phone (`home_repository_impl.dart:864-869`).
- Not paths: provider bookings (`GET /Bookings/provider/{id}`) carry no person names at all; review authors are
  «Customer <8 hex>» (no phone — noted, out of scope); booking notifications already go through RealOrNull.

## Acceptance scenarios

- S1 Given a salon whose only bookable member is its owner, whose account still reads «ارائه‌دهنده 9123135143»,
  when a customer opens it (GET /Providers/{id}?includeStaff=true), then the member is named with the salon's
  name and no staff field carries the placeholder word or the digits.
- S2 Same salon: available-slots `availableStaffName` and qualified-staff `name` are the salon's name.
- S3 Same salon, salon side: `/hierarchy/members` gives the owner NO name (the app labels it) and the number
  only as `phoneNumber`; the legacy `/staff` roster's first/last are blank and fullName is the salon's name.
- S4 A customer who signed up by OTP without a name has an empty name in the salon's client book (phone as phone).
- S5 GET /Bookings/my-bookings items and GET /Bookings/{id} carry `staffName`: the assigned member's real name;
  the salon's name when the member has only the placeholder; null when the booking is held by the salon itself.
- S6 Customer app: a placeholder or digits-only person name is never rendered — staff list and confirm step fall
  back to the salon's name; `BookingSummary.staffName` parses (optional) with the same guard.
- S7 Salon app: no header, sheet or team row ever shows a phone as a name; the name is restored from the token
  on a cold start; a placeholder shows a neutral label; the phone may appear only labelled as a phone.
- S8 Salon app: after OTP, an established salon account whose name is a placeholder is asked for its real name
  once (skippable, like the customer app); an account still in onboarding is not (the wizard asks for it).

## Tasks

- [x] T1 RED unit tests: PersonName drops any 7+-digit run, a bare placeholder, digits in the first-name field; ForMember rule
- [x] T2 RED integration tests S1-S4 reproducing the production shape (owner signed up by provider OTP, no name)
- [x] T3 Fix PersonName + provider-detail staff, /staff, qualified-staff, members, availability, client book, outbox
- [x] T4 RED integration tests S5 (staffName on my-bookings and booking details) → implement additively
- [x] T5 Customer app: person-name guard in staff parsing + confirm step; BookingSummary.staffName (tests first)
- [x] T6 Salon app: never a phone as a name (header, account sheet, More, team list, invitations) — tests first
- [x] T7 Salon app: name restored from the token on cold start and after a rename (tests first)
- [x] T8 Salon app: complete-name page after OTP for an established account with a placeholder name (tests first)
- [x] T9 Vue web: check the same displays; fix what reads the raw parts
- [x] T9b Admin: the users list names a person by their real name or «بدون نام», never «مشتری <digits>»
- [x] T10 Verify: build + unit projects + affected integration classes; flutter analyze/test in both apps

## Decisions

- D1 (tier 1) One rule, `PersonName`: a run of 7+ digits (any spacing, Latin or Persian digits) is a phone and is
  never part of a name; a digits-only surname is not a surname; the bare placeholder word is not a name. Shorter
  numbers stay (a display name like «سالن ۲۴ ساعته» is untouched).
- D2 (tier 1) Customer-facing names (salon page staff, qualified staff, slots, booking staffName) fall back to the
  salon's name for them, then the salon's own name — the 3.4 decision, now applied everywhere and never empty.
- D3 (tier 2, flagged) The salon's own roster (`/hierarchy/members`) and the client book return `name: ""` for a
  person with no real name (they returned the placeholder). The apps label it; the number stays in its phone field.
  On the salon's own team list the salon's name would read as if the member were the salon.
- D4 (tier 1) The person's OWN account (auth responses, token claims, GET /Users/{id}) still carries the stored
  placeholder: it is the signal both apps use to ask for a real name. Every place that DISPLAYS it guards it.
- D6 (tier 1) Salon app neutral labels: «نام شما ثبت نشده» for the signed-in person, «بدون نام» for a member or an
  invitation; the phone appears only labelled («موبایل 0912 313 5143»). The composer's staff picker, which fell
  back to the member's phone, shows «بدون نام · موبایل …».
- D7 (tier 1) The salon app asks for the name the way the customer app does: once per OTP sign-in, only on the
  way out of the auth screens (a pure `redirectFor(nameMissing:)` decision, not an imperative push that races the
  router), skippable, never at a cold start; not during onboarding, whose wizard asks for the owner's name.
- D8 (tier 1) The session takes the person's name from the token (the API writes the ClaimTypes URIs): on a cold
  start (it carried none, so the header fell back to the phone) and on every refresh (so a rename shows at once).
- D5 (tier 2, flagged) Additive: `staffName` on GET /Bookings/my-bookings items and GET /Bookings/{id}; left out of
  the JSON (null) when the salon itself holds the booking. The Vue web already renders `booking.staffName` when set.

## Open (not decided here)

- Q1 (tier 3, privacy) GET /providers/{id}/hierarchy/members is `[Authorize]` only: any signed-in user can read any
  salon's member list with phone numbers. Not changed — an authz rule is the business's call.
- Q2 (data) Accounts already stored as «ارائه‌دهنده/مشتری <digits>» are not rewritten; every surface now hides the
  placeholder, and the salon owner is asked for a name at the next OTP sign-in. A one-off backfill would need
  names nobody has.
- Q3 Review authors are «Customer <8 hex>» (GetProviderReviewsQueryHandler) — no phone, but not a name either.

## Log

- 2026-09-23 T10 FAST+: `dotnet build Booksy.sln` 0 errors; 9 unit/architecture projects green; the FULL
  integration suite 817/817 green (Testcontainers). Flutter: analyze clean and all tests green in both apps
  (customer 671, provider 646 + 1 pre-existing skip). Vue web vitest src 156/156; admin vitest 80/80, vue-tsc clean.
- 2026-09-23 T9b Admin (`booksy-admin`): the users list printed `firstName lastName` — «مشتری 9384444636» for a
  phone sign-up. It now shows the real name or a muted «بدون نام» (`user.noName`, fa + en); the phone keeps its
  own column and the edit form still shows what is stored. The spec was written before the util but could not be
  run RED at the time (no node_modules in the worktree yet); vitest 80/80, vue-tsc clean.
- 2026-09-23 T1-T4 RED `PersonNameTests` (16 new cases failing on the 3.4 rule) and 7 integration tests reproducing
  the production shape (owner signed up by provider OTP, no name): GET /providers/{id}?includeStaff returned
  `fullName: "ارائه‌دهنده 9129418152"`, qualified-staff the same, members and the client book the placeholder,
  and bookings no staffName → GREEN. Plus the outbox RecipientName test (RED on the old join, verified by
  reverting that line). Regression: 9 unit/architecture projects green; 19 integration classes, 142/142 green.
