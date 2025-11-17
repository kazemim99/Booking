# Provider Search API - Enhancement Implementation Guide

**Date:** November 16, 2025
**Feature:** Enhanced Provider Search with Advanced Filtering & Sorting
**RICE Score:** 6.4 (Reach: 80% × Impact: 4 × Confidence: 1.0 / Effort: 18 days)
**Status:** 🔧 IN PROGRESS - Partial Implementation

---

## 🎯 Overview

The Provider Search API **already exists** but lacks critical discovery features that prevent effective provider filtering and sorting. This guide details the enhancements needed to make it production-ready.

---

## ✅ What Already Exists

### Infrastructure in Place:
1. ✅ **SearchProvidersQuery** - Application layer query
2. ✅ **SearchProvidersQueryHandler** - Query handler with specifications
3. ✅ **SearchProvidersSpecification** - Domain specification pattern
4. ✅ **API Endpoint** - `GET /api/v1/providers/search`
5. ✅ **Pagination** - Generic pagination support
6. ✅ **Basic Filters:** SearchTerm, City, State, Type, MinRating, VerifiedOnly

---

## ❌ What's Missing (High Priority)

### Critical Features for Discovery:
1. ❌ **Service Category Filter** - Filter by service type (haircut, massage, spa)
2. ❌ **Date-based Availability** - Show only providers available on specific date
3. ❌ **Price Range Filter** - Budget/Moderate/Premium tiers
4. ❌ **Advanced Sorting** - By rating, popularity (booking count), price, distance
5. ❌ **Distance/Geospatial Search** - Find providers near user location
6. ❌ **Provider Statistics** - Booking count, review count for sorting/display
7. ❌ **Full-Text Search** - Persian text search optimization

---

## 📋 Implementation Roadmap

### Phase 1: Enhanced Filters (Days 1-5) ⭐ STARTED
**Status:** Partial - Request models updated

#### Task 1.1: Update Request/Query Models ✅ DONE
**Files Modified:**
- ✅ `SearchProvidersRequest.cs` - Added 7 new filter parameters
- ✅ `SearchProvidersQuery.cs` - Added corresponding query parameters

**New Parameters Added:**
```csharp
// Service category filter
public string? ServiceCategory { get; set; }

// Availability filter
public DateTime? AvailableOn { get; set; }

// Price range filter
public string? PriceRange { get; set; }  // "budget", "moderate", "premium"

// Sorting options
public string SortBy { get; set; } = "rating";  // "rating", "popularity", "price", "distance"
public bool SortDescending { get; set; } = true;

// Geospatial filter
public double? UserLatitude { get; set; }
public double? UserLongitude { get; set; }
```

---

#### Task 1.2: Update Specification (TODO)
**File:** `SearchProvidersSpecification.cs`

**Changes Needed:**

```csharp
public class SearchProvidersSpecification : Specification<Provider>
{
    public SearchProvidersSpecification(
        string? searchTerm = null,
        // ... existing parameters
        string? serviceCategory = null,         // NEW
        DateTime? availableOn = null,           // NEW
        string? priceRange = null,              // NEW
        bool includeInactive = false)
    {
        // Existing filters...

        // NEW: Service category filter
        if (!string.IsNullOrWhiteSpace(serviceCategory))
        {
            AddCriteria(p => p.Services.Any(s =>
                s.Category.ToString().ToLower() == serviceCategory.ToLower() &&
                s.Status == ServiceStatus.Active));
        }

        // NEW: Availability filter
        if (availableOn.HasValue)
        {
            var date = DateOnly.FromDateTime(availableOn.Value);
            AddCriteria(p => p.ProviderAvailability.Any(a =>
                a.Date == date &&
                a.Status == AvailabilityStatus.Available));
        }

        // NEW: Price range filter
        if (!string.IsNullOrWhiteSpace(priceRange))
        {
            AddCriteria(p => p.Profile.PriceRange.ToLower() == priceRange.ToLower());
        }
    }
}
```

**Database Considerations:**
- Requires `Provider.Services` navigation property
- Requires `Provider.ProviderAvailability` navigation property
- May need indexes on `ProviderAvailability.Date` and `ProviderAvailability.Status`

---

#### Task 1.3: Update Query Handler (TODO)
**File:** `SearchProvidersQueryHandler.cs`

**Changes Needed:**

