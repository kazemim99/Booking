# provider-promotions

## ADDED Requirements

### Requirement: Salons manage their own promotions
A salon's owner or a member allowed to manage the organization SHALL be able to create, edit, pause,
resume and end promotions on the salon's own services, from the provider app. A promotion SHALL declare
a title, whether it is automatic or code-based (with its code), a percentage (1–90, optional cap) or a
fixed amount, and any of: start and end, days of the week, a daily time window, a minimum subtotal,
targeted services, new customers only, a total usage limit and a per-customer limit.

#### Scenario: Create an off-peak offer
- **WHEN** the owner creates "۲۰٪ صبح‌های وسط هفته", automatic, 20%, Saturday–Wednesday 10:00–13:00
- **THEN** the promotion is active and customers booking in that window get 20% off

#### Scenario: Invalid promotion refused
- **WHEN** the owner submits a 95% promotion, an end before its start, or a targeted service of another salon
- **THEN** the request is refused with a Persian message naming the problem

#### Scenario: Duplicate code
- **WHEN** the owner creates a code that another of the salon's promotions already uses
- **THEN** the request is refused as a conflict

#### Scenario: Code locked once used
- **WHEN** the owner changes the code of a promotion that has redemptions
- **THEN** the change is refused

#### Scenario: Someone else's salon
- **WHEN** a user who is neither admin, owner nor a managing member calls the salon's promotion endpoints
- **THEN** the request is forbidden

### Requirement: Salons see how their promotions perform
The salon's promotion list SHALL show each promotion's derived state (scheduled, active, paused, expired,
exhausted, ended), its uses against its limit and the total discount given.

#### Scenario: Exhausted promotion
- **WHEN** a promotion limited to 10 uses has 10 applied redemptions
- **THEN** the list shows it as exhausted with 10/10 uses

### Requirement: Salons join and leave platform campaigns
A salon SHALL see the platform campaigns that are active or scheduled, with their terms and whether it has
joined, and SHALL be able to join or leave each one. A campaign SHALL apply only to bookings at salons that
have joined it at the moment of booking; leaving SHALL NOT change bookings already made.

#### Scenario: Join a campaign
- **WHEN** the owner joins the "نوروز ۱۴۰۵" campaign
- **THEN** customers booking at that salon receive the campaign discount while it is in force

#### Scenario: Leave a campaign
- **WHEN** the owner leaves a campaign
- **THEN** new bookings at the salon no longer receive it and existing discounted bookings are unchanged
