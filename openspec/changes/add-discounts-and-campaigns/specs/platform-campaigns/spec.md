# platform-campaigns

## ADDED Requirements

### Requirement: Admins run platform campaigns
An administrator SHALL be able to create, edit, pause, resume and end platform campaigns in the admin
panel, with the same benefit and condition model as salon promotions except service targeting. A
platform campaign SHALL be funded by the salons that join it and SHALL apply only at joined salons.

#### Scenario: Publish a seasonal campaign
- **WHEN** an admin creates "یلدا", automatic, 15% capped at 200,000 Toman, from 1 to 7 Dey
- **THEN** salons see the campaign and can join it, and it applies at joined salons during those dates

#### Scenario: Non-admin refused
- **WHEN** a provider or customer calls the admin promotion endpoints
- **THEN** the request is forbidden

### Requirement: Admins oversee every promotion
An administrator SHALL be able to list all promotions (platform and salon), filter them by owner, salon,
state and text, see each one's uses and total discount, see which salons joined a campaign, and pause or
end any salon's promotion.

#### Scenario: Pause an abusive salon promotion
- **WHEN** an admin pauses a salon's promotion
- **THEN** it stops applying to new bookings and the salon sees it as paused

#### Scenario: Campaign participation
- **WHEN** an admin opens a campaign
- **THEN** the panel shows the joined salons, the number of uses and the total discount given
