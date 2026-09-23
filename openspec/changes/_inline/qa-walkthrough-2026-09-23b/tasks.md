Status: ACTIVE
Verify: FULL

User report (2026-09-23 evening): a 5:12 screen recording (20:46–20:51 Tehran) of production BEFORE the
customer-app-ux-review-fixes deploy: customer books «اصلاح سر با ماشین» Thu 2 Mehr 10:30 at سالن نهال, then the salon
confirms it in the provider app. User said "go" on the triage, with the defaults: web push now; the customer's name is
required at booking confirmation (the login stays phone-only). Each surface — customer (Flutter + Vue), provider, admin
— is checked per item (memory: plan all three frontends).

## Findings

- T1 root cause (confirmed in code + frames): booking times are salon WALL-CLOCK values (FOLLOW-UPS #63). Since
  00dd2777 (2026-09-11) UtcDateTimeConverter reads every DateTime back as Kind=Utc, so the API serializes "10:30:00Z".
  Both Flutter apps parse and `.toLocal()` it (+3:30 → 14:00); the Vue apps `new Date(...)` it (same shift in a Tehran
  browser). Server-built notification text formats the digits and is right (10:30). The provider calendar's END time is
  read as clock text (right) and its START via toLocal (wrong) — hence "11:00 تا 14:00" for a 10:30–11:00 booking.
  Yesterday's 3.0 fix corrected the SEND side; before it, send (UTC) and read (+3:30) errors cancelled out.

## Tasks

- [x] 1 P0: booking times read back as zone-less wall clock (TimeSlot converter); every booking API returns "10:30:00"
- [x] 2 P0: both Flutter apps read booking times as wall-clock digits whatever zone suffix arrives (no .toLocal())
- [x] 3 A person's phone number is never shown as their name (confirm step «ارائه‌دهنده 9123135143», salon app header)
- [x] 4 The salon app asks an owner/member whose name is the OTP placeholder for a real name, once
- [x] 5 After the salon confirms (or declines) a booking, the customer gets an inbox notice
- [ ] 6 Web push: customer and salon web apps register for push and show notifications on Android Chrome
- [x] 7 Salon profile: one «تماس و موقعیت» section holding address + map; «موقعیت روی نقشه» duplicate removed
- [x] 8 Appointment detail shows the staff member's name and what «در انتظار تأیید» means
- [x] 9 A customer without a real name must enter it before a booking can be confirmed (no skip there)
- [x] 10 Salon profile says reviews can be written after a completed visit, from the appointment
- [x] 12 Only the salon (owner / booking-managing member / admin) can confirm, complete, no-show or staff its bookings
- [?] 13 DECISION: GET /providers/{id}/hierarchy/members only requires sign-in — any signed-in user can read any
      salon's member list WITH phone numbers. Restrict to the salon (CanManageProvider) and drop phones for others?
- [ ] 11 FULL verify green; deploy after the user's go

## Decisions

- T2 (1) WallClockDateTimeConverter on Booking.TimeSlot only: stored as before, read Kind=Unspecified, so every booking
  API writes zone-less digits; fixes the Vue apps without touching them. Clients also parse digits defensively.
- T2 (12) Found by the notice fix: confirm/complete/no-show/assign-staff had no ownership check (another salon → 200).
  Fixed with the existing CanManageProvider rule, not a new policy.

- USER 2026-09-23: web push now (Android Chrome; iOS needs the site added to the home screen); the name is required
  at booking confirmation, not at login.

## Log

- 2026-09-23 3-4 done by an agent (4084ccef.. cherry-picked; two agents' overlapping name helpers merged into one
  rule: realFullNameOrNull delegates to personNameOrNull). Root cause of the confirm step's «ارائه‌دهنده 9123135143»:
  3.4 made the owner's FullName "" (no display name), and GET /Providers/{id} rebuilt the placeholder from the raw
  FirstName/LastName; single-staff booking reads that name, not the slot's. Fixed in backend, customer app, salon app
  (asks for a real name after OTP, never shows the phone as a name), Vue web header/staff and admin users list.
  Stored placeholders are not rewritten; every screen hides them. Customer 726 / salon 647 tests green.

- 2026-09-23 7-10 done by an agent (c76f9e6f.. cherry-picked), plus f3e304b4: booking requires first AND last name
  (the agent's check accepted a first name alone). Customers signed in before this build have no stored name and are
  asked once at their next booking. Customer app 713 tests green.

- 2026-09-23 1+2 done (26c7dda6): integration RED showed "2026-09-25T10:30:00Z" on the salon calendar; salon-app RED
  read 10:30 as 14:00. Full integration 810/810 (one flake on a loaded first run, green on re-run).
- 2026-09-23 5 done by an agent (c83cf481): confirm raised no notification at all — never wired in notification-system
  7.1. 12 done (f5b23cf9).

- 2026-09-23 Opened from the recording; T1 root cause confirmed before any code.
