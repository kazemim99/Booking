Status: ACTIVE
Verify: FAST

User request (2026-09-25): «الان من چرا نمیتونم بعنوان مشتری کامنت بذارم؟ ضمنا میخوام برای سالن نهال یکسری کامنت با
پاسخ ادمین به صورت random و rate ببینم (seed data). بعنوان متخصص UI/UX این موارد بررسی و اعمال کن.»

## Findings (investigated 2026-09-25, before any change)

- F1 Web (back.nahalkmi.ir): a customer cannot write a review at all. `reviewsApi.submit` has no caller; `ReviewForm.vue`
  is mounted only in the edit modal. My Bookings, the bookings sidebar, booking detail and both salon pages have no
  «ثبت نظر». The review-request notification leads to the booking detail page, which has none either.
- F2 Customer app (customer.nahalkmi.ir): «ثبت نظر» exists only on the booking detail page, only when the status is
  `Completed`. Only the salon can complete a booking (`POST /Bookings/{id}/complete`); nothing completes one by itself.
  A past booking the salon never marked done shows no review action and no reason.
- F3 A booking the salon entered for the customer's mobile (walk-in / phone booking) is stored with the salon OWNER as
  `CustomerId`; the customer sees it in «نوبت‌های من» (customer-sees-salon-bookings), but reviewing it is refused
  (`Forbid()`, empty 403) — and the salon owner, as its nominal customer, could review their own salon.
- F4 No booking payload says whether a review exists. The app remembers "reviewed" only in memory: reopening the
  booking offers «ثبت نظر» again, and the second submit fails.
- F5 Every refusal reads «ثبت نظر ناموفق بود»: the server's reason is in `errors[0].message` (the app reads `message`),
  the 403 has no body, and the server's messages are English («Cannot review booking with status…»).
- F6 Public reviews name the reviewer «Customer 3fa85f64» (a GUID prefix, in English); the web card drops the name.
- F7 Seeding: `BookingSeeder.cs`/`BookingDataSeeder.cs` are empty, so `ReviewSeeder` (reviews for completed bookings)
  has never produced a review. The 09-22 task 6.1 ("seed سالن نهال with test reviews") was checked off unproven.
  Demo data runs only where `Database:SeedOnStartup` is on (Development by default) — never in production.
- F8 Provider app offers «تکمیل»/«عدم حضور» on pending bookings and before the server would accept them.
- F9 Customer app salon page: a failed reviews request shows «هنوز نظری ثبت نشده است»; only the first 20 load.

## Acceptance scenarios

- Given a completed booking of mine without a review, its list row and detail carry `canReview`; the customer app and
  the web offer «ثبت نظر» on the booking card and the booking page.
- Given a confirmed booking whose time has passed and the salon has not marked it done, it carries
  `reviewBlockedReason` in Persian and the apps show the action disabled with that reason.
- Given I reviewed a booking, it carries `reviewId` and `reviewStatus`; the apps show «نظر شما ثبت شد» with its state
  («در انتظار تأیید» / «منتشر شده») instead of the button, also after leaving and coming back.
- Given the salon entered a booking for my verified mobile and completed it, I can review it and view it; the salon
  owner cannot review their own salon through it.
- Every refusal of a review or vote reads in Persian, and the apps show the server's reason.
- A public review names its author by first name and surname initial («ناصر ع.»), or «مشتری» — never a phone or id.
- A development environment starts with سالن نهال carrying published reviews of mixed ratings, Persian comments that
  match the rating, optional dimensions, a published salon reply on most, and helpful/not-helpful votes; its rating is
  computed from them; seeding again adds nothing.
- Customer app salon page: a failed load says so with «تلاش دوباره»; more than 20 reviews can be read.
- Provider app: «تکمیل» only for a confirmed booking from 15 minutes before its start; «عدم حضور» only after its end.

## Tasks

- [x] 1 API: customer booking list + detail carry canReview, reviewBlockedReason, reviewId, reviewStatus (integration)
- [x] 2 API: a salon-entered booking is reviewable/viewable by the person with its verified mobile, never by the owner
- [x] 3 API: review and vote refusals in Persian, 403 with a readable body (unit + integration)
- [x] 4 API: public reviews name the author «first-name initial.» or «مشتری» (integration)
- [x] 5 Seed: سالن نهال demo reviewers, completed visits, reviews, salon replies, votes; idempotent (unit + integration)
- [x] 6 Customer app: review state from the server on the booking card and page (button / reason / submitted state)
- [x] 7 Customer app: review/vote errors show the server's Persian reason
- [x] 8 Customer app: salon page reviews — error with retry, load more, author name
- [x] 9 Web: «ثبت نظر» on My Bookings and the bookings sidebar (modal with ReviewForm), states, author name on cards
- [ ] 10 Provider app: «تکمیل»/«عدم حضور» offered only when the server accepts them
- [?] 11 DECISION: should a confirmed booking complete by itself some hours after its end (unless marked no-show)?
- [?] 12 DECISION: put the Nahal demo reviews on production, where real customers would read them as genuine?
- [ ] 13 FULL verify

## Decisions

- D1 (tier 2, flagged) A booking the salon entered is reviewable by the person with its client-book entry's verified
  mobile — the rule customer-sees-salon-bookings already applies to «نوبت‌های من» — and that person can open it
  (`GET /Bookings/{id}` 403'd them). The salon owner, stored as such a booking's customer, can no longer review their
  own salon through it (they could, 201). One rule, `IBookingCustomer.IsForAsync`, for list, page and review.
- D2 (tier 2) Review state is additive on both customer booking payloads: `canReview`, `reviewBlockedReason`
  (Persian; only "the visit is over and the salon has not marked it done"), `reviewId`, `reviewStatus`. On the booking
  page they are filled only for the person the booking is for, so the salon's own view never says «canReview».
- D3 (tier 2, flagged) A public review is signed «first name + surname initial.» («ناصر ع.»), the first name alone
  without a surname, «مشتری» without a real name — was «Customer 3fa85f64». The full surname is not shown publicly;
  if you want full names (or first name only), it is one function: `PersonName.ForPublicReview`.
- D5 (tier 2) Seed: `DemoSalonReviewsSeeder` (ServiceCatalog) + `DemoSalonReviews` (host, creates the reviewers in the
  person directory, since it is the one project that reaches both contexts). 12 named demo customers (numbers
  +98999000100x), 18 reviews on completed past visits, fixed seed 1403 («random» but the same everywhere): average 4.0,
  ratings 1–5, 14 with the salon's approved reply (every poor one), votes by the other reviewers, dates back-dated to
  after each visit, salon rating recomputed. Marked by `ModeratedBy = 'DemoSalonReviewsSeeder'` (the save overwrites
  CreatedBy), which is also how a re-run skips and how they can be removed. Own switch `Database:SeedDemoReviews`,
  defaulting to `SeedOnStartup` (Development only). The old `ReviewSeeder` is left as is: it still reviews completed
  bookings if a booking seeder ever exists.
- D4 (tier 1) Refusals are translated at the source (review aggregate, review commands, create-review controller), with
  the same exception types and status codes; validation errors keep their field key («Rating», «SkillRating», «Comment»).

## Log

- 2026-09-25 Investigation: backend (reviews, bookings, seeding), customer app, provider app, web + admin traced.
  Toolchain in this container: .NET 10 SDK from Ubuntu's feed (Microsoft's is blocked by the network policy), tests
  run on it with DOTNET_ROLL_FORWARD=Major; Flutter 3.44.2 (CI's version); Docker daemon started for Testcontainers.
