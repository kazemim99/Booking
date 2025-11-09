# Integration Test Plan - Booksy Frontend

This document provides a comprehensive integration test plan for the newly implemented frontend features.

## 🎯 Test Scope

Testing integration between frontend (Vue 3) and backend APIs for:
- **Priority 4**: Gallery Management
- **Priority 5**: Financial & Payouts
- **Priority 6**: Customer Favorites & Quick Rebooking

---

## 🔧 Prerequisites

### Backend Services
Ensure the following services are running:
```bash
# ServiceCatalog API (Port 5010)
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run --launch-profile http

# UserManagement API (Port 5020)
cd src/UserManagement/Booksy.UserManagement.API
dotnet run --launch-profile http
```

### Frontend Service
```bash
cd booksy-frontend
npm install
npm run dev
```

### Database
Ensure database is running and migrations are applied:
```bash
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet ef database update

cd ../../UserManagement/Booksy.UserManagement.API
dotnet ef database update
```

### Test Accounts
Create test accounts:
- **Provider**: email: `provider@test.com`, password: `Test123!`
- **Customer**: email: `customer@test.com`, password: `Test123!`

---

## 📋 Test Cases

## Priority 4: Gallery Management

### Test Case 1.1: Upload Gallery Images
**Endpoint**: `POST /api/v1/providers/{providerId}/gallery`

**Steps**:
1. Login as provider
2. Navigate to Provider Dashboard → Gallery
3. Click "Upload Images"
4. Drag & drop 3 images (JPEG/PNG, < 5MB each)
5. Verify preview thumbnails appear
6. Click "Upload All"

**Expected Results**:
- ✅ Upload progress bars show for each image
- ✅ Success message appears
- ✅ Images appear in gallery grid
- ✅ Images are ordered correctly

**API Validation**:
```bash
curl -X GET http://localhost:5010/api/v1/providers/{providerId}/gallery \
  -H "Authorization: Bearer {token}"
```

---

### Test Case 1.2: Update Image Metadata
**Endpoint**: `PUT /api/v1/providers/{providerId}/gallery/{imageId}`

**Steps**:
1. Click "Edit" on an uploaded image
2. Update caption: "نمونه کار ۱"
3. Update alt text: "Sample haircut"
4. Click "Save"

**Expected Results**:
- ✅ Metadata saves successfully
- ✅ Caption displays in Persian
- ✅ Alt text is set on image tag

---

### Test Case 1.3: Reorder Gallery Images
**Endpoint**: `PUT /api/v1/providers/{providerId}/gallery/reorder`

**Steps**:
1. Drag image #3 to position #1
2. Verify reorder API call
3. Refresh page

**Expected Results**:
- ✅ Images reorder immediately (optimistic update)
- ✅ Order persists after refresh
- ✅ API receives correct imageIds array

---

### Test Case 1.4: Set Primary Image
**Endpoint**: `PUT /api/v1/providers/{providerId}/gallery/{imageId}/set-primary`

**Steps**:
1. Click "Set as Primary" on image #2
2. Verify star icon appears
3. Check provider profile

**Expected Results**:
- ✅ Image marked with primary indicator
- ✅ Only one primary image at a time
- ✅ Primary image shows on provider card

---

### Test Case 1.5: Delete Gallery Image
**Endpoint**: `DELETE /api/v1/providers/{providerId}/gallery/{imageId}`

**Steps**:
1. Click delete button on image
2. Confirm deletion in modal
3. Verify image removed

**Expected Results**:
- ✅ Confirmation modal appears
- ✅ Image removed from UI
- ✅ Deletion persists

---

## Priority 5: Financial & Payouts

### Test Case 2.1: View Financial Dashboard
**Endpoints**:
- `GET /api/v1/financial/provider/{providerId}/earnings/current-month`
- `GET /api/v1/financial/provider/{providerId}/earnings/previous-month`

**Steps**:
1. Login as provider with existing bookings
2. Navigate to Dashboard → Financial
3. View dashboard metrics

**Expected Results**:
- ✅ Current month earnings displayed
- ✅ Previous month earnings displayed
- ✅ Growth rate calculated correctly
- ✅ Persian number formatting (۱۲۳۴۵۶ instead of 123456)
- ✅ Currency formatted as "تومان"

