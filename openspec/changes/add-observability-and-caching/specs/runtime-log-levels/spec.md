## ADDED Requirements

### Requirement: Admins change log levels at runtime
An administrator (AdminOnly) SHALL be able to list effective log levels and set or reset the level of the default or
any logging category without restarting the host; the change SHALL take effect for existing loggers immediately and
apply to the category and its sub-categories.

#### Scenario: Debug one namespace
- **WHEN** an admin sets `AsanRezerve.ServiceCatalog` to Debug
- **THEN** Debug events from `AsanRezerve.ServiceCatalog.*` are written and other categories keep their levels

#### Scenario: Reset returns to configuration
- **WHEN** an admin resets an overridden category
- **THEN** its effective level is the configured one again

#### Scenario: Non-admins cannot read or change levels
- **WHEN** an authenticated non-admin calls the log-levels API
- **THEN** the response is 403 and nothing changes

#### Scenario: Invalid input is rejected
- **WHEN** the category contains characters outside `[A-Za-z0-9_.]` or the level is unknown
- **THEN** the response is 400

### Requirement: Temporary overrides expire by themselves
An override MAY carry a duration; when it elapses the category SHALL revert to its configured level without admin
action.

#### Scenario: Thirty minutes of Debug
- **WHEN** an admin sets Debug for 30 minutes
- **THEN** after 30 minutes the override is removed and the configured level applies

### Requirement: Overrides persist and are audited
Active overrides SHALL survive a restart, and every change SHALL write an audit event naming the admin, category,
previous level, new level and expiry.

#### Scenario: Restart keeps an override
- **WHEN** the host restarts while a non-expired override exists
- **THEN** the override is applied at startup

#### Scenario: Change is audited
- **WHEN** an admin changes a level
- **THEN** a Warning `LogLevelChanged` event records who changed what from which level to which level
