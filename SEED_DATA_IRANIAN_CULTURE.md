# Iranian Culture Seed Data Documentation

## Overview
This document describes the comprehensive seed data implementation with Iranian/Persian culture for the Booksy application.

## Architecture

### Seeder Pattern
All seeders implement the `ISeeder` interface with a `SeedAsync` method. The seeders are organized in separate files for better maintainability and follow the Single Responsibility Principle.

### Orchestrator Pattern
Two main orchestrators coordinate the seeding process:
- **ServiceCatalogDatabaseSeederOrchestrator**: Manages ServiceCatalog bounded context seeding
- **UserManagementDatabaseSeederOrchestrator**: Manages UserManagement bounded context seeding

## Seed Data Files

### ServiceCatalog Bounded Context

### 1. ProviderSeeder.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds Iranian beauty salons, spas, clinics, and fitness centers across major Iranian cities.

**Data Includes**:
- 20 providers across cities: Tehran, Mashhad, Isfahan, Shiraz, Tabriz, Karaj, Qom, Ahvaz, Kerman, Rasht
- Provider types: Salon, Spa, Clinic, Barbershop, GymFitness, Professional
- Persian and English names (e.g., "آرایشگاه زیبای پارسی - Arayeshgah Ziba Parsi")
- Iranian contact information (phone numbers, .ir email domains)
- Iranian addresses with city districts

### 2. StaffSeeder.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds staff members with authentic Persian names.

**Data Includes**:
- Persian first names (male): علی، رضا، محمد، حسین، امیر، مهدی، etc.
- Persian first names (female): فاطمه، زهرا، مریم، سارا، نگار، لیلا، etc.
- Persian last names: احمدی، محمدی، حسینی، رضایی، کریمی، etc.
- Iranian mobile phone numbers (09XXXXXXXXX format)
- Staff assigned based on provider type (Owner, ServiceProvider, Specialist, Receptionist, etc.)

### 3. ServiceSeeder.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds beauty and wellness services with Persian descriptions and Iranian Rial (IRR) pricing.

**Service Categories by Provider Type**:

#### Individual (Barbershop)
- کوتاهی مو مردانه (Men's Haircut) - 500,000 IRR
- اصلاح صورت (Face Shave) - 300,000 IRR
- اصلاح ریش (Beard Trim) - 350,000 IRR
- رنگ مو (Hair Color) - 800,000 IRR

#### Salon
- کوتاهی و فشن (Cut & Style) - 1,200,000 IRR
- رنگ مو (Hair Coloring) - 2,500,000 IRR
- هایلایت (Highlights) - 2,000,000 IRR
- کراتینه مو (Keratin Treatment) - 3,500,000 IRR
- آرایش عروس (Bridal Makeup) - 5,000,000 IRR
- مانیکور، پدیکور، اپیلاسیون

#### Spa
- ماساژ سوئدی (Swedish Massage) - 1,800,000 IRR
- ماساژ بافت عمقی (Deep Tissue Massage) - 2,200,000 IRR
- ماساژ سنگ داغ (Hot Stone Massage) - 2,500,000 IRR
- پاکسازی پوست (Facial Treatment) - 1,500,000 IRR
- ماسک طلا (Gold Facial) - 3,500,000 IRR

#### Clinic
- لیزر موهای زائد (Laser Hair Removal)
- مزوتراپی مو/صورت (Mesotherapy)
- تزریق بوتاکس (Botox Injection)
- تزریق فیلر (Filler Injection)
- پی آر پی (PRP Therapy)

#### Medical
- کاشت مو (Hair Transplant)
- عمل زیبایی بینی (Rhinoplasty)
- لیفت صورت (Face Lift)

#### GymFitness
- تمرین شخصی (Personal Training)
- یوگا، پیلاتس، زومبا
- ماساژ ورزشی (Sports Massage)

#### Professional
- ماساژ فیزیوتراپی (Physiotherapy)
- طب سوزنی (Acupuncture)
- طب سنتی ایرانی (Persian Traditional Medicine)
- حجامت (Cupping Therapy)

### 4. CustomerSeeder.cs
**Location**: `src/UserManagement/Booksy.UserManagement.Infrastructure/Persistence/Seeders/`

**Description**: Seeds 50 Iranian customer users with Persian names.

**Data Includes**:
- Persian male and female names
- Transliterated email addresses (e.g., ali.ahmadi1@example.ir)
- Iranian mobile phone numbers
- Age range: 18-65 years
- Gender-appropriate names
- Active user status for testing

### 5. BusinessHoursSeeder.cs ⭐ NEW
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds business operating hours with Iranian business culture.

**Features**:
- **Iranian Weekend**: Friday is weekend (closed), some businesses Thursday half-day
- **Business Patterns**:
  - **Standard Salon**: Saturday-Wednesday 10:00-20:00, Thursday 10:00-14:00, Friday closed
  - **Spa**: Saturday-Thursday 9:00-21:00, Friday closed
  - **Clinic**: Saturday-Thursday 8:00-18:00, Thursday 8:00-13:00, Friday closed
  - **Gym/Fitness**: Saturday-Thursday 6:00-22:00, Friday 8:00-14:00 (short hours)
  - **Barbershop**: Saturday-Thursday 9:00-21:00, Friday closed
- **Cultural Authenticity**: Reflects Iranian work week (Saturday-Thursday with Friday as Islamic weekend)
- Break periods support (lunch breaks, prayer times)

### 6. ServiceOptionSeeder.cs ⭐ NEW
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds service add-ons/options with Persian names and pricing.

**Features**:
- **Hair Services**: شستشوی مو (Hair Wash), ماساژ سر (Head Massage), سشوار (Blow Dry)
- **Color Services**: ترمیم مو (Hair Treatment), کراتینه (Keratin)
- **Massage Add-ons**: آروماتراپی (Aromatherapy), سنگ داغ (Hot Stone)
- **Facial Add-ons**: ماسک طلا (Gold Mask), میکرودرم (Microdermabrasion)
- **Nail Services**: ژل (Gel Polish), طراحی ناخن (Nail Art), اکستنشن ناخن (Nail Extension)
- **Makeup Add-ons**: مژه مصنوعی (False Lashes), کانتور حرفه‌ای (Professional Contouring)
- **Laser Add-ons**: ناحیه اضافی (Extra Area), کرم بیحس کننده (Numbing Cream)
- **Barbershop Add-ons**: حوله داغ (Hot Towel), ماساژ صورت (Face Massage)
- Realistic Iranian Rial (IRR) pricing
- Additional duration where applicable

### 7. BookingSeeder.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds 20-35 bookings per provider with various statuses and scenarios.

**Features**:
- Bookings across past (60 days), present, and future (90 days)
- Various booking statuses: Requested, Confirmed, Completed, Cancelled, NoShow
- Persian customer notes: "لطفا دقیق باشید"، "اولین باره که میام"، etc.
- Persian staff notes: "خدمات با موفقیت انجام شد"، "مشتری بسیار راضی بود"
- Realistic payment scenarios (deposits, full payments, refunds)
- Persian cancellation reasons

### 8. PaymentSeeder.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds payment records for bookings with Iranian payment providers.

**Features**:
- **ZarinPal Integration**: 80% of payments use ZarinPal (Iran's leading payment gateway)
- **Authentic Iranian Card Numbers**: Uses real Iranian bank BIN prefixes
  - 603799 (Bank Melli Iran)
  - 627353 (Bank Tejarat)
  - 627961 (Bank Sanat va Maadan)
  - 639607 (Bank Eghtesad Novin)
- **ZarinPal Authority Codes**: Generates realistic authority codes (A + 35 chars)
- **ZarinPal Reference Numbers**: Simulates verified transactions
- **Payment Methods**: OnlinePayment (80%), Card (15%), Cash (5%)
- **Payment Statuses**: Pending, Authorized, Captured, Failed, Refunded
- **Persian Descriptions**: "پرداخت برای رزرو شماره..."
- **Iranian Rial (IRR)** currency

### 9. PayoutSeeder.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds provider payout records with Iranian bank information.

**Features**:
- **Monthly Payouts**: Creates payouts for last 3 months per provider
- **Iranian Banks**: Authentic Iranian bank names
  - بانک ملی ایران (Bank Melli Iran)
  - بانک تجارت (Bank Tejarat)
  - بانک صنعت و معدن (Bank Sanat va Maadan)
  - بانک اقتصاد نوین (Bank Eghtesad Novin)
  - بانک پاسارگاد (Bank Pasargad)
  - And 10+ more Iranian banks
- **Commission Calculation**: 10-15% commission rate
- **Payout Statuses**: Pending, Scheduled, Paid, Failed, Cancelled
- **Persian Notes**: "تسویه حساب دوره..."
- **Period-based Status**: Older payouts more likely to be paid
- **Iranian Bank Account Details**: Last 4 digits + bank name

### 10. UserNotificationPreferencesSeeder.cs ⭐ NEW
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds notification preferences for all customers.

**Features**:
- Creates default notification preferences for each customer
- Extracts customer IDs from existing bookings
- Default settings include all notification channels (Email, SMS, Push)
- Default notification types (Booking confirmations, reminders, cancellations)
- Quiet hours support for Iranian time zone

### 11. ProvinceCitiesSeeder.cs
**Location**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Seeders/`

**Description**: Seeds Iranian provinces and cities from JSON file.

**Dependencies**: Requires `ProvinceCity-ParentChild.json` file in the project root.

### UserManagement Bounded Context

## Orchestrators

### ServiceCatalogDatabaseSeederOrchestrator
**Execution Order**:
1. ProvinceCitiesSeeder (independent)
2. ProviderSeeder (independent)
3. StaffSeeder (depends on Providers)
4. BusinessHoursSeeder (depends on Providers) ⭐ NEW
5. ServiceSeeder (depends on Providers)
6. ServiceOptionSeeder (depends on Services) ⭐ NEW
7. NotificationTemplateSeeder (independent)
8. BookingSeeder (depends on Providers, Staff, Services)
9. PaymentSeeder (depends on Bookings)
10. PayoutSeeder (depends on Payments)
11. UserNotificationPreferencesSeeder (depends on Bookings) ⭐ NEW

### UserManagementDatabaseSeederOrchestrator
**Execution Order**:
1. CustomerSeeder

## Integration

### Dependency Injection
Updated in:
- `ServiceCatalogInfrastructureExtensions.cs` - Line 65
- `UserManagementInfrastructureExtensions.cs` - Line 82

```csharp
services.AddScoped<ISeeder, ServiceCatalogDatabaseSeederOrchestrator>();
services.AddScoped<ISeeder, UserManagementDatabaseSeederOrchestrator>();
```

### Automatic Seeding
Seeding runs automatically in **Development** environment on application startup via:
- `ServiceCatalog.Api/Startup.cs` - Configure method (lines 206-211)
- `UserManagement.API/Program.cs` - (lines 198-202)

## Running the Seeders

### Automatic (Development Only)
Seeders run automatically when starting the application in Development mode.

### Manual Execution
```csharp
using var scope = app.Services.CreateScope();
await scope.ServiceProvider.InitializeDatabaseAsync();
```

## Data Statistics

### Expected Seed Data Counts
- **Providers**: 20 Iranian beauty/wellness businesses
- **Staff**: 60-80 staff members (3-5 per provider)
- **BusinessHours**: 140-160 business hours records (7 days × 20 providers) ⭐ NEW
- **Services**: 100-200 services (5-10 per provider)
- **ServiceOptions**: 200-400 service add-ons (2-4 per service) ⭐ NEW
- **Customers**: 50 Iranian users
- **Bookings**: 400-700 bookings (20-35 per provider)
- **Payments**: 300-600 payments (linked to completed/confirmed bookings)
- **Payouts**: 40-60 monthly payouts (2-3 months × ~20 providers)
- **UserNotificationPreferences**: 30-50 preference records (one per unique customer) ⭐ NEW
- **Provinces/Cities**: Based on JSON file

## Persian Language Support

All seed data includes Persian text in UTF-8 encoding:
- Provider names and descriptions
- Service names and categories
- Staff first and last names
- Customer notes and staff notes
- Cancellation reasons

## Iranian Cultural Elements

### Names
- Authentic Persian first names (علی، رضا، فاطمه، زهرا)
- Common Iranian family names (احمدی، محمدی، حسینی)

### Locations
- Major Iranian cities: Tehran, Mashhad, Isfahan, Shiraz, Tabriz, Karaj, Qom, Ahvaz, Kerman, Rasht
- City districts (Vanak, Jordan, Elahieh, etc.)

### Services
- Traditional Persian beauty services
- Iranian massage techniques
- Persian traditional medicine
- Common Iranian beauty treatments

### Pricing
- Iranian Rial (IRR) currency
- Realistic Iranian market prices (500,000 - 30,000,000 IRR)

### Contact Information
- .ir domain emails
- 09XX mobile phone format
- Tehran area codes (021)

## Files Created

### New Seeder Files

**ServiceCatalog:**
1. `/src/BoundedContexts/ServiceCatalog/.../ProviderSeeder.cs`
2. `/src/BoundedContexts/ServiceCatalog/.../StaffSeeder.cs`
3. `/src/BoundedContexts/ServiceCatalog/.../BusinessHoursSeeder.cs` ⭐ NEW
4. `/src/BoundedContexts/ServiceCatalog/.../ServiceSeeder.cs`
5. `/src/BoundedContexts/ServiceCatalog/.../ServiceOptionSeeder.cs` ⭐ NEW
6. `/src/BoundedContexts/ServiceCatalog/.../BookingSeeder.cs`
7. `/src/BoundedContexts/ServiceCatalog/.../PaymentSeeder.cs`
8. `/src/BoundedContexts/ServiceCatalog/.../PayoutSeeder.cs`
9. `/src/BoundedContexts/ServiceCatalog/.../UserNotificationPreferencesSeeder.cs` ⭐ NEW
10. `/src/BoundedContexts/ServiceCatalog/.../ProvinceCitiesSeeder.cs`
11. `/src/BoundedContexts/ServiceCatalog/.../ServiceCatalogDatabaseSeederOrchestrator.cs`

**UserManagement:**
12. `/src/UserManagement/.../CustomerSeeder.cs`
13. `/src/UserManagement/.../UserManagementDatabaseSeederOrchestrator.cs`

### Modified Files
1. `/src/BoundedContexts/ServiceCatalog/.../ServiceCatalogDatabaseSeederOrchestrator.cs` - Added 3 new seeders
2. `/src/BoundedContexts/ServiceCatalog/.../ServiceCatalogInfrastructureExtensions.cs`
3. `/src/UserManagement/.../UserManagementInfrastructureExtensions.cs`

## Benefits

1. **Separation of Concerns**: Each entity has its own seeder file
2. **Maintainability**: Easy to update or add new seed data
3. **Testability**: Can test each seeder independently
4. **Reusability**: Orchestrator pattern allows flexible composition
5. **Cultural Authenticity**: Genuine Iranian names, places, and business practices
6. **Comprehensive**: Covers all main entities with realistic data
7. **Extensibility**: Easy to add more seeders to the orchestrators

## Notes

- All seeders check if data already exists before seeding (idempotent)
- Deterministic random seeds ensure consistent test data
- Persian text uses UTF-8 encoding
- Prices are in Iranian Rial (IRR)
- Phone numbers follow Iranian mobile format (09XXXXXXXXX)
- Email domains use .ir extension