**API Validation**:
```bash
curl -X GET "http://localhost:5010/api/v1/financial/provider/{providerId}/earnings/current-month" \
  -H "Authorization: Bearer {token}"
```

---

### Test Case 2.2: View Transaction History
**Endpoint**: `GET /api/v1/transactions/provider/{providerId}`

**Steps**:
1. Navigate to Financial → Transactions
2. Apply date range filter
3. Filter by transaction type
4. Sort by date descending

**Expected Results**:
- ✅ Transactions load with pagination
- ✅ Filters apply correctly
- ✅ Sorting works (date, amount)
- ✅ Transaction details display correctly
- ✅ Export button available

---

### Test Case 2.3: Create Payout Request
**Endpoint**: `POST /api/v1/payouts`

**Steps**:
1. Navigate to Financial → Payouts
2. Click "Request Payout"
3. Enter amount: 500000
4. Add bank account details
5. Submit request

**Expected Results**:
- ✅ Validation: minimum amount check (10,000 تومان)
- ✅ Validation: available balance check
- ✅ Payout created with status "Pending"
- ✅ Success message in Persian

**API Validation**:
```bash
curl -X POST http://localhost:5010/api/v1/payouts \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "providerId": "{providerId}",
    "amount": 500000,
    "bankAccountNumber": "6037997000000001",
    "notes": "درخواست واریز"
  }'
```

---

### Test Case 2.4: View Payout Status
**Endpoint**: `GET /api/v1/payouts/provider/{providerId}`

**Steps**:
1. Navigate to Financial → Payouts
2. Filter by status: "Pending"
3. View payout details

**Expected Results**:
- ✅ Payouts list displays correctly
- ✅ Status badge colors (Pending: yellow, Processing: blue, Completed: green, Failed: red)
- ✅ Requested date in Persian
- ✅ Amount formatted correctly

---

## Priority 6: Customer Favorites

### Test Case 3.1: Add Provider to Favorites
**Endpoint**: `POST /api/v1/customers/{customerId}/favorites`

**Steps**:
1. Login as customer
2. Browse providers
3. Click heart icon on a provider card
4. Verify heart fills with red

**Expected Results**:
- ✅ Heart icon changes from outline to filled
- ✅ Red color indicates favorited
- ✅ Optimistic UI update (immediate feedback)
- ✅ Persists after page refresh

**API Validation**:
```bash
curl -X POST http://localhost:5020/api/v1/customers/{customerId}/favorites \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "providerId": "{providerId}",
    "notes": "آرایشگاه مورد علاقه"
  }'
```

---

### Test Case 3.2: View Favorites List
**Endpoint**: `GET /api/v1/customers/{customerId}/favorites`

**Steps**:
1. Navigate to Customer Dashboard → Favorites
2. View "List" tab
3. Verify all favorited providers appear

**Expected Results**:
- ✅ Provider cards display correctly
- ✅ Logo, rating, category shown
- ✅ Last booked date (if available)
- ✅ Book button navigates to provider page
- ✅ Unfavorite button works

---

### Test Case 3.3: Remove from Favorites
**Endpoint**: `DELETE /api/v1/customers/{customerId}/favorites/{providerId}`

**Steps**:
1. In favorites list, click unfavorite button
2. Verify provider removed from list

**Expected Results**:
- ✅ Provider removed immediately
- ✅ Empty state shows if no favorites remain
- ✅ Deletion persists

**API Validation**:
```bash
curl -X DELETE http://localhost:5020/api/v1/customers/{customerId}/favorites/{providerId} \
  -H "Authorization: Bearer {token}"
```

---

### Test Case 3.4: Quick Rebook from Favorites
**Endpoint**: `GET /api/v1/customers/{customerId}/favorites/quick-rebook` (if implemented)

**Steps**:
1. Navigate to Favorites → "Quick Rebook" tab
2. View quick rebook suggestions
3. Select a time slot
4. Click "Quick Book"

**Expected Results**:
- ✅ Suggestions show last booked service
- ✅ Available time slots displayed
- ✅ Slot selection works
- ✅ Persian date formatting (امروز, فردا, etc.)
- ✅ Navigation to booking page with pre-filled data

---

## 🔍 Cross-Cutting Concerns

