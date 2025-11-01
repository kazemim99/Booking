# API Design Notes

## Business Hours Management

### Current Implementation (As of 2025-10-29)

#### Issue: Inconsistent Business Hours Endpoints

**Problem:**
The `hours.service.ts` defines endpoints that don't exist in the backend:
- `GET /v1/providers/{id}/business-hours` ❌ Not implemented
- `PUT /v1/providers/{id}/business-hours` ❌ Not implemented

**Current Reality:**
Business hours are managed through the provider update endpoint:
- `PUT /v1/providers/{id}` ✅ Includes `businessHours` field

**Frontend Workaround:**
The `BusinessHoursView.vue` now correctly uses:
1. `providerStore.updateProvider()` to save business hours via the provider endpoint
2. `hoursStore.state.baseHours` is updated locally after save (no API call)

### Recommendation for Future

**Option 1: Add Dedicated Business Hours Endpoints** (Preferred)
```
GET    /v1/providers/{id}/business-hours
PUT    /v1/providers/{id}/business-hours
GET    /v1/providers/{id}/holidays
POST   /v1/providers/{id}/holidays
DELETE /v1/providers/{id}/holidays/{holidayId}
GET    /v1/providers/{id}/exceptions
POST   /v1/providers/{id}/exceptions
DELETE /v1/providers/{id}/exceptions/{exceptionId}
```

Benefits:
- ✅ Consistent API design with holidays/exceptions
- ✅ Separation of concerns
- ✅ More granular control and permissions
- ✅ Better for caching and performance

**Option 2: Keep Current Approach**
Continue using the provider update endpoint for business hours, and update the frontend service definitions to match reality.

### Related Files
- Frontend: `booksy-frontend/src/modules/provider/services/hours.service.ts`
- Frontend: `booksy-frontend/src/modules/provider/stores/hours.store.ts`
- Frontend: `booksy-frontend/src/modules/provider/views/hours/BusinessHoursView.vue`
- Backend: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs`

### Status
- ⚠️ **Temporary Fix Applied**: Frontend now uses provider update endpoint correctly
- 📋 **TODO**: Backend team should implement dedicated business hours endpoints
- 🔄 **Phase 3**: Holidays/exceptions endpoints should follow the same pattern

### Breaking Changes
When dedicated endpoints are added:
1. Update `hours.service.ts` to use the correct endpoints
2. Update `hours.store.ts` `updateHours()` to call the service
3. Remove direct state mutation in `BusinessHoursView.vue` (line 617)