1. **Pass New Parameters to Specification:**
```csharp
var specification = new SearchProvidersSpecification(
    searchTerm: request.SearchTerm,
    // ... existing parameters
    serviceCategory: request.ServiceCategory,    // NEW
    availableOn: request.AvailableOn,           // NEW
    priceRange: request.PriceRange,             // NEW
    includeInactive: request.IncludeInactive);
```

2. **Add Sorting Logic:**
```csharp
// After creating specification, before pagination
var providers = await _providerRepository.GetFilteredAsync(specification, cancellationToken);

// Apply sorting based on request
providers = request.SortBy.ToLowerInvariant() switch
{
    "rating" => request.SortDescending
        ? providers.OrderByDescending(p => p.AverageRating)
        : providers.OrderBy(p => p.AverageRating),

    "popularity" => request.SortDescending
        ? providers.OrderByDescending(p => p.Statistics.TotalBookings)
        : providers.OrderBy(p => p.Statistics.TotalBookings),

    "price" => request.SortDescending
        ? providers.OrderByDescending(p => p.Profile.AveragePriceIRR)
        : providers.OrderBy(p => p.Profile.AveragePriceIRR),

    "distance" => request.UserLatitude.HasValue && request.UserLongitude.HasValue
        ? providers.OrderBy(p => CalculateDistance(
            request.UserLatitude.Value,
            request.UserLongitude.Value,
            p.Location.Coordinates.Latitude,
            p.Location.Coordinates.Longitude))
        : providers.OrderByDescending(p => p.AverageRating), // Fallback to rating

    _ => providers.OrderByDescending(p => p.AverageRating) // Default: rating desc
};

// Then apply pagination
var result = await providers
    .Skip((request.PageNumber - 1) * request.PageSize)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);
```

3. **Add Distance Calculation Method:**
```csharp
/// <summary>
/// Calculate distance between two points using Haversine formula
/// Returns distance in kilometers
/// </summary>
private static double CalculateDistance(
    double lat1, double lon1,
    double lat2, double lon2)
{
    const double R = 6371; // Earth radius in kilometers

    var dLat = ToRadians(lat2 - lat1);
    var dLon = ToRadians(lon2 - lon1);

    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

    return R * c; // Distance in kilometers
}

private static double ToRadians(double degrees) => degrees * Math.PI / 180;
```

---

### Phase 2: Provider Statistics Enhancement (Days 6-8)

**Problem:** Provider entity lacks booking count and review count for sorting.

**Solution:** Add statistics navigation property or denormalize counts.

#### Option A: Use Existing Statistics (Recommended)
**File:** `Provider.cs` (Domain)

Check if `Provider.Statistics` already exists:
```csharp
public class Provider
{
    // ... existing properties
    public ProviderStatistics Statistics { get; private set; }  // If this exists, use it!
}

public class ProviderStatistics
{
    public int TotalBookings { get; set; }
    public int ReviewCount { get; set; }
    public decimal AverageRating { get; set; }
}
```

#### Option B: Calculate On-the-Fly (Performance Hit)
```csharp
// In SearchProvidersQueryHandler
var result = await query
    .Select(p => new ProviderSearchItem(
        // ... existing mapping
        TotalBookings: _context.Bookings.Count(b => b.ProviderId == p.Id),
        ReviewCount: _context.Reviews.Count(r => r.ProviderId == p.Id)))
    .ToListAsync(cancellationToken);
```

**Recommendation:** If `ProviderStatistics` entity exists (from ProviderStatisticsSeeder), use Option A.

---

### Phase 3: Full-Text Search for Persian (Days 9-12)

**Problem:** Basic string matching doesn't work well for Persian text.

#### Option A: PostgreSQL Full-Text Search (Simpler)
```sql
-- Add tsvector column
ALTER TABLE "ServiceCatalog"."Providers"
ADD COLUMN "SearchVector" tsvector;

-- Create index
CREATE INDEX idx_providers_search_vector
ON "ServiceCatalog"."Providers"
USING GIN("SearchVector");

-- Update trigger to maintain tsvector
CREATE OR REPLACE FUNCTION update_provider_search_vector()
RETURNS trigger AS $$
BEGIN
    NEW."SearchVector" :=
        setweight(to_tsvector('simple', COALESCE(NEW."BusinessName", '')), 'A') ||
        setweight(to_tsvector('simple', COALESCE(NEW."BusinessDescription", '')), 'B');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trig_update_provider_search_vector
BEFORE INSERT OR UPDATE ON "ServiceCatalog"."Providers"
FOR EACH ROW EXECUTE FUNCTION update_provider_search_vector();
```

