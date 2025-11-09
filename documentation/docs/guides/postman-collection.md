---
sidebar_position: 1
---

# Booksy API - Postman Collection

## Overview

This comprehensive Postman collection includes **all 120+ API endpoints** from the Booksy booking system, organized by bounded context and functionality. All sample data reflects **Iranian cultural context** with Persian names, Iranian phone numbers, national codes, addresses, and currency (IRR).

## Files Included

1. **Booksy_API_Collection.postman_collection.json** - Complete API collection
2. **Booksy_API.postman_environment.json** - Environment variables for local development

## API Coverage

### User Management APIs (UserManagement Bounded Context)
- **Authentication** (4 endpoints)
  - Login, Logout, Refresh Token, Forgot Password
- **Phone Verification** (5 endpoints)
  - Send/Verify OTP codes, Resend OTP
- **Users** (9 endpoints)
  - Register, Get by ID, Search, Update Profile, Change Password, Activate/Deactivate, Delete
- **Customers** (6 endpoints)
  - Register Customer, Get by ID, Update Profile, Manage Favorites

### Service Catalog APIs (ServiceCatalog Bounded Context)

#### Location & Discovery
- **Locations** (6 endpoints)
  - Get Provinces, Cities, Hierarchy, Search
- **Providers** (12+ endpoints)
  - Create Draft, Complete Registration, Search, Get by Location, Upload Images
- **Provider Staff** (5 endpoints)
  - Add, Update, Remove Staff, Upload Photos
- **Provider Gallery** (6 endpoints)
  - Upload, Update, Reorder, Set Primary, Delete Images

#### Services & Bookings
- **Services** (10 endpoints)
  - Add, Update, Delete, Search, Activate/Deactivate, Get Popular
- **Bookings** (13 endpoints)
  - Create, Confirm, Cancel, Reschedule, Complete, Mark No-Show, Search
- **Availability** (3 endpoints)
  - Get Available Slots, Check Slot, Get Available Dates

#### Provider Management
- **Provider Settings** (20+ endpoints)
  - Business Info, Location, Working Hours, Business Hours, Holidays, Exceptions, Services
- **Notification Preferences** (8 endpoints)
  - Get/Update Preferences, Enable/Disable Email/SMS
- **Notifications** (6 endpoints)
  - Get All, Mark as Read, Delete, Clear All
- **Provider Registration** (9 endpoints)
  - Complete multi-step registration flow

#### Financial
- **Payments** (11 endpoints)
  - Process Payment, ZarinPal Integration, Refunds, Calculate Pricing, Revenue Reports
- **Financial** (3 endpoints)
  - Provider Earnings (Current/Previous Month)
- **Payouts** (5 endpoints)
  - Create, Execute, Get Pending/Completed

## Iranian Cultural Context

All sample data uses authentic Iranian details:

### Names (Persian)
- **Male**: رضا احمدی, محمد کریمی, علی حسینی
- **Female**: فاطمه محمدی, زهرا حسینی, مریم کریمی, نگار کریمی

### Contact Information
- **Phone Numbers**: +98 912 345 6789, 09123456789 (Iranian mobile format)
- **City Codes**: Tehran (021), Isfahan (031), Shiraz (071)
- **National Codes**: 10-digit Iranian national ID format

### Addresses
- **Tehran**: خیابان ولیعصر، نرسیده به میدان ونک
- **Isfahan**: خیابان چهارباغ عباسی، کوچه گلستان
- **Shiraz**: Various Persian street addresses
- **Postal Codes**: 10-digit Iranian postal codes (e.g., 1991945311)

### Location Coordinates
- **Tehran Center**: 35.7575°N, 51.4184°E
- **Isfahan**: 32.6546°N, 51.6680°E
- **Tajrish**: 35.8043°N, 51.5309°E

### Financial
- **Currency**: IRR (Iranian Rial)
- **Service Prices**:
  - Haircut: 500,000 IRR
  - Color & Highlights: 2,500,000 - 3,500,000 IRR
  - Manicure & Pedicure: 800,000 IRR
- **Tax**: 9% (Iranian VAT)
- **Bank Details**: SHABA numbers, Iranian bank names (بانک ملی ایران)

### Business Details
- **Business Names**: آرایشگاه زیبای تهران, سالن زیبایی نگار, آرایشگاه زیبای شیراز
- **Service Descriptions**: All in Persian
- **Working Hours**: Saturday-Thursday (Friday closed - Iranian weekend)
- **Holidays**: عید نوروز (Nowruz) and other Iranian holidays

