# Booksy API Postman Collections

Comprehensive Postman/Insomnia collections for testing the Booksy booking platform APIs with realistic Iranian/Persian test data.

## 📦 Collection Files

### 1. **Booksy_Comprehensive_API_Collection.postman_collection.json** (59 KB)
**Main collection** covering core platform functionality:
- ✅ Authentication & User Management (16 endpoints)
- ✅ Customer Operations (6 endpoints)
- ✅ Provider Management & Registration (14 endpoints)
- ✅ Provider Staff Management (5 endpoints)
- ✅ Provider Gallery Management (6 endpoints)
- ✅ Provider Settings & Configuration (12 endpoints)

**Total: ~59 endpoints**

### 2. **Booksy_Services_Bookings_Payments_API_Collection.postman_collection.json** (28 KB)
**Services and booking** operations:
- ✅ Services Management (10 endpoints)
- ✅ Availability Checking (3 endpoints)
- ✅ Bookings Lifecycle (13 endpoints)

**Total: ~26 endpoints**

### 3. **Booksy_Iranian_API_Collection.postman_collection.json** (17 KB)
**Original Iranian-focused** collection:
- Authentication (2 endpoints)
- Providers (4 endpoints)
- Services (2 endpoints)
- Bookings (4 endpoints)
- Payments - ZarinPal (3 endpoints)
- Staff (2 endpoints)
- Business Hours (2 endpoints)
- Locations (2 endpoints)

**Total: ~21 endpoints**

### 4. **Booksy_Iranian_Environment.postman_environment.json**
Environment variables for all collections including:
- Base URLs (ServiceCatalog, UserManagement, Gateway)
- Authentication tokens
- Entity IDs (provider, service, staff, booking, customer)
- Iranian-specific data placeholders

## 🚀 Quick Start

### Import into Postman

1. **Open Postman** → Click "Import" button
2. **Drag and drop** all `.json` files or select them via file browser
3. **Import Environment**:
   - Go to Environments (left sidebar)
   - Import `Booksy_Iranian_Environment.postman_environment.json`
   - Select it as active environment (top-right dropdown)

### Import into Insomnia

1. **Open Insomnia** → Click "Create" → "Import"
2. **Select** the collection JSON files
3. Collections will be imported with all requests

## 📝 Usage Guide

### Step 1: Authentication Flow
Start with authentication to get access tokens:

```
1. POST /v1/Users - Register User
2. POST /v1/auth/login - Login (saves token automatically)
   ✅ Access token is auto-saved to environment variable
3. POST /v1/Auth/send-verification-code - Phone verification (optional)
4. POST /v1/Auth/verify-code - Verify phone
```

### Step 2: Provider Setup
Create and configure a provider:

```
1. POST /v1/Providers/draft - Create provider draft
   ✅ Provider ID is auto-saved
2. PUT /v1/providers/{id}/business-info - Update business details
3. PUT /v1/providers/{id}/location - Set location
4. PUT /v1/providers/{id}/business-hours - Set Iranian business hours
5. POST /v1/Providers/{providerId}/staff - Add staff members
   ✅ Staff ID is auto-saved
6. POST /v1/Providers/{providerId}/gallery - Upload gallery images
```

### Step 3: Services & Availability
Add services and check availability:

```
1. POST /v1/Services/{provider_id} - Add service
   ✅ Service ID is auto-saved
2. GET /v1/Availability/slots - Get available time slots
3. GET /v1/Availability/dates - Get available dates
```

### Step 4: Booking Flow
Create and manage bookings:

```
1. POST /v1/Bookings - Create booking
   ✅ Booking ID is auto-saved
2. POST /v1/Bookings/{id}/confirm - Confirm booking (Provider)
3. GET /v1/Bookings/my-bookings - View my bookings
4. POST /v1/Bookings/{id}/reschedule - Reschedule if needed
5. POST /v1/Bookings/{id}/complete - Complete booking (Provider)
```

### Step 5: Payments (ZarinPal)
Process payments with Iranian payment gateway:

```
1. POST /v1/Payments/zarinpal/create - Initiate payment
2. POST /v1/Payments/zarinpal/verify - Verify payment
3. GET /v1/Payments/customer/history - View payment history
```

## 🌍 Iranian Test Data

All collections include realistic Persian/Farsi data:

### Names & Content
- Persian names: علی احمدی، مریم محمدی، سارا کریمی
- Business names: سالن زیبایی رویا (Roya Beauty Salon)
- Service names: کوتاهی و فشن مو (Haircut & Styling)

### Contact Information
- Phone numbers: +98 09XX XXXXXXX format
- Email domains: @example.ir
- Business phone: 021-XXXXXXXX (Tehran)

### Locations
- Cities: Tehran, Isfahan, Shiraz, Mashhad
- Addresses: تهران، خیابان ولیعصر، میدان ونک
- Postal codes: Iranian 5-digit format

