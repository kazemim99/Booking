Status: ACTIVE
Verify: FULL

User report (2026-09-24 morning): a screen recording (10:33–10:40 Tehran) of production after the 2026-09-24 deploy.
The salon tries to mark a 10:00 appointment done at 10:33 and is refused with "Booking cannot be completed before
scheduled time"; a salon member is asked for a name again although onboarding took one; the customer's book entry
still carries the phone-contact name «Mostafa Cell». User said "go" to: (1) fix the wall-clock/UTC-now comparison
test-first, (2) sync a customer's confirmed name into the salon's client book, (3) investigate the provider name
re-prompt and the confirm-screen customer name; push delivery and "name right after OTP" wait on the user's answers.

## Findings

- T1 root cause: booking times are salon WALL-CLOCK values (FOLLOW-UPS #63, WallClockDateTimeConverter). Every
  business rule compared them with `DateTime.UtcNow`, and .NET compares DateTimes by digits whatever their Kind, so
  "now" was 3:30 behind the salon: completing a 10:00 booking was refused until 13:15, a no-show until 3:30 after the
  end. The same frame error, in the loose direction, let a customer book (and see slots for) the last 3:30, let the
  booking/reschedule/cancel-fee windows run 3:30 long, queued reminders 3:30 late (the "2h before" SMS would go out
  1:30 AFTER the appointment), sent the 08:00 salon digest at 11:30, and measured the refund window 3:30 long.
  Sites: Booking (Confirm, Cancel, Reschedule, Complete, MarkAsNoShow, IsInPast, IsUpcoming), CreateBookingCommandHandler,
  AvailabilityController, AvailabilityService (constraints, day check, slot list), GetProviderProfile next slot,
  GetProviderAvailabilitySummary/GetProviderProfile/MemberBookabilityService/ProviderAvailability "today",
  BookingReadRepository upcoming, ProviderCustomer booking stats, BookingReminderScheduler, DailyScheduleDigestJob,
  refund-on-cancel, UserManagement CustomerBookingHistoryEntry + GetUpcomingBookings.

## Acceptance scenarios

- Given a confirmed 10:00 booking, when the salon marks it done at 10:33 salon time, then it is completed.
- Given a confirmed 10:00 booking, when the salon marks it done at 09:40, then it is refused (15-minute grace stays).
- Given a booking that ended an hour ago in salon time, a no-show can be recorded; it counts as past, not upcoming.
- Given a booking 22h ahead in salon time, it is "upcoming (within 24h)"; with a 24h reschedule window it cannot be moved.
- Given a 2h minimum-advance policy, a request for one hour from now (salon time) cannot be confirmed.
- A customer cannot book a start earlier than salon "now"; today's slot list offers nothing earlier than salon "now".
- The "2h before" reminder for a 10:00 booking is queued for 08:00 salon time = 04:30 UTC.
- The salon's morning digest runs at 08:00 salon time.

## Tasks

- [x] 1 One canonical salon clock (Core.Domain `SalonTime`: fixed +03:30, FromUtc/ToUtc/Now) with unit tests
- [x] 2 Booking aggregate rules measured in salon time (failing domain tests first)
- [x] 3 Application/infrastructure sites use salon time; reminders queued as real UTC instants (failing tests first)
- [x] 4 POST /bookings/{id}/complete at 33 minutes past start succeeds (integration)
- [x] 5 A customer's confirmed real name reaches the salon's client-book entry for them
- [x] 6 Provider name re-prompt after onboarding (fixed); customer name headline on the salon's request row + sheet
- [ ] 7 FULL verify

## Decisions

- D1 (tier 2) Iran's fixed offset +03:30, not `TimeZoneInfo("Asia/Tehran")`: Iran abolished DST in 2022, the
  containers may lack tzdata, and there is no per-provider zone anywhere (FOLLOW-UPS #63). One constant in one place is
  the seam where a real per-salon zone would go.
- D2 (tier 2) Instants stay UTC (CreatedAt, CompletedAt, ScheduledFor, ...). Only comparisons between an instant and
  a booking time convert, and they convert the instant into salon time — never relabel a wall-clock value as UTC.
- D4 (tier 2) Client-book rename happens when the customer books THAT salon themselves (the moment they share
  their name with it), only with a real first AND last name, never for other salons holding the same number. A later
  profile rename does not propagate; self-bookings still do not create client-book entries (unchanged).
- D5 (tier 2) Provider app: `refreshProviderStatus` re-mints the token first, so the session carries the name the
  server holds (onboarding's draft call adopts the owner's name onto the account; the app kept the sign-in token's
  placeholder and asked «نام شما ثبت نشده»).
- Surfaces: time rules and client-book naming are server-side; customer apps compare wall-clock times with the
  device clock, correct on Iranian devices, so nothing needed there. The request-row/sheet change is the Flutter
  provider app (the live salon surface); admin: nothing needed.
- D3 (tier 2) Refund-on-cancel measures the same stated policy in salon time; this corrects the measurement, not the
  policy (no deposit flow is live).

## Log

- 2026-09-24 worktree .claude/worktrees/qa0924 on ba2158b7.
- Tests seen failing first: SalonTime (compile), 6 domain rules, 5 integration (reminder 08:00 vs 04:30Z, digest
  silent at 08:05, 09:00 slots offered at 11:34, 201 for an hour ago, started appointment still upcoming), client-book
  rename, provider session name after onboarding, request-row name headline. RescheduleNotificationTests expected the
  old wall-clock send time; corrected. Targeted runs: 731 domain, 311+22 integration, 283 provider-app tests green.
