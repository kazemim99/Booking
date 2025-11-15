# Customer Booking Specification

## Overview
Enable customers to book appointments with service providers through a guided multi-step wizard, including service selection, time slot booking, payment processing, and booking management.

---

## ADDED Requirements

### Requirement: Booking Wizard Flow
Customers must complete booking through a guided multi-step wizard.

#### Scenario: Start booking from provider details
**Given** a customer is viewing a provider's profile
**When** they click "Book Now" on a specific service
**Then** the booking wizard opens with the service pre-selected
**And** they see a step indicator showing "Step 1 of 5"
**And** they can navigate back to cancel

#### Scenario: Complete all booking steps
**Given** a customer starts the booking wizard
**When** they complete each step in sequence:
1. Select service and options
2. Choose date and time slot
3. Enter customer details and notes
4. Review booking summary
5. Process payment
**Then** they receive a booking confirmation
**And** they see a booking reference number
**And** they receive a confirmation email

#### Scenario: Navigate between wizard steps
**Given** a customer is in step 3 of the wizard
**When** they click "Back"
**Then** they return to step 2 with their selections preserved
**And** they can click "Next" to proceed again
**And** no data is lost when navigating

#### Scenario: Cancel booking wizard
**Given** a customer is in the middle of booking
**When** they click "Cancel" or close the wizard
**Then** they see a confirmation dialog "Are you sure?"
**And** if confirmed, all selections are cleared
**And** they return to the previous page

---

### Requirement: Service Selection
Customers must select a service, optional add-ons, and preferred staff member.

#### Scenario: Select service
**Given** a customer is in the service selection step
**When** they view available services
**Then** they see all provider's services grouped by category
**And** each service shows name, description, price, and duration
**And** they can select one service with a radio button

#### Scenario: Add service options
**Given** a customer has selected "Women's Haircut"
**When** they view available options
**Then** they see add-ons like "Hair Coloring", "Blow Dry"
**And** each option shows additional price
**And** they can select multiple options with checkboxes
**And** the total price updates automatically

#### Scenario: Select preferred staff
**Given** the provider has multiple staff members
**When** the customer views staff selection
**Then** they see all staff members with photos
**And** each staff member shows specialties
**And** they can select "No Preference" option
**And** selecting a specific staff may affect available time slots

#### Scenario: View pricing breakdown
**Given** a customer has selected service and options
**When** they view the price summary
**Then** they see:
- Base service price
- Each option price
- Subtotal
- Deposit amount (if required)
- Total amount
**And** prices are in Iranian Rial (IRR)
**And** numbers use Persian numerals

---

### Requirement: Date and Time Selection
Customers must select appointment date and time from available slots.

#### Scenario: View available dates
**Given** a customer is in the date/time selection step
**When** the Persian calendar loads
**Then** they see current month in Jalali calendar
**And** past dates are disabled
**And** dates beyond maximum advance booking window are disabled
**And** dates with no availability are visually marked
**And** today is highlighted

#### Scenario: Select available date
**Given** a customer views the calendar
**When** they click on an available date
**Then** time slots for that date are loaded
**And** time slots are displayed in a grid
**And** each slot shows start time and service duration
**And** unavailable slots are disabled

#### Scenario: Select time slot
**Given** time slots are displayed
**When** a customer clicks an available slot
**Then** the slot is highlighted as selected
**And** the booking summary shows selected date and time
**And** they can proceed to the next step

#### Scenario: No available slots
**Given** a customer selects a date
**When** no time slots are available
**Then** they see "No available slots for this date"
**And** they see suggestion to try another date
**And** alternative dates with availability are suggested

#### Scenario: Time zone handling
**Given** all times are stored in UTC
**When** a customer views time slots
**Then** times are displayed in Tehran timezone
**And** the timezone is clearly indicated
**And** date conversions are accurate

#### Scenario: Staff availability affects slots
**Given** a customer selected a specific staff member
**When** they view available time slots
**Then** only slots when that staff member is available are shown
**And** if staff has a break, those slots are excluded

---

### Requirement: Customer Details and Notes
Customers must provide contact information and optional booking notes.

#### Scenario: Pre-filled customer information
**Given** a customer is logged in
**When** they reach the customer details step
**Then** their name, email, and phone are pre-filled
**And** they can edit any information
**And** changes are saved to their profile

#### Scenario: Add customer notes
**Given** a customer has special requirements
**When** they type in the notes field
**Then** they can provide up to 500 characters
**And** character count is displayed
**And** notes are included in the booking confirmation

#### Scenario: Required fields validation
**Given** a customer attempts to proceed
**When** required fields are empty
**Then** validation errors are shown
**And** the "Next" button is disabled
**And** error messages are in Persian

---

### Requirement: Booking Review and Confirmation
Customers must review all booking details before payment.