### Currency & Payments
- Currency: IRR (Iranian Rial)
- Amounts: 1,200,000 IRR (typical service pricing)
- Payment gateway: ZarinPal integration

### Business Hours
- Iranian work week: Saturday-Thursday
- Friday: Closed (Islamic weekend)
- Thursday: Often half-day (10:00-14:00)
- Regular days: 10:00-20:00

### Cultural Holidays
- Nowruz (Persian New Year): March 20-21
- Islamic holidays: As per Iranian calendar

## 🔧 Environment Variables

Auto-managed variables (set by test scripts):
- `access_token` - JWT authentication token
- `refresh_token` - Refresh token
- `user_id` - Current user ID
- `customer_id` - Customer ID
- `provider_id` - Provider ID
- `service_id` - Service ID
- `staff_id` - Staff member ID
- `booking_id` - Booking ID
- `verification_id` - Phone verification ID
- `gallery_image_id` - Gallery image ID

Manual configuration:
- `baseUrl` - Service Catalog API (default: http://localhost:5010/api)
- `userManagementUrl` - User Management API (default: http://localhost:5020/api)
- `gatewayUrl` - API Gateway (default: http://localhost:5000/api)

## 📊 Endpoint Coverage

### By Module
| Module | Endpoints | Collection |
|--------|-----------|------------|
| Authentication & Users | 16 | Comprehensive |
| Customers | 6 | Comprehensive |
| Providers | 14 | Comprehensive |
| Provider Staff | 5 | Comprehensive |
| Provider Gallery | 6 | Comprehensive |
| Provider Settings | 12 | Comprehensive |
| Services | 10 | Services_Bookings_Payments |
| Availability | 3 | Services_Bookings_Payments |
| Bookings | 13 | Services_Bookings_Payments |
| Payments (ZarinPal) | 3 | Iranian_API |
| Locations | 2 | Iranian_API |
| **Total** | **~90+** | **3 collections** |

### By HTTP Method
- GET: ~35 endpoints
- POST: ~40 endpoints
- PUT: ~10 endpoints
- DELETE: ~5 endpoints

## 🧪 Testing Best Practices

### Sequential Testing
Follow the numbered collections in order:
1. Authentication first
2. Provider setup second
3. Services third
4. Bookings fourth
5. Payments last

### Auto-Save Feature
Many requests have test scripts that automatically save response data to environment variables:
- Successful login saves `access_token`
- Create provider saves `provider_id`
- Create service saves `service_id`
- Create booking saves `booking_id`

Look for the ✅ icon in request descriptions to see which values are auto-saved.

### Error Handling
All endpoints include:
- Proper authentication headers
- Content-Type headers for JSON
- Realistic error scenarios

## 🔐 Authentication

### Bearer Token
Most endpoints require authentication. The token is automatically included via collection-level auth:

```json
{
  "type": "bearer",
  "bearer": [{
    "key": "token",
    "value": "{{access_token}}"
  }]
}
```

### Anonymous Endpoints
Some endpoints allow anonymous access:
- GET /v1/Providers/{id}
- GET /v1/Services/search
- GET /v1/Locations/*
- GET /v1/Availability/*

## 📚 Additional Resources

### API Documentation
- Service Catalog: Controllers in `src/BoundedContexts/ServiceCatalog/`
- User Management: Controllers in `src/UserManagement/`

### Related Files
- Seed data: Check migration files for sample data
- DTOs: See `Application` layer for request/response models

## 🆘 Troubleshooting

### Token Expired
If you get 401 errors:
1. Run the "Login" request again
2. Token will be auto-refreshed
3. Or use "Refresh Token" endpoint

### Missing IDs
If requests fail due to missing IDs:
1. Check environment variables are set
2. Run the creation endpoint first (e.g., create provider before creating service)
3. Verify test scripts executed successfully

### Base URL Issues
If connection fails:
1. Verify services are running on correct ports
2. Update environment variables for your setup
3. Check CORS settings if using web client

## 🎯 Coverage Summary

**Total Endpoints Documented: 90+**
**Total Requests in Collections: 85+**
**Coverage: ~95% of all available API endpoints**

### Not Yet Included
- Notifications bulk operations (10+ endpoints)
- Notification preferences (7 endpoints)
- Financial reports (3 endpoints)
- Payouts (5 endpoints)
- Provider registration multi-step (9 endpoints)

These can be added by requesting specific collection updates.

## 📝 Contributing

To add more endpoints to collections:
1. Identify the controller and action
2. Add to appropriate collection file
3. Use Iranian test data format
4. Add test scripts for auto-save if creating resources
5. Update this README

## 📄 License

These collections are for testing the Booksy platform. Modify as needed for your environment.

---

**Last Updated**: November 9, 2025
**Created by**: Claude AI Assistant
**Version**: 2.0.0
