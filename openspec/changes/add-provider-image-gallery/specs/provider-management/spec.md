# provider-management Specification Delta

## MODIFIED Requirements

### Requirement: Feature-specific visibility
The system SHALL allow providers to configure feature visibility including gallery images.

#### Scenario: Feature-specific visibility
- **WHEN** a provider configures feature visibility
- **THEN** the system allows hiding specific features from public view (e.g., pricing, staff names, gallery)
- **AND** displays how each setting affects customer experience
- **AND** maintains booking functionality even when some features are hidden

#### Scenario: Gallery visibility control
- **WHEN** a provider accesses gallery visibility settings
- **THEN** the system allows toggling gallery visibility (show/hide on public profile)
- **AND** previews the customer-facing profile with current visibility settings
- **AND** warns that hiding the gallery may reduce customer engagement
