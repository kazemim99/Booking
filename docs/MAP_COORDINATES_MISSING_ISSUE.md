# Map Coordinates Missing Issue

## Problem Summary

When loading the Neshant map in the Location tab, the map doesn't show the actual point/marker because the API doesn't return `latitude` and `longitude` in the address object.

### Example API Response
```json
GET /Providers/2a80a718-0cb1-4db9-b244-626ee930fe99
{
  "address": {
    "formattedAddress": "پارس آباد، بلوار جمهوری اسلامی، جهاد، نشاط 3",
    "city": "پارس آباد",
    "state": "اردبیل",
    "postalCode": "-",
    "country": "IR"
    // ❌ NO latitude field!
    // ❌ NO longitude field!
  }
}
```

---

## Root Cause Analysis

### Backend is Correct ✅

The backend code handles coordinates properly:

1. **Database Schema** - Columns exist:
   ```sql
   AddressLatitude  DECIMAL(10, 8)  -- Can store: 35.12345678
   AddressLongitude DECIMAL(11, 8)  -- Can store: 51.123456789
   ```

2. **EF Core Configuration** - Mapped correctly:
   ```csharp
   // ProviderConfiguration.cs:278-284
   address.Property(a => a.Latitude)
       .HasPrecision(10, 8)
       .HasColumnName("AddressLatitude");

   address.Property(a => a.Longitude)
       .HasPrecision(11, 8)
       .HasColumnName("AddressLongitude");
   ```

3. **DTO Includes Coordinates** - Response model has fields:
   ```csharp
   // AddressResponse.cs:12-13
   public double? Latitude { get; set; }
   public double? Longitude { get; set; }
   ```

4. **Controller Maps Coordinates** - Mapping is correct:
   ```csharp
   // ProvidersController.cs:1150-1151
   Latitude = result.Address.Latitude,
   Longitude = result.Address.Longitude
   ```

5. **Update Command Receives Coordinates** - Command has parameters:
   ```csharp
   // UpdateLocationCommand.cs:14-15
   double Latitude,
   double Longitude,
   ```

6. **Handler Saves Coordinates** - Creates BusinessAddress with lat/long:
   ```csharp
   // UpdateLocationCommandHandler.cs:55-56
   request.Latitude,
   request.Longitude
   ```

**Conclusion:** The backend infrastructure is complete and correct!

---

## The Actual Problem

### Database Has NULL Values ❌

The issue is that the database columns `AddressLatitude` and `AddressLongitude` are **NULL** for this provider!

```sql
SELECT
    Id,
    BusinessName,
    AddressFormattedAddress,
    AddressLatitude,    -- NULL!
    AddressLongitude    -- NULL!
FROM ServiceCatalog.Providers
WHERE Id = '2a80a718-0cb1-4db9-b244-626ee930fe99';

-- Result:
-- AddressLatitude:  NULL
-- AddressLongitude: NULL
```

### Why Are They NULL?

There are several possible reasons:

#### 1. **Frontend Not Sending Coordinates** ⚠️

When the user clicks on the Neshant map to select a location, the frontend might be:
- Only sending the address text (formattedAddress, city, etc.)
- **NOT sending** the `latitude` and `longitude` from the map click event

**Check Frontend:**
```typescript
// ProfileManager.vue - When submitting location update
const handleLocationSelected = async (location) => {
  const payload = {
    formattedAddress: location.address,
    city: location.city,
    provinceId: location.provinceId,
    cityId: location.cityId,
    country: 'IR',
    // ❌ Check if these are included:
    latitude: location.lat,   // Make sure this exists!
    longitude: location.lng   // Make sure this exists!
  };

  await updateProviderLocation(payload);
};
```

#### 2. **Neshant Map Component Not Returning Coordinates**

The Neshant map component might not be emitting lat/long when the user clicks:

```typescript
// NeshantMap.vue or similar
const handleMapClick = (event) => {
  emit('location-selected', {
    address: formattedAddress,
    city: cityName,
    provinceId: provinceId,
    cityId: cityId,
    // ❌ Make sure these are emitted:
    lat: event.latlng.lat,
    lng: event.latlng.lng
  });
};
```

#### 3. **Initial Provider Creation Without Coordinates**

When the provider was initially created/registered, the coordinates might not have been provided:

```csharp
// Provider registration
var address = BusinessAddress.Create(
    formattedAddress: "پارس آباد، بلوار جمهوری اسلامی...",
    street: "بلوار جمهوری اسلامی",
    city: "پارس آباد",
    state: "اردبیل",
    postalCode: "-",
    country: "IR",
    provinceId: 27,
    cityId: 123,
    latitude: null,   // ❌ NULL on creation
    longitude: null   // ❌ NULL on creation
);
```

---

## How to Fix This

### Option 1: Frontend Must Send Coordinates (Recommended)

**Update the frontend code** to ensure lat/long are sent when updating location:

```typescript
// booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue

const handleLocationSubmit = async () => {
  if (!selectedLocation.value) {
    toast.error('Please select a location on the map');
    return;
  }

  const payload = {
    formattedAddress: selectedLocation.value.formattedAddress,
    addressLine1: selectedLocation.value.addressLine1,
    city: selectedLocation.value.city,
    postalCode: selectedLocation.value.postalCode || '-',
    country: 'IR',
    provinceId: selectedLocation.value.provinceId,
    cityId: selectedLocation.value.cityId,
    // ✅ MUST include coordinates from map
    latitude: selectedLocation.value.latitude,
    longitude: selectedLocation.value.longitude
  };

  try {
    await providerService.updateLocation(provider.value.id, payload);
    toast.success('Location updated successfully');
  } catch (error) {
    toast.error('Failed to update location');
  }
};
```

### Option 2: Geocode Address to Get Coordinates

If the map doesn't provide coordinates, use a geocoding service:

```typescript
// Use Neshant Map API or Google Maps Geocoding
const geocodeAddress = async (address: string) => {
  const response = await fetch(`https://map.ir/search/v2/autocomplete?text=${address}`);
  const data = await response.json();

  return {
    latitude: data.geom.coordinates[1],  // Lat
    longitude: data.geom.coordinates[0]  // Lng
  };
};

// Before submitting
const coords = await geocodeAddress(formattedAddress);
payload.latitude = coords.latitude;
payload.longitude = coords.longitude;
```

### Option 3: Use Default Coordinates for City

If exact coordinates aren't available, use the city center:

```typescript
// Fallback to city center coordinates
const cityCoordinates = {
  'پارس آباد': { lat: 39.6483, lng: 47.9167 },
  'تهران': { lat: 35.6892, lng: 51.3890 },
  // ... other cities
};

payload.latitude = cityCoordinates[selectedCity].lat;
payload.longitude = cityCoordinates[selectedCity].lng;
```

---

## Testing the Fix

### 1. Verify Frontend Sends Coordinates

Open browser DevTools → Network tab:

```http
PUT /api/v1/providers/{id}/location
Content-Type: application/json

{
  "formattedAddress": "پارس آباد، بلوار جمهوری اسلامی، جهاد، نشاط 3",
  "city": "پارس آباد",
  "provinceId": 27,
  "cityId": 123,
  "country": "IR",
  "latitude": 39.6483,    // ✅ Check this is sent
  "longitude": 47.9167    // ✅ Check this is sent
}
```

### 2. Verify Backend Saves Coordinates

Check database after update:

```sql
SELECT
    BusinessName,
    AddressFormattedAddress,
    AddressLatitude,
    AddressLongitude
FROM ServiceCatalog.Providers
WHERE Id = '2a80a718-0cb1-4db9-b244-626ee930fe99';

-- Expected:
-- AddressLatitude:  39.6483  ✅
-- AddressLongitude: 47.9167  ✅
```

### 3. Verify API Returns Coordinates

```http
GET /api/v1/providers/2a80a718-0cb1-4db9-b244-626ee930fe99
```

```json
{
  "address": {
    "formattedAddress": "پارس آباد، بلوار جمهوری اسلامی، جهاد، نشاط 3",
    "city": "پارس آباد",
    "state": "اردبیل",
    "postalCode": "-",
    "country": "IR",
    "latitude": 39.6483,    // ✅ Now included!
    "longitude": 47.9167    // ✅ Now included!
  }
}
```

### 4. Verify Map Shows Marker

The Neshant map component should now show a marker at the correct location:

```typescript
// Map initialization
if (provider.address.latitude && provider.address.longitude) {
  map.setView([provider.address.latitude, provider.address.longitude], 15);
  L.marker([provider.address.latitude, provider.address.longitude])
    .addTo(map)
    .bindPopup(provider.address.formattedAddress);
}
```

---

## Quick Fix for Existing Providers

For providers that already have NULL coordinates, run this migration:

```sql
-- Update existing providers with city center coordinates
UPDATE ServiceCatalog.Providers
SET
    AddressLatitude = 39.6483,  -- Pars Abad city center
    AddressLongitude = 47.9167
WHERE
    AddressCity = 'پارس آباد'
    AND (AddressLatitude IS NULL OR AddressLongitude IS NULL);

-- Repeat for other cities...
```

Or add a background job to geocode existing addresses.

---

## Related Files

### Frontend (Need to Check/Fix)
- `booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue`
- `booksy-frontend/src/modules/provider/services/provider.service.ts`
- Neshant Map component (location selection)

### Backend (Already Correct)
- ✅ `UpdateLocationCommand.cs` - Has Latitude/Longitude parameters
- ✅ `UpdateLocationCommandHandler.cs` - Saves coordinates
- ✅ `AddressResponse.cs` - Includes Latitude/Longitude
- ✅ `ProviderConfiguration.cs` - EF Core mapping correct

---

## Summary

❌ **Problem:** Map doesn't show markers because `latitude` and `longitude` are NULL in database
✅ **Backend:** Already supports coordinates (schema, DTOs, handlers all correct)
⚠️ **Frontend:** Likely not sending coordinates when user selects location on map
🔧 **Fix:** Update frontend to send lat/long from Neshant map click event

The issue is a **frontend → backend data flow problem**, not a backend infrastructure issue!