**In Specification:**
```csharp
if (!string.IsNullOrWhiteSpace(searchTerm))
{
    // Use PostgreSQL full-text search
    AddCriteria(p => EF.Functions.ToTsVector("simple", p.Profile.BusinessName + " " + p.Profile.BusinessDescription)
        .Matches(EF.Functions.PlainToTsQuery("simple", searchTerm)));
}
```

#### Option B: ElasticSearch (Best Performance, More Complex)
**Requires:**
1. ElasticSearch server setup
2. NEST NuGet package
3. Provider indexing service
4. Separate ElasticSearch repository

**Indexing:**
```csharp
public class ProviderDocument
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; }
    public string BusinessDescription { get; set; }
    public string[] ServiceCategories { get; set; }
    public string City { get; set; }
    public decimal Rating { get; set; }
    public GeoLocation Location { get; set; }
}
```

**Search Query:**
```csharp
var response = await _elasticClient.SearchAsync<ProviderDocument>(s => s
    .Query(q => q
        .Bool(b => b
            .Must(
                m => m.MultiMatch(mm => mm
                    .Fields(f => f
                        .Field(p => p.BusinessName, boost: 2.0)
                        .Field(p => p.BusinessDescription))
                    .Query(searchTerm)
                    .Fuzziness(Fuzziness.Auto)),
                m => m.Term(t => t.Field(p => p.City).Value(city)))
            .Filter(
                f => f.Range(r => r.Field(p => p.Rating).GreaterThanOrEquals(minRating)))))
    .Sort(sort => sort.Descending(p => p.Rating)));
```

**Recommendation:** Start with PostgreSQL (Option A), migrate to ElasticSearch if performance issues arise.

---

### Phase 4: Geospatial Search Enhancement (Days 13-15)

**Requirement:** Find providers within X km of user location.

#### Using PostGIS Extension (Recommended)
**Setup:**
```sql
-- Enable PostGIS extension
CREATE EXTENSION IF NOT EXISTS postgis;

-- Add geography column
ALTER TABLE "ServiceCatalog"."Providers"
ADD COLUMN "LocationPoint" geography(POINT, 4326);

-- Update existing data
UPDATE "ServiceCatalog"."Providers"
SET "LocationPoint" = ST_SetSRID(ST_MakePoint("Longitude", "Latitude"), 4326);

-- Create spatial index
CREATE INDEX idx_providers_location_point
ON "ServiceCatalog"."Providers"
USING GIST("LocationPoint");
```

**In Specification:**
```csharp
if (userLatitude.HasValue && userLongitude.HasValue && maxDistanceKm.HasValue)
{
    var userLocation = $"POINT({userLongitude} {userLatitude})";

    AddCriteria(p =>
        EF.Functions.DistanceBetween(
            p.LocationPoint,
            EF.Functions.GeometryFromText(userLocation, 4326)) <= maxDistanceKm * 1000); // meters
}
```

**Enhanced Request:**
```csharp
public double? MaxDistanceKm { get; set; }  // Add to SearchProvidersRequest
```

---

### Phase 5: Response Enhancement (Days 16-18)

**Update** `ProviderSearchItem.cs` and response mapping:

```csharp
public record ProviderSearchItem(
    Guid Id,
    string BusinessName,
    string Description,
    ProviderType Type,
    ProviderStatus Status,
    string City,
    string State,
    string Country,
    string? LogoUrl,
    bool AllowOnlineBooking,
    bool OffersMobileServices,
    decimal AverageRating,
    int ServiceCount,
    int YearsInBusiness,
    bool IsVerified,
    DateTime RegisteredAt,
    DateTime? LastActiveAt,

    // NEW FIELDS
    int TotalBookings,           // For popularity sorting
    int ReviewCount,             // For trust signals
    string PriceRange,           // Budget/Moderate/Premium
    double? DistanceKm,          // Distance from user (if location provided)
    bool AvailableToday,         // Quick availability indicator
    string[] ServiceCategories); // List of services offered
```

---

## 🧪 Testing Guide

### Test Scenario 1: Service Category Filter
```bash
# Find all haircut providers in Tehran
curl -X GET "http://localhost:5020/api/v1/providers/search?serviceCategory=haircut&city=Tehran&pageSize=10"
```

**Expected:**
- Only providers offering haircut services
- All in Tehran city
- Up to 10 results

**Verification:**
```sql
SELECT p."BusinessName", s."Category"
FROM "ServiceCatalog"."Providers" p
JOIN "ServiceCatalog"."Services" s ON s."ProviderId" = p."Id"
WHERE s."Category" = 'Haircut'
  AND p."City" = 'Tehran';
```