#### Scenario: Review booking summary
**Given** a customer reaches the review step
**When** the summary page loads
**Then** they see all booking details:
- Provider name, address, phone
- Selected service with options
- Selected staff member (or "No Preference")
- Appointment date and time (Persian format)
- Customer name, phone, email
- Customer notes
**And** they see pricing breakdown
**And** they see cancellation policy

#### Scenario: Edit booking details
**Given** a customer reviews their booking
**When** they click "Edit" next to any section
**Then** they return to that specific step
**And** they can modify the selection
**And** they return to the review step after editing

#### Scenario: Accept terms and conditions
**Given** a customer reviews their booking
**When** they proceed to payment
**Then** they must check "I agree to cancellation policy"
**And** the "Proceed to Payment" button is enabled only after agreement

---

### Requirement: Payment Processing
Customers must complete payment to confirm booking.

#### Scenario: View payment amount
**Given** a customer proceeds to payment
**When** the payment page loads
**Then** they see the amount to pay (deposit or full amount)
**And** they see what the payment covers
**And** they see remaining balance (if deposit only)

#### Scenario: Process payment via ZarinPal
**Given** a customer clicks "Pay Now"
**When** payment initializes
**Then** they are redirected to ZarinPal gateway
**And** they can choose payment method (card, wallet, etc.)
**And** they complete payment on ZarinPal
**And** they are redirected back to Booksy

#### Scenario: Successful payment
**Given** a customer completes payment
**When** they return from ZarinPal
**Then** payment is verified with backend
**And** booking status changes to "Confirmed"
**And** they see a success message with booking reference
**And** they receive a confirmation email
**And** they can view booking details

#### Scenario: Failed payment
**Given** payment fails on ZarinPal
**When** the customer returns to Booksy
**Then** they see "Payment Failed" message
**And** they see the reason for failure
**And** they can retry payment
**And** booking remains in "Pending Payment" status

#### Scenario: Payment timeout
**Given** a customer doesn't complete payment within 15 minutes
**When** the payment session expires
**Then** the booking is automatically cancelled
**And** the time slot is released
**And** the customer is notified

---

### Requirement: Booking Management
Customers must be able to view and manage their bookings.

#### Scenario: View all bookings
**Given** a customer navigates to "My Bookings"
**When** the page loads
**Then** they see bookings grouped by status:
- Upcoming (confirmed bookings in the future)
- Past (completed bookings)
- Cancelled (cancelled bookings)
**And** upcoming bookings show countdown timer
**And** each booking shows provider, service, date, time, status

#### Scenario: View booking details
**Given** a customer clicks on a booking
**When** the booking details page loads
**Then** they see complete booking information
**And** they see provider contact and address
**And** they see payment receipt
**And** they see booking history/timeline
**And** they can get directions to provider
**And** they can add appointment to calendar (iCal/Google)

#### Scenario: Cancel upcoming booking
**Given** a customer has an upcoming booking
**When** they click "Cancel Booking"
**Then** they see cancellation confirmation dialog
**And** they see refund amount based on cancellation policy
**And** they can provide cancellation reason
**And** if confirmed, booking status changes to "Cancelled"
**And** they receive refund according to policy
**And** they receive cancellation confirmation email

#### Scenario: Cancellation policy enforcement
**Given** a customer tries to cancel within restricted window
**When** they click "Cancel Booking"
**Then** they see "Cancellation not allowed" message
**And** they see minimum notice requirement
**And** they can contact provider for exceptions
**And** the cancel button is disabled

#### Scenario: Reschedule booking
**Given** a customer wants to change appointment time
**When** they click "Reschedule"
**Then** they see a calendar with available alternative slots
**And** they can select a new date and time
**And** they see any price difference
**And** they confirm rescheduling
**And** booking is updated with new time
**And** they receive rescheduled confirmation

#### Scenario: Booking reminders
**Given** a customer has an upcoming booking
**When** 24 hours before appointment
**Then** they receive a reminder email
**And** they receive a reminder SMS (if enabled)
**When** 1 hour before appointment
**Then** they receive another reminder
**And** reminders include provider address and contact

---

### Requirement: Booking Availability Calculation
System must accurately calculate and display available time slots.

#### Scenario: Calculate available slots
**Given** a customer selects a date
**When** availability is calculated
**Then** the system considers:
- Provider's business hours for that day
- Service duration + preparation time + buffer time
- Existing confirmed bookings
- Staff member working hours (if specific staff selected)
- Provider's time off / holidays
**And** only truly available slots are shown
**And** calculation completes within 500ms

#### Scenario: Prevent double booking
**Given** two customers try to book the same slot simultaneously
**When** the second customer completes their booking
**Then** they see "This slot is no longer available"
**And** they are asked to select another slot
**And** the slot is removed from available options

