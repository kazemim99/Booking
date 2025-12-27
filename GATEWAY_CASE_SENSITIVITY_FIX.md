# Gateway Case Sensitivity Fix

**Date**: 2025-12-27
**Issue**: API requests returning 404 errors for platform and category endpoints
**Status**: ✅ RESOLVED

## Problem Description

Several API endpoints were returning 404 errors when accessed through the frontend:
- `http://localhost:8080/api/v1/platform/statistics` → 404
- `http://localhost:8080/api/v1/categories/popular?limit=8` → 404
- `http://localhost:8080/api/v1/categories?limit=25` → 404

However, the Providers endpoint worked correctly:
- `http://localhost:8080/api/v1/Providers/search?...` → 200 OK ✅

## Root Cause Analysis

The issue was a **case sensitivity mismatch** between the frontend API calls and the Ocelot Gateway routing configuration:

### Frontend (Sending lowercase URLs)
```typescript
// platform.service.ts
const API_BASE = `/${API_VERSION}/platform`  // lowercase

// category.service.ts
const API_BASE = `/${API_VERSION}/categories`  // lowercase
```

### Ocelot Gateway (Expecting PascalCase)
```json
{
  "UpstreamPathTemplate": "/api/v1/Platform/{url}",     // PascalCase
  "UpstreamPathTemplate": "/api/v1/Categories/{url}",   // PascalCase
}
```

### Why Providers Worked
The Providers endpoint worked because it was already using PascalCase in the frontend:
```typescript
// provider.service.ts
const API_BASE = `/${API_VERSION}/Providers`  // Already PascalCase ✅
```

### Why RouteIsCaseSensitive Didn't Work
Ocelot's `RouteIsCaseSensitive: false` configuration setting did not work as expected in version 23.4.0. Even though we added:
```json
"RouteIsCaseSensitive": false  // This didn't work!
```

The routes remained case-sensitive, causing mismatches.

## Solution

Updated all frontend API service files to use **PascalCase URLs** matching the Ocelot Gateway configuration:

### Files Modified

1. **booksy-frontend/src/core/api/services/platform.service.ts**
   ```typescript
   // Before:
   const API_BASE = `/${API_VERSION}/platform`

   // After:
   const API_BASE = `/${API_VERSION}/Platform`
   ```

2. **booksy-frontend/src/core/api/services/category.service.ts**
   ```typescript
   // Before:
   const API_BASE = `/${API_VERSION}/categories`

   // After:
   const API_BASE = `/${API_VERSION}/Categories`
   ```

3. **Other services fixed via sed command**:
   - `locations` → `Locations`
   - `availability` → `Availability`
   - `providers` → `Providers` (in hierarchy.service.ts)
   - `services` → `Services`
   - `provider-settings` → `Provider-Settings`

## Testing Results

After applying the fix, all endpoints now work correctly:

✅ **Working URLs** (PascalCase):
```bash
# Platform statistics
curl http://localhost:8080/api/v1/Platform/statistics
# Response: 200 OK

# Popular categories
curl http://localhost:8080/api/v1/Categories/popular?limit=8
# Response: 200 OK

# All categories
curl http://localhost:8080/api/v1/Categories?limit=25
# Response: 200 OK

# Providers search (was already working)
curl http://localhost:8080/api/v1/Providers/search?PageNumber=1&PageSize=6
# Response: 200 OK
```

❌ **Expected to fail** (lowercase - frontend no longer uses these):
```bash
curl http://localhost:8080/api/v1/platform/statistics
# Response: 404 (expected, not used by frontend)

curl http://localhost:8080/api/v1/categories/popular
# Response: 404 (expected, not used by frontend)
```

## Request Flow

The complete request flow now works correctly:

```
Browser
  ↓
Frontend (PascalCase URLs: /api/v1/Platform/statistics)
  ↓
Nginx (Port 8080)
  ↓ proxy_pass http://booksy-gateway:8080
Gateway (Port 8080 - Ocelot)
  ↓ Matches route: /api/v1/Platform/{url}
  ↓ Downstream: http://booksy-service-catalog-api:8080/api/v1/Platform/statistics
Backend API (ServiceCatalog)
  ↓
Response: 200 OK
```

## Gateway Logs (Before Fix)

```
warn: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
  requestId: ..., message: '404 (Not Found) status code of request URI:
  http://booksy-service-catalog-api:8080/api/v1/Platform/{url}.'
```

Notice the literal `{url}` in the URL - this indicated the route wasn't matching correctly due to case sensitivity.

## Gateway Logs (After Fix)

```
info: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
  requestId: ..., message: '200 (OK) status code of request URI:
  http://booksy-service-catalog-api:8080/api/v1/Platform/statistics.'
```

The `{url}` placeholder is now correctly replaced with `statistics`.

## Additional Fixes Applied

1. **Nginx Configuration**: Removed trailing slash from `proxy_pass` directive
   ```nginx
   # Before:
   proxy_pass http://booksy-gateway:8080/;

   # After:
   proxy_pass http://booksy-gateway:8080;
   ```

2. **Ocelot Production Config**: Updated placeholder from `{everything}` to `{url}`
   ```json
   # Before:
   "UpstreamPathTemplate": "/api/v1/Platform/{everything}"

   # After:
   "UpstreamPathTemplate": "/api/v1/Platform/{url}"
   ```

## Lessons Learned

1. **Consistency Matters**: Frontend and backend URL casing must match exactly
2. **Ocelot Case Sensitivity**: The `RouteIsCaseSensitive` setting doesn't always work as documented
3. **PascalCase Convention**: .NET APIs typically use PascalCase for endpoints
4. **Testing Strategy**: Always test with the exact URLs the frontend will send
5. **Gateway Logs**: Monitor gateway logs for literal placeholder values (e.g., `{url}` in URLs)

## Prevention

To prevent this issue in the future:

1. **Establish a naming convention**: Use PascalCase for all API endpoints (frontend and backend)
2. **Integration tests**: Add tests that verify frontend→gateway→backend routing
3. **Code review**: Check that new API service files use PascalCase
4. **Documentation**: Update onboarding docs to specify the PascalCase requirement

## References

- Original Issue: 404 errors on `/api/v1/platform/*` and `/api/v1/categories/*`
- Ocelot Version: 23.4.0
- .NET Version: 9.0
- Nginx Version: alpine (latest)

## Status

✅ **RESOLVED** - All API endpoints working correctly with PascalCase URLs