---

### Test Scenario 2: Availability Filter
```bash
# Find providers available on November 20, 2025
curl -X GET "http://localhost:5020/api/v1/providers/search?availableOn=2025-11-20&city=Tehran"
```

**Expected:**
- Only providers with Available slots on 2025-11-20
- Sorted by rating (default)

**Verification:**
```sql
SELECT DISTINCT p."BusinessName", COUNT(a."SlotId") as AvailableSlots
FROM "ServiceCatalog"."Providers" p
JOIN "ServiceCatalog"."ProviderAvailability" a ON a."ProviderId" = p."Id"
WHERE a."Date" = '2025-11-20'
  AND a."Status" = 'Available'
  AND p."City" = 'Tehran'
GROUP BY p."Id", p."BusinessName";
```

---

### Test Scenario 3: Price Range Filter
```bash
# Find budget-friendly providers
curl -X GET "http://localhost:5020/api/v1/providers/search?priceRange=budget&city=Tehran"
```

**Expected:**
- Only providers with PriceRange = "Budget"

---

### Test Scenario 4: Sort by Popularity
```bash
# Find most popular providers (most bookings)
curl -X GET "http://localhost:5020/api/v1/providers/search?sortBy=popularity&sortDescending=true&pageSize=10"
```

**Expected:**
- Providers sorted by TotalBookings descending
- Top 10 most booked providers

---

### Test Scenario 5: Distance-based Search
```bash
# Find providers within 5km of current location
curl -X GET "http://localhost:5020/api/v1/providers/search?userLatitude=35.6892&userLongitude=51.3890&sortBy=distance&maxDistanceKm=5"
```

**Expected:**
- Providers within 5km radius
- Sorted by distance (nearest first)
- Response includes `distanceKm` field

---

### Test Scenario 6: Combined Filters
```bash
# Complex search: Available tomorrow, haircut, 4+ rating, near me
curl -X GET "http://localhost:5020/api/v1/providers/search?serviceCategory=haircut&availableOn=2025-11-17&minRating=4.0&userLatitude=35.6892&userLongitude=51.3890&sortBy=distance"
```

**Expected:**
- Haircut providers
- Available on 2025-11-17
- Rating >= 4.0
- Sorted by distance from user

---

## 📊 Database Indexes Required

### For Optimal Performance:

```sql
-- Service category filter
CREATE INDEX idx_services_provider_category
ON "ServiceCatalog"."Services" ("ProviderId", "Category", "Status");

-- Availability filter
CREATE INDEX idx_availability_provider_date_status
ON "ServiceCatalog"."ProviderAvailability" ("ProviderId", "Date", "Status");

-- Price range filter
CREATE INDEX idx_providers_pricerange
ON "ServiceCatalog"."Providers" ("PriceRange");

-- Sorting indexes
CREATE INDEX idx_providers_rating
ON "ServiceCatalog"."Providers" ("AverageRating" DESC);

CREATE INDEX idx_provider_statistics_bookings
ON "ServiceCatalog"."ProviderStatistics" ("ProviderId", "TotalBookings" DESC);

-- Full-text search (PostgreSQL)
CREATE INDEX idx_providers_search_vector
ON "ServiceCatalog"."Providers" USING GIN("SearchVector");

-- Geospatial search (PostGIS)
CREATE INDEX idx_providers_location_point
ON "ServiceCatalog"."Providers" USING GIST("LocationPoint");
```

---

## 🚀 API Endpoint Specification

### Complete Request Example
```http
GET /api/v1/providers/search?searchTerm=آرایشگاه&serviceCategory=haircut&city=Tehran&availableOn=2025-11-20&minRating=4.0&priceRange=moderate&sortBy=distance&sortDescending=false&userLatitude=35.6892&userLongitude=51.3890&pageNumber=1&pageSize=20
```

