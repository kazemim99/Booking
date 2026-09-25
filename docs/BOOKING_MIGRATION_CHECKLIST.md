# Booking System Migration Checklist

**Migration Type**: Staff Collection → Hierarchy-Based Architecture
**Date**: December 8, 2025
**Status**: ✅ Completed

## Quick Reference

### What Changed?
- ❌ **Removed**: `provider.Staff` collection pattern
- ✅ **Added**: Hierarchy-based staff provider loading
- ⚠️ **Breaking**: API field renamed `staffId` → `staffProviderId`

### Who Needs to Update?
- ✅ Backend developers (C# API)
- ✅ Frontend developers (Vue.js/TypeScript)
- ✅ Mobile app developers (if applicable)
- ✅ API documentation maintainers
- ✅ QA/Test teams

---

## Backend Developer Checklist

### ✅ Code Changes Completed

- [x] Updated `CreateBookingCommand` to use `StaffProviderId`
- [x] Updated `CreateBookingCommandHandler` validation logic
- [x] Updated `CreateBookingResult` to use `StaffProviderId`
- [x] Updated API request model `CreateBookingRequest`
- [x] Updated API response models (`BookingResponse`, `BookingDetailsResponse`)
- [x] Updated `BookingsController` mappings
- [x] Removed references to `provider.Staff` collection
- [x] Removed `service.IsStaffQualified()` calls
- [x] Added hierarchy validation (`ParentProviderId` check)
- [x] Added staff provider status check

### 🔍 Code Review Checklist

- [ ] All `StaffId` references changed to `StaffProviderId`
- [ ] No remaining `provider.Staff` collection access
- [ ] Hierarchy validation implemented correctly
- [ ] Error messages are user-friendly
- [ ] Logging includes `StaffProviderId` context
- [ ] Transaction boundaries are correct

### 🧪 Testing Checklist

- [ ] **Happy Path**: Booking created with valid staff provider
- [ ] **Invalid Hierarchy**: Booking fails when staff belongs to different organization
- [ ] **Inactive Staff**: Booking fails when staff provider is inactive
- [ ] **Not Found**: Booking fails when staff provider doesn't exist
- [ ] **Service Validation**: Service belongs to correct organization
- [ ] **Conflict Detection**: Time slot conflicts detected correctly

### 📊 Database Checklist

- [x] **No schema changes required** (StaffId column unchanged)
- [ ] Verify existing bookings still work
- [ ] Verify StaffId values reference valid ProviderId values
- [ ] Run data integrity checks

---

## Frontend Developer Checklist

### ✅ TypeScript Interface Updates Completed

- [x] Updated `CreateBookingRequest` interface in `booking.service.ts`
- [x] Updated `Appointment` interface in `booking.types.ts`
- [x] Updated `BookingRequest` interface
- [x] Updated `Schedule` interface
- [x] Updated `AvailabilitySlot` interface
- [x] Updated `AvailabilityRequest` interface

### ✅ Component Updates Completed

- [x] Updated `BookingWizard.vue` request mapping
- [x] Changed `staffId` → `staffProviderId` in API calls

### 🔍 Frontend Review Checklist

- [ ] All TypeScript errors resolved
- [ ] No console errors in browser
- [ ] API requests send `staffProviderId` correctly
- [ ] API responses parse `staffProviderId` correctly
- [ ] UI displays staff information correctly
- [ ] Staff selection flow works end-to-end

### 🧪 Frontend Testing Checklist

- [ ] **Staff Selection**: Can select staff from list
- [ ] **Booking Creation**: Creates booking with correct `staffProviderId`
- [ ] **Booking Display**: Shows correct staff name/info
- [ ] **Error Handling**: Displays appropriate error messages
- [ ] **API Errors**: 400/404/409 errors handled gracefully

### 📱 Browser Compatibility

- [ ] Chrome/Edge (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Mobile browsers

---

## API Documentation Checklist

### ✅ Documentation Updates Completed

- [x] Created `BOOKING_HIERARCHY_MIGRATION.md`
- [x] Created `BOOKING_API_REFERENCE.md`
- [x] Updated `FIXES_SUMMARY_DEC_2025.md`
- [x] Created `BOOKING_MIGRATION_CHECKLIST.md` (this file)

### 📝 Documentation Review Checklist

- [ ] OpenAPI/Swagger spec updated
- [ ] Postman collection updated
- [ ] API examples use `staffProviderId`
- [ ] Migration guide is clear
- [ ] Breaking changes documented
- [ ] Error response examples updated

---

## QA/Testing Team Checklist

### 🔬 Test Scenarios

#### Positive Tests
- [ ] **TC-01**: Create booking with valid organization and staff provider
- [ ] **TC-02**: Create booking with different staff providers from same organization
- [ ] **TC-03**: View booking details shows correct `staffProviderId`
- [ ] **TC-04**: List bookings returns `staffProviderId` in all items
- [ ] **TC-05**: Search bookings by staff provider ID works

#### Negative Tests
- [ ] **TC-06**: Create booking with staff from different organization fails with 409
- [ ] **TC-07**: Create booking with inactive staff provider fails with 409
- [ ] **TC-08**: Create booking with non-existent staff provider fails with 404
- [ ] **TC-09**: Create booking without `staffProviderId` fails with 400
- [ ] **TC-10**: Create booking with invalid GUID format fails with 400

#### Edge Cases
- [ ] **TC-11**: Staff provider has no bookings yet (first booking)
- [ ] **TC-12**: Staff provider has conflicting bookings
- [ ] **TC-13**: Staff provider schedule has no availability
- [ ] **TC-14**: Multiple bookings for same staff at different times
- [ ] **TC-15**: Organization has multiple staff providers

### 📋 Regression Testing

- [ ] Existing bookings still display correctly
- [ ] Booking cancellation works
- [ ] Booking rescheduling works
- [ ] Booking confirmation works
- [ ] Payment flow works
- [ ] Customer notifications work
- [ ] Provider dashboard shows bookings

---

## DevOps/Deployment Checklist

### 🚀 Deployment Preparation

- [ ] **No database migrations required** ✅
- [ ] Backend build successful (0 errors)
- [ ] Frontend build successful (0 errors)
- [ ] Docker images built successfully
- [ ] Environment variables unchanged

### 📦 Deployment Steps

1. [ ] Deploy backend API first
2. [ ] Verify API health check passes
3. [ ] Deploy frontend application
4. [ ] Verify frontend loads without errors
5. [ ] Smoke test booking creation
6. [ ] Monitor error logs
7. [ ] Monitor API metrics

### 🔍 Post-Deployment Monitoring

- [ ] API response times normal
- [ ] Error rate within acceptable range
- [ ] No increase in 400/404/409 errors
- [ ] Booking creation success rate normal
- [ ] Customer complaints minimal

### 🔙 Rollback Plan

If issues occur:
1. Revert frontend deployment (no API changes visible)
2. Revert backend deployment if needed
3. **No database rollback needed** (schema unchanged)
4. Notify users of temporary issues

---

## Integration Team Checklist

### 🔌 API Integrations

- [ ] Update external API clients (if any)
- [ ] Update webhooks (if `staffId` is included)
- [ ] Update reporting systems
- [ ] Update analytics tracking
- [ ] Update monitoring dashboards

### 📧 Third-Party Notifications

- [ ] Email templates use correct field name
- [ ] SMS templates use correct field name
- [ ] Push notifications use correct field name
- [ ] Calendar integrations work

---

## Communication Checklist

### 📢 Internal Communication

- [ ] Notify development team
- [ ] Notify QA team
- [ ] Notify DevOps team
- [ ] Notify product team
- [ ] Update team documentation

### 📱 External Communication

- [ ] Notify API consumers of breaking change
- [ ] Update API changelog
- [ ] Send migration guide to partners
- [ ] Provide deprecation timeline (if applicable)
- [ ] Update developer portal

---

## Sign-Off

### Team Sign-Off

- [ ] **Backend Lead**: __________________ Date: __________
- [ ] **Frontend Lead**: _________________ Date: __________
- [ ] **QA Lead**: _____________________ Date: __________
- [ ] **DevOps Lead**: _________________ Date: __________
- [ ] **Product Owner**: _______________ Date: __________

### Deployment Approval

- [ ] **Technical Lead**: _______________ Date: __________
- [ ] **Engineering Manager**: __________ Date: __________

---

## Quick Reference Commands

### Build Commands
```bash
# Backend
cd c:\Repos\Booking
dotnet build src/BoundedContexts/ServiceCatalog/AsanRezerve.ServiceCatalog.Api/AsanRezerve.ServiceCatalog.Api.csproj

# Frontend (if applicable)
cd asanrezerve-frontend
npm run build
```

### Test Commands
```bash
# Backend Unit Tests
dotnet test tests/AsanRezerve.ServiceCatalog.Application.UnitTests/

# Backend Integration Tests
dotnet test tests/AsanRezerve.ServiceCatalog.IntegrationTests/

# Frontend Tests
npm run test
```

### API Test
```bash
# Create booking test
curl -X POST "https://api.asanrezerve.com/api/v1/bookings" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "providerId": "guid",
    "serviceId": "guid",
    "staffProviderId": "guid",
    "startTime": "2025-01-15T10:00:00Z"
  }'
```

---

## Notes

- **Build Status**: ✅ All projects build successfully (0 errors)
- **Breaking Changes**: ⚠️ API field renamed (requires client updates)
- **Database**: ✅ No schema changes (no migrations needed)
- **Rollback**: ✅ Easy rollback (no data changes)
- **Risk Level**: 🟡 Medium (breaking API change but isolated to booking)

---

**Last Updated**: December 8, 2025
**Migration Status**: ✅ Code Complete - Pending Deployment
**Documentation**: Complete
