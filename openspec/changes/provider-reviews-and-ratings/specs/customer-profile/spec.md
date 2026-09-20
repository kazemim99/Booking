# customer-profile

## MODIFIED Requirements

### Requirement: Review Management
Customers MUST be able to view all their submitted reviews and edit recent reviews (within 7 days).

The list MUST show every review the customer has written regardless of moderation state, and MUST show each review's state — published, awaiting approval, rejected or hidden — together with the administrator's reason where one was recorded. A review that is awaiting approval MUST NOT be presented as though it were live.

Editing MUST be offered only for a review that is within the 7-day window **and** is in the published or awaiting-approval state. A rejected or hidden review MUST NOT offer editing.

The edit form MUST carry the overall star rating, the four optional dimension ratings (cleanliness and hygiene, skill and quality of work, punctuality, conduct and manner) behind a disclosure, and a comment of 10 to 2000 characters. Saving MUST call the ServiceCatalog reviews endpoint for the review, and the customer MUST be told that an edited review returns for approval before it is public again.

#### Scenario: Customer views their reviews
**GIVEN** the customer has submitted 5 reviews
**WHEN** the customer clicks "نظرات من" in user menu
**THEN** the system displays ReviewsModal with list of 5 reviews
**AND** each review shows:
- Provider name and logo
- Service name
- Star rating (1-5, in half-star steps)
- Any dimension ratings the customer gave
- Review text
- Submission date (Jalali format)
- Its moderation state, and the reason if it was rejected or hidden
- "ویرایش" button (if review is <7 days old and is published or awaiting approval)

#### Scenario: Customer edits recent review
**GIVEN** the customer submitted a review 3 days ago and it is published
**WHEN** the customer clicks "ویرایش" on the review
**THEN** the system displays edit form with:
- Star rating selector (pre-filled with current overall rating)
- The four dimension ratings behind a disclosure, pre-filled where given
- Text area (pre-filled with current review text)
- Character counter (10–2000 chars)
- "ذخیره" and "انصراف" buttons
**AND** upon clicking "ذخیره", validates input
**AND** sends the edit to the ServiceCatalog reviews endpoint for that review
**AND** updates review display with "(ویرایش شده)" label
**AND** shows the review as awaiting approval rather than public
**AND** displays a toast confirming the edit was saved and is awaiting approval

#### Scenario: Customer tries to edit old review
**GIVEN** the customer submitted a review 10 days ago
**WHEN** the customer views the review in reviews modal
**THEN** the "ویرایش" button is not displayed
**AND** a message shows: "فقط نظرات کمتر از ۷ روز قابل ویرایش هستند"

#### Scenario: Customer tries to edit a rejected review
**GIVEN** the customer submitted a review 3 days ago that an administrator rejected
**WHEN** the customer views the review in reviews modal
**THEN** the "ویرایش" button is not displayed
**AND** the review is shown as rejected, with the recorded reason

#### Scenario: Customer sees a review awaiting approval
**GIVEN** the customer submitted a review that has not yet been approved
**WHEN** the customer opens reviews modal
**THEN** the review is listed and marked as awaiting approval
**AND** it is not presented as visible to other customers

#### Scenario: Customer has no reviews
**GIVEN** the customer has not submitted any reviews
**WHEN** the customer opens reviews modal
**THEN** the system displays empty state:
- Star icon illustration
- Message: "شما هنوز نظری ثبت نکرده‌اید"
- "جستجوی خدمات" button
