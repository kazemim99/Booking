# Implementation Tasks

## 1. Map to Selector Synchronization
- [x] 1.1 Import useLocations composable in ProfileManager
- [x] 1.2 Update handleLocationSelected to be async
- [x] 1.3 Extract province name from Neshan reverse geocoding response
- [x] 1.4 Implement province name normalization (strip "استان" prefix)
- [x] 1.5 Load provinces and match by name
- [x] 1.6 Load cities for matched province
- [x] 1.7 Match city name and update locationForm.cityId
- [x] 1.8 Add console logging for debugging

## 2. Selector to Map Synchronization
- [x] 2.1 Create centerMapToLocation helper function
- [x] 2.2 Implement Neshan Search API integration for geocoding
- [x] 2.3 Update handleProvinceChange to center map on province selection
- [x] 2.4 Update handleCityChange to center map on city selection
- [x] 2.5 Update locationForm.coordinates to trigger map re-centering
- [x] 2.6 Add error handling for failed geocoding requests

## 3. Testing
- [x] 3.1 Test clicking various locations on map updates dropdowns
- [x] 3.2 Test province selection centers map
- [x] 3.3 Test city selection centers map with better accuracy
- [x] 3.4 Verify province name normalization works for all provinces