### Response Example
```json
{
  "items": [
    {
      "id": "guid",
      "businessName": "آرایشگاه زیبایی سارا",
      "description": "خدمات تخصصی زیبایی و آرایش",
      "type": "Beauty",
      "status": "Active",
      "city": "Tehran",
      "state": "Tehran",
      "country": "Iran",
      "logoUrl": "https://...",
      "allowOnlineBooking": true,
      "offersMobileServices": false,
      "averageRating": 4.7,
      "serviceCount": 12,
      "yearsInBusiness": 3,
      "isVerified": true,
      "totalBookings": 450,
      "reviewCount": 120,
      "priceRange": "Moderate",
      "distanceKm": 2.3,
      "availableToday": true,
      "serviceCategories": ["Haircut", "Coloring", "Styling"],
      "registeredAt": "2021-03-15T...",
      "lastActiveAt": "2025-11-16T..."
    }
  ],
  "totalCount": 45,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## ⚡ Performance Optimization

### Caching Strategy
```csharp
// In SearchProvidersQueryHandler
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
public async Task<PagedResult<ProviderSearchItem>> Handle(...)
{
    var cacheKey = $"provider-search:{GetHashCode(request)}";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await ExecuteSearchAsync(request, cancellationToken);
    });
}
```

### Query Optimization
1. **Use AsNoTracking()** for read-only queries
2. **Select only needed fields** (avoid loading entire entities)
3. **Use projection** to ProviderSearchItem early in the query
4. **Limit eager loading** (use Include sparingly)
5. **Add database indexes** for all filter columns

---

## 📝 Implementation Checklist

### Phase 1: Enhanced Filters ✅ STARTED
- [x] Update SearchProvidersRequest with new parameters
- [x] Update SearchProvidersQuery with new parameters
- [ ] Update SearchProvidersSpecification with filter logic
- [ ] Update SearchProvidersQueryHandler with sorting logic
- [ ] Add distance calculation method
- [ ] Test service category filter
- [ ] Test availability filter
- [ ] Test price range filter
- [ ] Test sorting options

### Phase 2: Provider Statistics
- [ ] Verify ProviderStatistics entity exists
- [ ] Update Provider entity with Statistics navigation
- [ ] Include Statistics in search query
- [ ] Map Statistics to response
- [ ] Test popularity sorting

### Phase 3: Full-Text Search
- [ ] Choose approach (PostgreSQL vs ElasticSearch)
- [ ] Add tsvector column (if PostgreSQL)
- [ ] Create search vector index
- [ ] Update trigger for automatic indexing
- [ ] Update specification with full-text query
- [ ] Test Persian text search

### Phase 4: Geospatial Search
- [ ] Install PostGIS extension
- [ ] Add geography column to Providers table
- [ ] Populate location points from coordinates
- [ ] Create spatial index (GIST)
- [ ] Add distance filter to specification
- [ ] Test distance-based search

### Phase 5: Response Enhancement
- [ ] Update ProviderSearchItem with new fields
- [ ] Update mapping in query handler
- [ ] Add distance calculation to response
- [ ] Add service categories aggregation
- [ ] Test complete response structure

### Phase 6: Testing & Documentation
- [ ] Write integration tests for all filters
- [ ] Write integration tests for all sorting options
- [ ] Performance testing (1000+ providers)
- [ ] Load testing (100+ concurrent requests)
- [ ] Update API documentation (Swagger)
- [ ] Create user guide for frontend team

---

## 🎯 Success Metrics

### Functional Requirements
- ✅ Service category filtering works
- ✅ Availability filtering works
- ✅ Price range filtering works
- ✅ All sorting options work (rating, popularity, price, distance)
- ✅ Distance calculation accurate within 100m
- ✅ Persian text search works correctly
- ✅ Combined filters work together

### Performance Targets
- ⏱️ Search response time: <500ms (95th percentile)
- ⏱️ Distance calculation: <100ms for 1000 providers
- ⏱️ Full-text search: <200ms for large result sets
- 📊 Concurrent requests: Handle 100+ without degradation
- 💾 Cache hit rate: >70% for common searches

### Business Metrics
- 📈 Search usage: Track most common filters
- 🎯 Search success rate: % of searches with results
- ⏰ Average filters per search
- 🔄 Refinement rate: % of users who refine search

---

## 🔄 Next Steps

1. **Complete Phase 1** (1-2 days)
   - Finish specification and handler updates
   - Test all new filters

2. **Implement Phase 2** (1 day)
   - Add provider statistics to search

3. **Choose Full-Text Approach** (Decision point)
   - Start with PostgreSQL for simplicity
   - Plan ElasticSearch migration if needed

4. **Implement Phases 3-5** (1-2 weeks)
   - Full-text search
   - Geospatial search
   - Response enhancements

5. **Testing & Optimization** (1 week)
   - Integration tests
   - Performance testing
   - Index optimization

---

**Created:** November 16, 2025
**Status:** 🔧 IN PROGRESS (Days 1-2 of 18 complete)
**Next Action:** Complete SearchProvidersSpecification updates
**Estimated Completion:** ~16 days remaining
