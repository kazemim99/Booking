# provider-management Specification Delta

## ADDED Requirements

### Requirement: Location Map Synchronization
The system SHALL provide two-way synchronization between the interactive map and location selector dropdowns when providers set their business location.

#### Scenario: Map click updates location selectors
- **WHEN** a provider clicks a location on the map
- **THEN** the system performs reverse geocoding to detect the province and city
- **AND** automatically selects the detected province in the province dropdown
- **AND** loads cities for the detected province
- **AND** automatically selects the detected city in the city dropdown
- **AND** normalizes province names by removing "استان" prefix if present
- **AND** logs detection results for debugging

#### Scenario: Province selection centers map
- **WHEN** a provider selects a province from the dropdown
- **THEN** the system geocodes the province name to coordinates
- **AND** centers the map on the province location
- **AND** updates the map marker to the new coordinates
- **AND** resets the city selection

#### Scenario: City selection centers map
- **WHEN** a provider selects a city from the dropdown
- **THEN** the system geocodes "city, province" for better accuracy
- **AND** centers the map on the city location
- **AND** updates the map marker to the new coordinates
- **AND** zooms the map to an appropriate level

#### Scenario: Geocoding failure handling
- **WHEN** geocoding fails for a location name
- **THEN** the system logs an error to the console
- **AND** does not update the map position
- **AND** maintains existing coordinates
- **AND** does not prevent the user from continuing with location setup
