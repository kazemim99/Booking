# Add Location Map Synchronization

## Why
Providers need seamless integration between the map interface and location selector dropdowns when setting their business location. Currently, selecting a location on the map doesn't automatically update the province/city dropdowns, and selecting province/city doesn't center the map, creating a disjointed user experience.

## What Changes
- **Two-way synchronization**: Map clicks now auto-detect and select province/city in dropdowns
- **Reverse synchronization**: Province/city selection now centers the map to that location
- **Name normalization**: Handle Neshan API province name format (with "استان" prefix) and match to database records
- **Geocoding integration**: Use Neshan Search API to convert location names to coordinates for map centering

## Impact
- Affected specs: provider-management
- Affected code:
  - Frontend: `booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue`
  - Composable: Uses existing `useLocations` composable for province/city data
  - Map component: Uses existing `NeshanMapPicker` component with its reverse geocoding API