### Payment Gateways
- **ZarinPal**: Iranian payment gateway integration
- Jalali calendar dates (1404/08/24)

## Installation & Setup

### 1. Import Collection

#### Option A: Postman Desktop/Web
1. Open Postman
2. Click **Import** button
3. Select `Booksy_API_Collection.postman_collection.json`
4. Collection will appear in your workspace

#### Option B: Using Postman CLI
```bash
postman collection import Booksy_API_Collection.postman_collection.json
```

### 2. Import Environment

1. Click **Environments** in Postman
2. Click **Import**
3. Select `Booksy_API.postman_environment.json`
4. Select **Booksy API - Local Development** environment

### 3. Configure Base URLs

Update environment variables based on your setup:

```json
{
  "baseUrl": "http://localhost:5000",          // UserManagement API
  "servicecat_baseUrl": "http://localhost:5001" // ServiceCatalog API
}
```

For production/staging:
```json
{
  "baseUrl": "https://api.booksy.ir",
  "servicecat_baseUrl": "https://catalog.booksy.ir"
}
```

## Usage Guide

### Authentication Flow

1. **Register User**
   ```
   POST {{baseUrl}}/api/v1/users
   ```
   - Creates new user account
   - Returns userId (saved to environment)

2. **Login**
   ```
   POST {{baseUrl}}/api/v1/auth/login
   ```
   - Automatically saves `accessToken` and `refreshToken` to environment
   - Token automatically used in subsequent requests

3. **Phone Verification (Alternative)**
   ```
   POST {{baseUrl}}/api/v1/auth/send-verification-code
   POST {{baseUrl}}/api/v1/auth/verify-code
   ```
   - Passwordless authentication with OTP

### Provider Registration Flow

Complete multi-step provider onboarding:

```
1. POST /provider-registration/start
2. POST /provider-registration/business-details
3. POST /provider-registration/location
4. POST /provider-registration/working-hours
5. POST /provider-registration/services
6. POST /provider-registration/documents (multipart/form-data)
7. POST /provider-registration/bank-details
8. GET  /provider-registration/review
9. POST /provider-registration/submit
```

### Booking Flow

```
1. GET  /availability/slots              # Check available times
2. POST /bookings                        # Create booking
3. POST /bookings/{id}/confirm           # Confirm with payment
4. POST /payments                        # Process payment
5. GET  /bookings/{id}                   # Get booking details
```

### Payment with ZarinPal (Iranian Gateway)

```
1. POST /payments/zarinpal/create        # Initiate payment
   Response: { authority, paymentUrl }

2. Redirect user to paymentUrl

3. POST /payments/zarinpal/verify        # Verify after redirect
   Body: { authority }
```

## Environment Variables

### Auto-populated (by test scripts)
- `accessToken` - JWT access token (auto-set on login)
- `refreshToken` - Refresh token (auto-set on login)
- `userId` - Current user ID
- `providerId` - Current provider ID
- `customerId` - Current customer ID
- `bookingId` - Last created booking ID
- `serviceId` - Last created service ID
- `verificationId` - Phone verification session ID

### Manual Configuration
- `baseUrl` - UserManagement API base URL
- `servicecat_baseUrl` - ServiceCatalog API base URL

## Collection Features

### Automatic Token Management
Login and phone verification endpoints automatically save tokens:
```javascript
// Post-response script (already included)
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.collectionVariables.set('accessToken', jsonData.accessToken);
    pm.collectionVariables.set('refreshToken', jsonData.refreshToken);
}
```

### Bearer Token Authentication
Collection-level auth automatically adds bearer token to all requests:
```
Authorization: Bearer {{accessToken}}
```

### ID Extraction
Create operations automatically save IDs for subsequent requests:
```javascript
// Example: Create Provider
if (pm.response.code === 201) {
    pm.collectionVariables.set('providerId', jsonData.providerId);
}
```

## Sample Data Examples

### Register Iranian Customer
```json
{
  "email": "fateme.mohammadi@example.com",
  "password": "SecurePass@123",
  "firstName": "فاطمه",
  "lastName": "محمدی",
  "phoneNumber": "09351234567",
  "nationalCode": "0012345678",
  "address": {
    "street": "خیابان آزادی",
    "city": "تهران",
    "province": "تهران",
    "postalCode": "1234567890",
    "country": "ایران"
  }
}
```

### Create Persian Service
```json
{
  "serviceName": "رنگ و هایلایت مو",
  "description": "رنگ کامل مو با رنگ‌های طبیعی و هایلایت",
  "durationHours": 2,
  "duration": 30,
  "basePrice": 2500000,
  "currency": "IRR",
  "category": "رنگ مو",
  "isMobileService": false
}
```

