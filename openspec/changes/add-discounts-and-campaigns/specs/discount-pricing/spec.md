# discount-pricing

## ADDED Requirements

### Requirement: Server-authoritative discounted price
The server SHALL compute a booking's price, including any discount, from the services' list prices and
the promotions in force; a client SHALL NOT send a price or a discount amount. The same evaluation SHALL
back both the price quote and booking creation.

#### Scenario: Quote before confirming
- **WHEN** a signed-in customer asks for a quote for services at a salon, a start time and an optional code
- **THEN** the response carries the subtotal, the discount, the total, the applied promotion (title, code,
  owner) and, when a code was entered, the code's outcome with a Persian message

#### Scenario: Booking re-evaluates on the server
- **WHEN** a customer creates a booking
- **THEN** the server evaluates promotions at that instant and the booking's total is the subtotal minus
  the applied discount, regardless of any quote obtained earlier

### Requirement: At most one discount per booking, the best one
The system SHALL apply at most one promotion to a booking: the eligible promotion with the largest
discount. An entered code SHALL replace the best automatic promotion only when its discount is strictly
larger.

#### Scenario: Two automatic promotions
- **WHEN** a salon has a 10% and a 20% automatic promotion both eligible for a visit
- **THEN** only the 20% promotion is applied

#### Scenario: Code worse than the automatic offer
- **WHEN** a customer enters a valid code worth less than the automatic offer already applicable
- **THEN** the automatic offer is applied and the code's outcome says a better offer was applied

#### Scenario: Unknown or ineligible code
- **WHEN** a customer enters a code that does not exist at this salon, or whose conditions are not met
- **THEN** the quote reports the code as not found or not eligible with the reason, and a booking
  submitted with that code is refused with the same reason

### Requirement: Promotion conditions
A promotion SHALL apply only when every condition it declares holds: its validity window contains the
booking moment; its days of the week and daily time window contain the appointment's salon-local start;
the eligible subtotal meets its minimum; the visit contains at least one of its targeted services (all
services when none are targeted); a new-customers-only promotion applies only to a customer with no
earlier non-cancelled booking at that salon; its total and per-customer usage limits are not reached; it
is not paused or ended; and, for a platform campaign, the salon has joined it.

#### Scenario: Off-peak window
- **WHEN** a promotion is limited to Saturday–Wednesday 10:00–14:00 and the appointment starts Thursday 11:00
- **THEN** the promotion does not apply

#### Scenario: Targeted services
- **WHEN** a promotion targets only "haircut" and the visit is haircut plus colour
- **THEN** the discount is computed on the haircut's price only

#### Scenario: Usage limit reached
- **WHEN** a promotion limited to 50 uses has 50 applied redemptions
- **THEN** it no longer applies

### Requirement: Discount amount rules
The discount SHALL be the percentage of the eligible subtotal (capped by the promotion's maximum when set)
or the fixed amount, SHALL NOT exceed 90% of the eligible subtotal, and SHALL be rounded down to a whole
Toman. A booking's total SHALL therefore always be positive.

#### Scenario: Fixed amount larger than the price
- **WHEN** a 500,000 Toman fixed discount applies to a 300,000 Toman service
- **THEN** the discount is 270,000 Toman and the total is 30,000 Toman

#### Scenario: Percentage with a cap
- **WHEN** a 30% promotion capped at 100,000 Toman applies to a 1,000,000 Toman visit
- **THEN** the discount is 100,000 Toman

### Requirement: The booking snapshots its discount
A booking SHALL record the discount it received — promotion id, title, code, owner and amount — and
SHALL keep it unchanged when the promotion is later edited, paused or ended. A rescheduled booking SHALL
carry the same discount to its successor. Deposit and cancellation fee SHALL be computed on the
discounted total.

#### Scenario: Promotion edited after booking
- **WHEN** a salon changes a promotion from 20% to 10% after a customer booked with it
- **THEN** that booking still shows and charges the 20% discount

#### Scenario: Reschedule keeps the discount
- **WHEN** a discounted booking is rescheduled
- **THEN** the new booking has the same total and discount snapshot

### Requirement: Redemptions are counted exactly
Each applied discount SHALL record one redemption linked to the booking. Usage limits SHALL hold under
concurrent bookings. Cancelling a booking, by the customer or the salon, SHALL release its redemption; a
no-show or a completed booking SHALL keep it.

#### Scenario: Racing for the last use
- **WHEN** two customers concurrently book with a promotion that has one use left
- **THEN** at most one booking receives the discount; the other request fails with a conflict and, when
  retried, is priced without it

#### Scenario: Cancellation returns the use
- **WHEN** a customer cancels a booking made with a single-use code
- **THEN** the redemption is released and the customer may use the code again

### Requirement: Salon-entered bookings are not discounted
A booking a salon enters for its own client SHALL NOT receive any promotion.

#### Scenario: Walk-in during an automatic offer
- **WHEN** the salon enters a booking for a walk-in while a 20% automatic offer is active
- **THEN** the booking is priced at the list price and no redemption is recorded

### Requirement: Customers see offers before booking
The public salon page SHALL list the salon's currently applicable automatic offers (title, benefit,
conditions, targeted services, end date) without authentication, and SHALL NOT list codes. Customer
clients SHALL show a discount badge and the discounted price on eligible services, a code field and a
subtotal/discount/total breakdown before confirming, and the discount on the booking's details.

#### Scenario: Salon page shows an offer
- **WHEN** a visitor opens a salon that has an active 20% automatic offer on haircuts
- **THEN** the haircut shows a 20% badge with its list price struck through and the discounted price

#### Scenario: Codes stay private
- **WHEN** a salon has an active code-based promotion
- **THEN** the public offers list does not reveal it
