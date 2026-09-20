# Booking Reminders

Time-based notifications tied to an appointment: scheduled when the appointment becomes real, moved when it
moves, and cancelled when it stops being real.

## ADDED Requirements

### Requirement: Reminders are scheduled when a booking is confirmed
When a booking is confirmed, the system SHALL schedule its reminders relative to the appointment's start
time: reminders for the customer, and a next-appointment reminder for the provider or the assigned staff
member.

A reminder whose scheduled time has already passed at the moment of confirmation SHALL NOT be scheduled — a
booking made an hour before the appointment SHALL NOT immediately fire a reminder meant for the day before.

#### Scenario: Confirming schedules the reminders
- **WHEN** a booking is confirmed for a future date
- **THEN** its reminders are scheduled at their offsets before the appointment start

#### Scenario: A late booking skips elapsed reminders
- **WHEN** a booking is confirmed for a time sooner than a reminder's offset
- **THEN** that reminder is not scheduled and no reminder fires immediately

### Requirement: Reminders follow the booking
When a booking is rescheduled, its pending reminders SHALL be rescheduled against the new start time. When a
booking is cancelled, completed, or marked no-show, its pending reminders SHALL be cancelled.

A reminder SHALL NOT be sent for an appointment that is no longer going to happen.

#### Scenario: Rescheduling moves the reminders
- **WHEN** a confirmed booking is rescheduled to a different time
- **THEN** its pending reminders are rescheduled relative to the new start time

#### Scenario: Cancelling removes the reminders
- **WHEN** a confirmed booking is cancelled
- **THEN** its pending reminders are cancelled and none is sent

#### Scenario: A completed booking sends no further reminder
- **WHEN** a booking is marked completed before a pending reminder is due
- **THEN** that reminder is cancelled

### Requirement: A reminder is sent once
Each scheduled reminder SHALL be sent at most once, regardless of how many times the scheduling sweep runs or
how many times the booking is saved.

#### Scenario: Repeated sweeps send one reminder
- **WHEN** the scheduled-notification sweep runs repeatedly around a reminder's due time
- **THEN** the recipient receives that reminder exactly once

#### Scenario: Re-confirming does not duplicate
- **WHEN** a booking's confirmation is processed more than once
- **THEN** its reminders are scheduled once, not twice

### Requirement: Reminders are rendered in the salon's local time
A reminder SHALL state the appointment time in the salon's local wall-clock and in the Jalali calendar,
matching the rest of the product's notification copy.

#### Scenario: Reminder shows local wall-clock
- **WHEN** a reminder is rendered for an appointment
- **THEN** it shows the appointment's local wall-clock time and its Jalali date