### Iranian Working Hours
```json
{
  "Saturday": {"isOpen": true, "openTime": "09:00", "closeTime": "18:00"},
  "Sunday": {"isOpen": true, "openTime": "09:00", "closeTime": "18:00"},
  "Monday": {"isOpen": true, "openTime": "09:00", "closeTime": "18:00"},
  "Tuesday": {"isOpen": true, "openTime": "09:00", "closeTime": "18:00"},
  "Wednesday": {"isOpen": true, "openTime": "09:00", "closeTime": "18:00"},
  "Thursday": {"isOpen": true, "openTime": "09:00", "closeTime": "21:00"},
  "Friday": {"isOpen": false}
}
```

## API Organization

### Folder Structure
```
Booksy API Collection
├── User Management
│   ├── Authentication (4)
│   ├── Phone Verification (5)
│   ├── Users (9)
│   └── Customers (6)
└── Service Catalog
    ├── Locations (6)
    ├── Providers (12)
    ├── Provider Staff (5)
    ├── Provider Gallery (6)
    ├── Services (10)
    ├── Bookings (13)
    ├── Availability (3)
    ├── Provider Settings (20)
    │   ├── Business Info (2)
    │   ├── Location (2)
    │   ├── Working Hours (2)
    │   ├── Business Hours (2)
    │   ├── Holidays (3)
    │   ├── Exceptions (3)
    │   ├── Availability (1)
    │   └── Provider Services (4)
    ├── Notification Preferences (8)
    ├── Notifications (6)
    ├── Provider Registration (9)
    ├── Payments (11)
    ├── Financial (3)
    └── Payouts (5)
```

## Testing Scenarios

### Scenario 1: Complete Provider Onboarding
1. Register user → `POST /users`
2. Login → `POST /auth/login`
3. Start registration → `POST /provider-registration/start`
4. Complete all registration steps
5. Submit for approval → `POST /provider-registration/submit`

### Scenario 2: Customer Books Service
1. Login as customer
2. Search providers → `GET /providers/search?city=تهران`
3. View services → `GET /services/provider/{id}`
4. Check availability → `GET /availability/slots`
5. Create booking → `POST /bookings`
6. Process payment → `POST /payments/zarinpal/create`
7. Verify payment → `POST /payments/zarinpal/verify`

### Scenario 3: Provider Manages Schedule
1. Login as provider
2. Update working hours → `PUT /providers/{id}/working-hours`
3. Add holiday (Nowruz) → `POST /providers/{id}/holidays`
4. Add exception → `POST /providers/{id}/exceptions`
5. View availability → `GET /providers/{id}/availability?date=2025-11-20`

## Troubleshooting

### Token Expired
```json
{
  "error": "Token expired"
}
```
**Solution**: Call `POST /auth/refresh` with refreshToken

### Invalid Phone Format
```json
{
  "error": "Invalid phone number format"
}
```
**Solution**: Use Iranian format: `09123456789` or `+989123456789`

### CORS Issues (Web)
Add to API server configuration:
```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowPostman",
        builder => builder.AllowAnyOrigin()
                         .AllowAnyMethod()
                         .AllowAnyHeader());
});
```

## Persian Text Support

All Persian text in request bodies is UTF-8 encoded. Ensure your API:
- Accepts `Content-Type: application/json; charset=utf-8`
- Database supports Persian characters (UTF-8 collation)
- Responses include proper charset headers

## Additional Resources

### Iranian Date Formats
- **Jalali Calendar**: 1404/08/24 (used in some payment descriptions)
- **Gregorian**: 2025-11-15 (used in API requests)

### Common Iranian Provinces
- تهران (Tehran) - ID: 1
- اصفهان (Isfahan) - ID: 3
- شیراز (Shiraz) - ID: 7
- مشهد (Mashhad) - ID: 9

### Working with Files
For file uploads (gallery images, documents):
1. Select request
2. Body → form-data
3. Change type to "File"
4. Select file from disk

## Support

For API documentation and support:
- Swagger UI: `http://localhost:5000/swagger` (UserManagement)
- Swagger UI: `http://localhost:5001/swagger` (ServiceCatalog)
- GitHub Issues: Report bugs or request features

## Version

- **Collection Version**: 1.0
- **API Version**: v1
- **Last Updated**: 2025-11-09
- **Total Endpoints**: 120+
- **Cultural Context**: Iranian/Persian

---

**Note**: This collection is designed for testing and development purposes. Ensure all sensitive data (tokens, passwords, etc.) is handled securely and never committed to version control.
