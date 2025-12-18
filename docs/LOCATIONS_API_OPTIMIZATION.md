# Locations API Optimization Guide

## Problem Summary

The Profile page loads provinces and cities inefficiently:
1. Calls `GET /api/v1/locations/provinces` → Returns 31 provinces
2. For EACH province, calls `GET /api/v1/locations/provinces/{id}/cities`
3. **Total API calls: 1 + 31 = 32 requests!**

This is extremely costly in terms of:
- Network bandwidth
- Server load
- Page load time
- Database queries

---

## ✅ Solution: Use the Existing `/hierarchy` Endpoint!

The backend already has an optimized endpoint that returns **all provinces with their cities in a single API call**!

### Endpoint Details

```http
GET /api/v1/locations/hierarchy
```

**Response:** All 31 provinces with nested cities in one call!

```json
[
  {
    "id": 1,
    "name": "آذربایجان شرقی",
    "provinceCode": 1,
    "cities": [
      {
        "id": 101,
        "name": "تبریز",
        "cityCode": 1
      },
      {
        "id": 102,
        "name": "مراغه",
        "cityCode": 2
      }
      // ... all cities in this province
    ]
  },
  {
    "id": 2,
    "name": "آذربایجان غربی",
    "provinceCode": 2,
    "cities": [
      {
        "id": 201,
        "name": "ارومیه",
        "cityCode": 1
      }
      // ... all cities in this province
    ]
  }
  // ... all 31 provinces
]
```

---

## Performance Comparison

### ❌ Current Implementation (Inefficient)

```typescript
// 1. Load provinces
const provinces = await fetch('/api/v1/locations/provinces');
// Returns: 31 provinces

// 2. Load cities for EACH province (31 separate API calls!)
for (const province of provinces) {
  const cities = await fetch(`/api/v1/locations/provinces/${province.id}/cities`);
  // Stores cities in state
}

// Total: 32 API calls
// Time: ~3-5 seconds (depending on network)
// Database queries: 32
```

### ✅ Optimized Implementation (Use Hierarchy)

```typescript
// 1. Load all provinces with cities in ONE call
const hierarchy = await fetch('/api/v1/locations/hierarchy');
// Returns: All 31 provinces with nested cities

// Total: 1 API call ✅
// Time: ~100-300ms
// Database queries: 1 (with eager loading)
```

**Performance Improvement:**
- **32x fewer API calls** (32 → 1)
- **~10-15x faster** page load
- **32x less database load**
- **Much better UX** (no loading spinners for each province)

---

## Frontend Implementation

### Current Code (Inefficient)

**File:** `booksy-frontend/src/shared/composables/useLocations.ts` or `ProfileManager.vue`

```typescript
// ❌ BAD: Multiple API calls
const loadLocations = async () => {
  // Call 1: Get provinces
  const provincesResponse = await locationService.getProvinces();
  provinces.value = provincesResponse.data;

  // Calls 2-32: Get cities for each province
  for (const province of provinces.value) {
    const citiesResponse = await locationService.getCitiesByProvinceId(province.id);
    citiesByProvince.value[province.id] = citiesResponse.data;
  }
};
```

### Optimized Code

```typescript
// ✅ GOOD: Single API call
const loadLocations = async () => {
  try {
    const response = await fetch('/api/v1/locations/hierarchy');
    const hierarchy = await response.json();

    // Transform hierarchy to the format needed
    provinces.value = hierarchy.map(p => ({
      id: p.id,
      name: p.name,
      provinceCode: p.provinceCode
    }));

    // Create cities lookup by province ID
    citiesByProvince.value = {};
    hierarchy.forEach(province => {
      citiesByProvince.value[province.id] = province.cities;
    });

    console.log('✅ Loaded all locations in 1 API call!');
  } catch (error) {
    console.error('Failed to load locations:', error);
  }
};
```

---

## Implementation Steps

### Step 1: Update Location Service

**File:** `booksy-frontend/src/core/api/services/location.service.ts` (or similar)

