Status: STOPPED(decision)
Verify: FULL

User request (2026-09-25, with 5 screenshots of the live customer app): «همه چیز دیپلوی شد.»
1. امتیاز کلی («امتیاز شما») جدا نباشد؛ کاربر امتیاز با جزئیات بدهد.
2. امتیاز و تعداد نظر کنار هم «۴.۰ ۱ نظر» مثل «۱۴» خوانده می‌شود — در همه کارت‌ها.
3. «Customer 8156da0c» در نظرها؛ نام و نام خانوادگی نمایش داده شود، با گزینه‌ای برای مشتری که نامش نمایش داده نشود.
4. هنوز جاهایی «بوکسی» است.
5. دکمه مسیریابی: اول مسیریاب‌های نصب‌شده روی گوشی را پیشنهاد دهد، مثل سایر اپلیکیشن‌ها.
6. اگر سالن هیچ‌وقت «تکمیل» را نزند، مشتری هیچ‌وقت نمی‌تواند نظر بدهد.
7. «پاسخ سالن» ساده و بی‌روح است و معلوم نیست پاسخ به همان نظر است.
8. هر مشتری برای هر سالن فقط یک نظر؛ می‌تواند آن را ویرایش کند.
9. پس از تأیید نوبت، مشتری تا دو ساعت قبل بتواند زمان را تغییر دهد؛ زمان جدید دوباره به تأیید سالن برسد.

## Findings (investigated 2026-09-25, before any change)