### Authentication & Authorization
**Test Cases**:
- ✅ JWT token included in all API requests
- ✅ 401 response triggers login redirect
- ✅ Token refresh works seamlessly
- ✅ Role-based access (provider vs customer routes)

### Error Handling
**Test Cases**:
- ✅ Network errors show user-friendly Persian messages
- ✅ Validation errors display field-specific messages
- ✅ 404 errors redirect to not found page
- ✅ 500 errors show generic error message

### Loading States
**Test Cases**:
- ✅ Skeleton loaders show during data fetch
- ✅ Spinners indicate ongoing operations
- ✅ Disabled buttons during async operations

### Caching
**Test Cases**:
- ✅ Favorites cached for 10 minutes
- ✅ Cache invalidation on add/remove
- ✅ Provider data cached appropriately

### Responsive Design
**Test Cases**:
- ✅ Mobile view (< 768px): single column layouts
- ✅ Tablet view (768px - 1024px): 2 column grids
- ✅ Desktop view (> 1024px): 3+ column grids
- ✅ Touch gestures work on mobile

### Accessibility
**Test Cases**:
- ✅ Keyboard navigation works
- ✅ Focus indicators visible
- ✅ Screen reader compatible (ARIA labels)
- ✅ Color contrast meets WCAG standards

---

## 🐛 Known Issues & Edge Cases

### Gallery Management
- [ ] Upload fails silently if file size > 5MB (needs better validation)
- [ ] Drag & drop doesn't work on Safari < 14
- [ ] No image compression before upload (large files slow)

### Financial Module
- [ ] Commission percentage hardcoded (should come from backend)
- [ ] No real-time updates for payout status
- [ ] Export functionality not implemented

### Favorites
- [ ] Quick rebook endpoint may not exist yet (verify with backend team)
- [ ] No bulk favorite operations (select multiple)
- [ ] No favorites sync across devices (if using local cache)

---

## 📊 Test Execution Report Template

```
Date: _______________
Tester: _______________
Environment: Development / Staging / Production

| Test Case | Status | Notes |
|-----------|--------|-------|
| 1.1 Upload Gallery Images | ⬜ Pass ⬜ Fail | |
| 1.2 Update Image Metadata | ⬜ Pass ⬜ Fail | |
| 1.3 Reorder Gallery | ⬜ Pass ⬜ Fail | |
| 1.4 Set Primary Image | ⬜ Pass ⬜ Fail | |
| 1.5 Delete Image | ⬜ Pass ⬜ Fail | |
| 2.1 Financial Dashboard | ⬜ Pass ⬜ Fail | |
| 2.2 Transaction History | ⬜ Pass ⬜ Fail | |
| 2.3 Create Payout | ⬜ Pass ⬜ Fail | |
| 2.4 View Payout Status | ⬜ Pass ⬜ Fail | |
| 3.1 Add to Favorites | ⬜ Pass ⬜ Fail | |
| 3.2 View Favorites List | ⬜ Pass ⬜ Fail | |
| 3.3 Remove from Favorites | ⬜ Pass ⬜ Fail | |
| 3.4 Quick Rebook | ⬜ Pass ⬜ Fail | |

Overall Status: ____ / ____ Passed
```

---

## 🚀 Quick Start Commands

### Start Backend Services
```bash
# Terminal 1: ServiceCatalog API
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api
dotnet run --launch-profile http

# Terminal 2: UserManagement API
cd src/UserManagement/Booksy.UserManagement.API
dotnet run --launch-profile http
```

### Start Frontend
```bash
# Terminal 3: Frontend Dev Server
cd booksy-frontend
npm run dev
```

### Run Database Migrations
```bash
# ServiceCatalog
cd src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure
dotnet ef database update --startup-project ../Booksy.ServiceCatalog.Api

# UserManagement
cd src/UserManagement/Booksy.UserManagement.Infrastructure
dotnet ef database update --startup-project ../Booksy.UserManagement.API
```

---

## 📞 Support

If you encounter issues:
1. Check browser console for errors
2. Verify API responses in Network tab
3. Check backend logs
4. Verify JWT token is valid
5. Ensure database has test data

---

## ✅ Checklist Before Testing

- [ ] Backend services running
- [ ] Database migrations applied
- [ ] Test accounts created
- [ ] Frontend dev server running
- [ ] JWT tokens obtained
- [ ] Test data seeded (providers, services, bookings)