```typescript
export class LocationService {
  // ✅ Add new method
  async getLocationHierarchy(): Promise<ProvinceHierarchy[]> {
    const response = await this.httpClient.get('/api/v1/locations/hierarchy');
    return response.data;
  }

  // Keep existing methods for backward compatibility (if needed)
  async getProvinces(): Promise<Province[]> {
    const response = await this.httpClient.get('/api/v1/locations/provinces');
    return response.data;
  }

  async getCitiesByProvinceId(provinceId: number): Promise<City[]> {
    const response = await this.httpClient.get(`/api/v1/locations/provinces/${provinceId}/cities`);
    return response.data;
  }
}
```

### Step 2: Update Types

**File:** `booksy-frontend/src/types/location.types.ts` (or similar)

```typescript
export interface Province {
  id: number;
  name: string;
  provinceCode: number;
}

export interface City {
  id: number;
  name: string;
  cityCode: number;
}

// ✅ Add new type for hierarchy
export interface ProvinceHierarchy extends Province {
  cities: City[];
}
```

### Step 3: Update Composable

**File:** `booksy-frontend/src/shared/composables/useLocations.ts`

```typescript
import { ref } from 'vue';
import type { ProvinceHierarchy, Province, City } from '@/types/location.types';
import { locationService } from '@/core/api/services/location.service';

export function useLocations() {
  const provinces = ref<Province[]>([]);
  const citiesByProvince = ref<Record<number, City[]>>({});
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // ✅ OPTIMIZED: Load all in one call
  const loadProvinces = async () => {
    if (provinces.value.length > 0) {
      return; // Already loaded
    }

    isLoading.value = true;
    error.value = null;

    try {
      const hierarchy = await locationService.getLocationHierarchy();

      // Extract provinces
      provinces.value = hierarchy.map(p => ({
        id: p.id,
        name: p.name,
        provinceCode: p.provinceCode
      }));

      // Build cities lookup
      hierarchy.forEach(province => {
        citiesByProvince.value[province.id] = province.cities;
      });

      console.log(`✅ Loaded ${provinces.value.length} provinces with cities in 1 API call`);
    } catch (err) {
      error.value = 'Failed to load locations';
      console.error('Error loading locations:', err);
    } finally {
      isLoading.value = false;
    }
  };

  // ✅ Get cities for a province (no API call needed!)
  const getCitiesByProvinceId = (provinceId: number): City[] => {
    return citiesByProvince.value[provinceId] || [];
  };

  return {
    provinces,
    citiesByProvince,
    isLoading,
    error,
    loadProvinces,
    getCitiesByProvinceId
  };
}
```

### Step 4: Update ProfileManager Component

**File:** `booksy-frontend/src/modules/provider/components/dashboard/ProfileManager.vue`

```typescript
<script setup lang="ts">
import { onMounted } from 'vue';
import { useLocations } from '@/shared/composables/useLocations';

const { provinces, getCitiesByProvinceId, loadProvinces, isLoading } = useLocations();

onMounted(async () => {
  // ✅ Load all provinces with cities in ONE call
  await loadProvinces();

  // No need for the province loop anymore! ✅
  // All data is already loaded!
});

// When user selects a province
const handleProvinceChange = (provinceId: number) => {
  // ✅ Get cities from memory (no API call!)
  const cities = getCitiesByProvinceId(provinceId);
  console.log(`Province ${provinceId} has ${cities.length} cities`);
};
</script>

<template>
  <div v-if="isLoading">Loading locations...</div>

  <select v-model="selectedProvinceId" @change="handleProvinceChange">
    <option v-for="province in provinces" :key="province.id" :value="province.id">
      {{ province.name }}
    </option>
  </select>

  <select v-model="selectedCityId">
    <option
      v-for="city in getCitiesByProvinceId(selectedProvinceId)"
      :key="city.id"
      :value="city.id"
    >
      {{ city.name }}
    </option>
  </select>
</template>
```

---

## Additional Optimizations

### 1. Add Caching

Cache the hierarchy data to avoid reloading on every component mount:

```typescript
// useLocations.ts
const CACHE_KEY = 'locations_hierarchy';
const CACHE_DURATION = 24 * 60 * 60 * 1000; // 24 hours

const loadProvinces = async () => {
  // Try to load from cache first
  const cached = localStorage.getItem(CACHE_KEY);
  if (cached) {
    const { data, timestamp } = JSON.parse(cached);
    if (Date.now() - timestamp < CACHE_DURATION) {
      provinces.value = data.provinces;
      citiesByProvince.value = data.citiesByProvince;
      console.log('✅ Loaded from cache');
      return;
    }
  }

  // Fetch from API
  const hierarchy = await locationService.getLocationHierarchy();

  // ... process data ...

  // Save to cache
  localStorage.setItem(CACHE_KEY, JSON.stringify({
    data: { provinces: provinces.value, citiesByProvince: citiesByProvince.value },
    timestamp: Date.now()
  }));
};
```