- F0 The live server does not run this code. Deploy runs #111 and #112 were cancelled with no runner ever taking the
  deploy job (runner_id 0); master since PR #32 has not reached the box. The current code cannot print
  «Customer 8156da0c» (`PersonName.ForPublicReview` is the only author label since PR #32), the 18 Nahal demo reviews
  are absent (one review), and the header «بوکسی» is gone from source (both `web/index.html` and `web/manifest.json`
  were fixed in 58c750c). Items 3's label and 4 are the stale deploy, not code; they get no code change beyond a
  leftover in `docs-site/`.
- F1 Review form (web `ReviewForm.vue`, app `write_review_dialog.dart`): a required overall row plus four optional
  dimensions behind a disclosure; the overall is deliberately NOT derived from them. `Rating` accepts 1–5 in 0.5 steps.
- F2 App `core/widgets/provider_rating.dart`: «⭐ ۴.۰» and «۱ نظر» are 4 px apart with no separator, so RTL reads
  «۱ ۴.۰ نظر» as one number. Web: four hand-written variants (detail, favorites, quick-rebook), and the search
  `ProviderCard.vue` shows no rating at all.
- F3 No per-review choice to hide the author: `ForPublicReview` («ناصر ع.») always applies when a name is on file.
- F5 App: `provider_location_card.dart` already offers نشان / بلد / گوگل‌مپ as web links; nothing opens the phone's own
  chooser of installed map apps. Web: `ProfileAbout.vue` opens Google Maps only (a TODO says so).
- F6 Only the salon can complete a booking; nothing completes one by itself (open DECISION 11 of
  customer-reviews-and-nahal-seed). Hosted-service pattern to copy: `DailyScheduleDigestService`.
- F7 Reply block: a grey box (app) / grey quote with a thin border (web), no link to the review it answers.
- F8 One review per BOOKING (`GetByBookingIdAsync` check + unique index on BookingId): three visits, three reviews.
  The booking payloads' review state is per booking too.
- F9 Window: `BookingPolicy.RescheduleWindowHours`, salon setting, default 24, copied onto every booking. A reschedule
  already creates a new booking in `Requested` (the salon confirms it again) and closes the old one `Rescheduled`.

## Acceptance scenarios

- The review form has no separate overall row: the four aspects are required, the overall shown and stored is their
  average to the nearest half star, visible live as «امتیاز کلی: ۴.۵».
- Rating and count read as two things everywhere: «⭐ ۴.۰ · ۱ نظر»; the web search card shows it too.
- A public review is signed with the author's full name by default; when the author ticked «نامم نمایش داده نشود»
  it reads «مشتری». The choice can be changed on edit.
- «مسیریابی» first offers «برنامه‌های مسیریابی گوشی» (the phone's own chooser: `geo:` on Android, Apple Maps on
  iOS), then نشان / بلد / گوگل‌مپ / ویز, in the app and on the web.
- A confirmed booking whose end is 12 hours past and was not marked done or no-show becomes Completed by itself; the
  customer can then review it.
- The salon reply reads as the salon's answer to that review: indented under it, the salon's name, tinted, a reply
  icon.
- A customer with a review for a salon cannot post a second one from another visit; that visit's card shows
  «ویرایش نظر» (while editable) instead of «ثبت نظر».
- A customer can move a confirmed booking until 2 hours before it; the moved booking waits for the salon's
  confirmation, and the customer is told so before and after.

## Tasks

- [ ] 1 API: overall rating derived from the four aspects when not sent (half-star rounding) (unit)
- [ ] 2 API: `showName` on create/edit review; public label full name or «مشتری»; migration (unit + integration)
- [ ] 3 API: one review per (customer, salon); booking review state per salon; refusal names the edit (integration)
- [ ] 4 API: auto-complete confirmed bookings 12 h after their end (hosted service) (unit + integration)
- [ ] 5 API: reschedule window default 2 h; migrate the old default 24 → 2 on salons, services, open bookings
- [ ] 6 Seed: Nahal demo — one review per reviewer (18 reviewers)
- [x] 7 Customer app: review form aspects-only with live overall + «نامم نمایش داده نشود»; edit from booking
- [x] 8 Customer app: rating · count separator; reply redesign; installed-apps directions first; reschedule notice
- [ ] 9 Web: review form aspects-only + name choice; shared rating display (+ search card); reply redesign;
      directions chooser; reschedule notice; «ویرایش نظر»
- [x] 10 Docs: runbook note on F0 (docs-site «بوکسی» left: docs/KNOWLEDGE_MAP.md marks docs-site historical)
- [ ] 11 FULL verify

## Decisions

- D1 (tier 2, flagged) Overall = average of the four aspects, nearest half star, all four required in the form. The
  API still accepts an explicit `rating` (older app versions in the field) and derives it only when absent.
- D2 (tier 2, flagged) Public name: full name («ناصر عابدی») by default, «مشتری» when the author opts out. Replaces
  D3 of customer-reviews-and-nahal-seed («ناصر ع.»), per this request. Existing reviews keep showing names (default).
- D3 (tier 2, flagged) Auto-complete after 12 hours past the end — a salon's working day to mark a no-show first. One
  constant (`BookingAutoCompletion.After`).
- D4 (tier 2, flagged) One review per (customer, salon) enforced in the command, not by a unique index: existing
  duplicates (possible today) would fail the migration on deploy. The booking-level unique index stays.
- D5 (tier 2, flagged) Window 2 h: new default; rows still at the old default (24) move to 2; a salon that chose
  another value keeps it.
- D6 (tier 1) Items 3 (label) and 4 are the stale deploy (F0): no app change.

## Log

- 2026-09-25 Tasks 7–8 (customer app): analyze clean, 868 tests. Also: after a reschedule the detail screen and list
  follow the NEW booking (was reloading the closed one); status `Rescheduled` labelled «تغییر زمان داده شد» (fell
  through the badge mapping). Asked the API for `reviewBookingId` on booking payloads and `newBookingId` on the
  reschedule response (the app parses the message's guid until then).

- 2026-09-25 STOPPED(decision): the user decides whether `origin` stays on the renamed repository URL
  (github.com/kazemim99/AsanRezerve) or goes back to the session's configured github.com/kazemim99/Booking — the
  permission check refused further work citing "Remote Repoint". Tasks 1–9 are being implemented by three
  background agents (backend / customer app / web); nothing is pushed until the user answers.

- 2026-09-25 Investigation (5 parallel read-only passes): review UI, author naming, brand leftovers, directions,
  review/booking backend rules.