#### Scenario: Consider service duration
**Given** a service requires 90 minutes
**When** a customer views 2:00 PM slot
**Then** the system checks if 2:00 PM - 3:30 PM is completely free
**And** if any overlap exists, the slot is not shown
**And** buffer time is added between bookings

---

### Requirement: Booking Notifications
Customers and providers must receive appropriate notifications.

#### Scenario: Booking confirmation email
**Given** a customer successfully books an appointment
**When** payment is confirmed
**Then** they receive an email containing:
- Booking reference number
- Provider name, address, phone, website
- Service name and duration
- Appointment date and time (Persian format)
- Total price paid
- Cancellation policy
- "Add to Calendar" link
- "Cancel Booking" link

#### Scenario: Booking confirmation SMS
**Given** a customer provides phone number
**When** booking is confirmed
**Then** they receive an SMS with:
- Booking reference
- Provider name
- Appointment date and time
- "View Details" link

#### Scenario: Cancellation confirmation
**Given** a customer cancels a booking
**When** cancellation is processed
**Then** they receive email confirming cancellation
**And** they see refund amount and timeline
**And** they see provider contact for questions

#### Scenario: Rescheduling confirmation
**Given** a customer reschedules a booking
**When** reschedule is confirmed
**Then** they receive email with updated appointment details
**And** old appointment time is clearly marked as cancelled
**And** new appointment time is highlighted

---

## MODIFIED Requirements

### Requirement: Customer Authentication (from authentication spec)
Customer registration must collect necessary information for booking.

#### Scenario: Register with booking intent
**Given** a guest customer tries to book
**When** they click "Book Now"
**Then** they are prompted to register or login
**And** after registration, they return to booking wizard
**And** their details are pre-filled in the booking form

---

## Non-Functional Requirements

### Performance
- Availability calculation must complete within 500ms
- Booking creation must complete within 1 second
- Payment processing must handle network delays gracefully
- All API calls must have proper timeout handling

### Reliability
- Booking data must not be lost during payment redirect
- Failed payments must not create orphaned bookings
- Concurrent booking attempts must be handled with locking
- All booking operations must be transactional

### Security
- Payment information must never be stored client-side
- All payment redirects must use HTTPS
- Booking cancellation must verify customer ownership
- API endpoints must validate authentication tokens

### Usability
- Wizard must show clear progress indication
- All steps must have validation with helpful error messages
- Mobile experience must be touch-optimized
- Loading states must be visible during async operations

### Localization
- All dates in Persian calendar (Jalali)
- All text in Persian (Farsi)
- Right-to-left (RTL) layout
- Persian numerals for all numbers
- Currency in Iranian Rial (IRR)

---

## Technical Notes

### API Endpoints
- `POST /api/v1.0/bookings` - Create booking
- `GET /api/v1.0/bookings/available-slots` - Get available time slots
- `GET /api/v1.0/bookings/my-bookings` - Get customer's bookings
- `GET /api/v1.0/bookings/{id}` - Get booking details
- `POST /api/v1.0/bookings/{id}/cancel` - Cancel booking
- `POST /api/v1.0/bookings/{id}/reschedule` - Reschedule booking
- `POST /api/v1.0/bookings/{id}/confirm` - Confirm booking after payment

### Frontend Components
- `BookingWizardView.vue` - Main wizard container
- `ServiceSelector.vue` - Step 1: Service selection
- `PersianCalendar.vue` - Persian date picker
- `TimeSlotPicker.vue` - Step 2: Time selection
- `CustomerDetailsForm.vue` - Step 3: Customer info
- `BookingSummary.vue` - Step 4: Review
- `PaymentView.vue` - Step 5: Payment
- `BookingConfirmation.vue` - Success page
- `MyBookingsView.vue` - Bookings list
- `BookingDetailView.vue` - Booking details

### State Management
- `bookingStore.ts` - Manages wizard state and booking data
- Persists wizard state in sessionStorage
- Clears state after successful booking

### Payment Integration
- ZarinPal merchant ID in environment variables
- Payment callback URL: `/customer/payment/callback`
- Payment verification on backend
- Handle success, failure, and timeout scenarios

### Notification Templates
- Email: Booking confirmation, cancellation, reschedule, reminder
- SMS: Booking confirmation, 24h reminder, 1h reminder

### Database
- Uses existing `ServiceCatalog.Bookings` table
- Optimistic locking for concurrent booking prevention
- Indexes on `CustomerId`, `StartTime`, `Status`

### Background Jobs
- Send 24-hour reminder: Run daily at 9:00 AM
- Send 1-hour reminder: Run every hour
- Cancel unpaid bookings: Run every 15 minutes
- Clean up expired slots: Run hourly