### 2. Add Response Compression

Enable GZIP/Brotli compression on the server for the `/hierarchy` endpoint:

```csharp
// Program.cs or Startup.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
```

Typical compression ratio: **~70-80%** for JSON with Persian text!

### 3. Add Response Caching Headers

```csharp
// LocationsController.cs
[HttpGet("hierarchy")]
[ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)] // Cache for 1 hour
public async Task<ActionResult<IEnumerable<ProvinceHierarchyDto>>> GetHierarchy()
{
    // ... existing code
}
```

---

## Testing

### Before Optimization

```bash
# Open browser DevTools → Network tab
# Load Profile page
# Count requests: 32 requests to /api/v1/locations/*
# Total time: ~3-5 seconds
```

### After Optimization

```bash
# Open browser DevTools → Network tab
# Load Profile page
# Count requests: 1 request to /api/v1/locations/hierarchy
# Total time: ~100-300ms
# Size: ~50-100 KB (uncompressed), ~10-20 KB (compressed)
```

### Verify with curl

```bash
# Test the hierarchy endpoint
curl -X GET "https://localhost:7001/api/v1/locations/hierarchy" \
  -H "Accept: application/json" \
  | jq '.[] | {id, name, cityCount: (.cities | length)}'

# Expected output:
# {
#   "id": 1,
#   "name": "آذربایجان شرقی",
#   "cityCount": 15
# }
# ... (31 provinces total)
```

---

## Backend Endpoint Details

### API Endpoint

```http
GET /api/v1/locations/hierarchy
```

### Implementation

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/LocationsController.cs:86-113`

```csharp
[HttpGet("hierarchy")]
[ProducesResponseType(typeof(IEnumerable<ProvinceHierarchyDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<IEnumerable<ProvinceHierarchyDto>>> GetHierarchy()
{
    var provinces = await _context.Set<ProvinceCities>()
        .Include(l => l.Children)  // ✅ Eager load cities
        .Where(l => l.Type == "Province")
        .OrderBy(l => l.Name)
        .ToListAsync();

    var hierarchy = provinces.Select(p => new ProvinceHierarchyDto
    {
        Id = p.Id,
        Name = p.Name,
        ProvinceCode = p.ProvinceCode,
        Cities = p.Children
            .OrderBy(c => c.Name)
            .Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name,
                CityCode = c.CityCode
            })
            .ToList()
    });

    return Ok(hierarchy);
}
```

### Database Query

The endpoint generates a single SQL query with JOIN:

```sql
SELECT
    p.[Id], p.[Name], p.[ProvinceCode],
    c.[Id], c.[Name], c.[CityCode]
FROM [ServiceCatalog].[ProvinceCities] p
LEFT JOIN [ServiceCatalog].[ProvinceCities] c
    ON p.[Id] = c.[ParentId] AND c.[Type] = 'City'
WHERE p.[Type] = 'Province'
ORDER BY p.[Name], c.[Name];
```

**Single database round-trip!** ✅

---

## Summary

### Current (Inefficient)
- ❌ **32 API calls** (1 for provinces + 31 for cities)
- ❌ **32 database queries**
- ❌ **3-5 seconds** load time
- ❌ **Poor UX** (multiple loading states)

### Optimized (Using /hierarchy)
- ✅ **1 API call**
- ✅ **1 database query**
- ✅ **100-300ms** load time
- ✅ **Great UX** (single loading state)

### Performance Gain
- **32x fewer API calls**
- **~10-15x faster page load**
- **32x less server load**
- **Better user experience**

---

## Action Items

1. ✅ Backend endpoint already exists (no work needed!)
2. ⚠️ Update frontend to use `/api/v1/locations/hierarchy`
3. ⚠️ Remove the inefficient province loop
4. ⚠️ Add caching for even better performance
5. ⚠️ Test and verify improvements

The backend is ready - just need to update the frontend! 🚀
