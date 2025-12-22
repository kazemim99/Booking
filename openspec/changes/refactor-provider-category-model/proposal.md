# Refactor Provider Category Model

## Why

The current domain model has **confusing and overlapping concepts** that make it difficult for both developers and users to understand the system:

### Problem 1: Three Overlapping Concepts
1. **ProviderType** (enum): Individual, Clinic, Salon, Spa, GymFitness, Educational, Medical, Automotive, HomeServices, PetServices, Professional
2. **ServiceCategory** (value object): Beauty, Makeup, HairCare, SkinCare, Massage, Fitness
3. **ProviderHierarchyType** (enum): Organization, Individual

These three concepts create semantic confusion:
- `ProviderType.GymFitness` vs `ServiceCategory.Fitness` - identical meaning, different types
- `ProviderType.Salon` vs `ServiceCategory.HairCare` - closely related but separate
- `ProviderType.Individual` vs `ProviderHierarchyType.Individual` - duplicate concepts

### Problem 2: Provider Has No Category
The `Provider` aggregate does NOT have a category/service-type property, even though:
- **Business requirement**: "Every provider can have only ONE category"
- **Frontend shows**: Category selection as the 2nd step of registration (hair_salon, barber, etc.)
- **Only Service entity has**: `ServiceCategory` property
- **Result**: No way to categorize or filter providers by what services they offer

### Problem 3: Frontend-Backend Mismatch
Frontend registration (`CategorySelectionStep.vue`):
```typescript
{ id: 'hair_salon', name: 'آریشگاه زنانه', icon: '💇‍♀️' }
{ id: 'barber', name: 'آرایشگاه مردانه', icon: '💇‍♂️' }
```

Backend domain:
- No `ProviderType.HairSalon` (only `Salon`)
- No `ServiceCategory.HairSalon` (only `HairCare`)
- **Result**: No clear mapping from frontend choices to backend domain

### Problem 4: Unclear Semantics
Users are confused:
- Is a solo barber `ProviderType.Individual` or `ProviderType.Salon`?
- What if a salon offers both HairCare AND Beauty services but can only have "one category"?
- Why does ProviderType mix business structure (Individual, Professional) with industry (Medical, Automotive)?

### Real-World Impact
- **Developers** struggle to understand which enum/value object to use when
- **Registration flow** has hardcoded category IDs (`hair_salon`) that don't match backend
- **Search/filtering** unclear - should we filter by ProviderType or infer from Service.Category?
- **Data integrity** - no enforcement that Provider's type aligns with their services

## What Changes

### Core Changes

1. **Remove ProviderType Enum Entirely**
   - Delete `Booksy.ServiceCatalog.Domain.Enums.ProviderType`
   - Already have `ProviderHierarchyType` (Organization vs Individual) for business structure
   - Industry/service categorization belongs in ServiceCategory

2. **Convert ServiceCategory from Value Object to Enum**
   - Simpler, more performant, database-friendly
   - Enum values match business domains: `HairSalon`, `BeautySalon`, `Barbershop`, `Spa`, `Clinic`, `Gym`, `MedicalClinic`, etc.
   - Each enum value has metadata (Persian name, icon, color, slug)

3. **Add PrimaryCategory to Provider Aggregate**
   ```csharp
   public sealed class Provider : AggregateRoot<ProviderId>
   {
       // Business structure (keeps existing HierarchyType)
       public ProviderHierarchyType HierarchyType { get; private set; }

       // NEW: Primary business category (REQUIRED)
       public ServiceCategory PrimaryCategory { get; private set; }

       // Services offered must align with PrimaryCategory
       public IReadOnlyList<Service> Services { get; }
   }
   ```

4. **Enforce Service Category Alignment**
   - Business rule: All services offered by a provider must match or relate to their `PrimaryCategory`
   - Domain validation prevents creating services outside the provider's category

5. **Update Frontend Registration Flow**
   - Category selection maps directly to backend `ServiceCategory` enum
   - Clear 1:1 mapping:
     - `hair_salon` (women's) → `ServiceCategory.HairSalon`
     - `barber` (men's) → `ServiceCategory.Barbershop`
     - `beauty` → `ServiceCategory.BeautySalon`
     - `spa` → `ServiceCategory.Spa`
     - `clinic` → `ServiceCategory.Clinic`

### ServiceCategory Enum Design

```csharp
public enum ServiceCategory
{
    // Beauty & Personal Care
    HairSalon = 1,        // آرایشگاه زنانه
    Barbershop = 2,       // آرایشگاه مردانه
    BeautySalon = 3,      // سالن زیبایی
    NailSalon = 4,        // آرایش ناخن
    Spa = 5,              // اسپا

    // Health & Wellness
    Massage = 6,          // ماساژ
    Gym = 7,              // باشگاه ورزشی
    Yoga = 8,             // یوگا و مدیتیشن

    // Medical
    MedicalClinic = 9,    // کلینیک پزشکی
    Dental = 10,          // دندانپزشکی
    Physiotherapy = 11,   // فیزیوتراپی

    // Professional Services
    Tutoring = 12,        // آموزش خصوصی
    Automotive = 13,      // تعمیرات خودرو
    HomeServices = 14,    // خدمات منزل
    PetCare = 15          // مراقبت حیوانات
}
```

### Breaking Changes

- **BREAKING**: Remove `Provider.ProviderType` property
- **BREAKING**: Add `Provider.PrimaryCategory` property (required, non-nullable)
- **BREAKING**: Convert `ServiceCategory` from value object to enum
- **BREAKING**: All existing providers must be assigned a `PrimaryCategory` during migration
- **BREAKING**: Service creation validates category alignment with provider
- **BREAKING**: Registration API changes category parameter from string to enum

### Non-Breaking Compatibility

- `ProviderHierarchyType` unchanged (Organization vs Individual)
- Service entity still has `ServiceCategory` (but now as enum, not value object)
- Booking flow unchanged
- Search/filtering improved (can now filter providers by category directly)

## Impact

### Affected Specs
- `provider-management` - Add PrimaryCategory property, remove ProviderType
- `provider-registration` - Update category selection to use ServiceCategory enum
- `service-management` - Enforce category alignment validation

### Affected Code

**Backend**:
- `Booksy.ServiceCatalog.Domain.Enums.ProviderType.cs` - **DELETE**
- `Booksy.ServiceCatalog.Domain.ValueObjects.ServiceCategory.cs` - **DELETE** (already commented out by user)
- `Booksy.ServiceCatalog.Domain.Enums.ServiceCategory.cs` - **CREATE NEW** enum
- `Booksy.ServiceCatalog.Domain.Aggregates.Provider.cs` - Add `PrimaryCategory`, remove `ProviderType`
- `Booksy.ServiceCatalog.Domain.Aggregates.Service.cs` - Change `ServiceCategory` from value object to enum
- `Booksy.ServiceCatalog.Infrastructure.Persistence.Configurations.ProviderConfiguration.cs` - Update EF mapping
- `Booksy.ServiceCatalog.Infrastructure.Persistence.Migrations` - **NEW** migration
- All repositories, DTOs, query handlers, controllers referencing `ProviderType`

**Frontend**:
- `booksy-frontend/src/modules/provider/components/registration/steps/CategorySelectionStep.vue` - Map to backend enum
- `booksy-frontend/src/modules/provider/types/provider.types.ts` - Update TypeScript types
- `booksy-frontend/src/modules/provider/services/provider.service.ts` - Update API calls
- `booksy-frontend/src/shared/types/categories.ts` - **CREATE NEW** category definitions

**Database**:
- **DROP** column: `providers.type` (ProviderType)
- **ADD** column: `providers.primary_category` (ServiceCategory enum as int)
- **MODIFY** column: `services.category` (convert from varchar to int enum)
- **MIGRATION**: Assign default categories to existing providers based on their services

### Migration Strategy

#### Phase 1: Data Analysis (Pre-Migration)
1. Query existing providers and their services
2. Infer appropriate `PrimaryCategory` based on:
   - Current `ProviderType` value
   - Most common `Service.Category` values
   - Business name keywords

#### Phase 2: Database Migration
```sql
-- Add new column (nullable initially)
ALTER TABLE providers ADD COLUMN primary_category INT NULL;

-- Migrate ProviderType → PrimaryCategory
UPDATE providers
SET primary_category = CASE
    WHEN type = 1 THEN 1  -- Individual → HairSalon (default, will refine)
    WHEN type = 2 THEN 9  -- Clinic → MedicalClinic
    WHEN type = 3 THEN 1  -- Salon → HairSalon
    WHEN type = 4 THEN 5  -- Spa → Spa
    WHEN type = 5 THEN 7  -- GymFitness → Gym
    WHEN type = 6 THEN 12 -- Educational → Tutoring
    WHEN type = 7 THEN 9  -- Medical → MedicalClinic
    WHEN type = 8 THEN 13 -- Automotive → Automotive
    WHEN type = 9 THEN 14 -- HomeServices → HomeServices
    WHEN type = 10 THEN 15 -- PetServices → PetCare
    WHEN type = 11 THEN 1  -- Professional → HairSalon (default)
    ELSE 1
END;

-- Refine based on service categories (more accurate)
UPDATE providers p
SET primary_category = (
    SELECT CASE s.category
        WHEN 'Beauty' THEN 3
        WHEN 'HairCare' THEN 1
        WHEN 'Massage' THEN 6
        WHEN 'Fitness' THEN 7
        ELSE p.primary_category
    END
    FROM services s
    WHERE s.provider_id = p.id
    GROUP BY s.category
    ORDER BY COUNT(*) DESC
    LIMIT 1
);

-- Make column required
ALTER TABLE providers ALTER COLUMN primary_category SET NOT NULL;

-- Drop old column
ALTER TABLE providers DROP COLUMN type;

-- Convert service.category from string to int
ALTER TABLE services ADD COLUMN category_temp INT;
UPDATE services SET category_temp = /* mapping logic */;
ALTER TABLE services DROP COLUMN category;
ALTER TABLE services RENAME COLUMN category_temp TO category;
```

#### Phase 3: Code Deployment
1. Deploy backend with new domain model
2. Update frontend to use new category enum
3. Test registration flow end-to-end
4. Monitor for errors

#### Phase 4: Data Cleanup
1. Manual review of provider categories
2. Provide admin UI to fix miscategorized providers
3. Archive old migration data

### User Impact
- **Existing Providers**: Automatically assigned a category based on their current type/services
- **New Providers**: Clear category selection during registration
- **Customers**: Better search/filtering by service category
- **Developers**: Simpler, clearer domain model

### Rollback Plan
If critical issues arise:
1. **Immediate**: Revert backend deployment
2. **Database**: Keep old `type` column as backup for 30 days
3. **Data recovery**: Re-populate `type` from `primary_category` mapping
4. **Frontend**: Revert to old registration flow

### Risk Mitigation
1. **Testing**: Comprehensive unit + integration tests before deployment
2. **Staging**: Full migration test on production data clone
3. **Monitoring**: Track registration errors, category distribution
4. **Admin tools**: Provide UI for manual category corrections
5. **Documentation**: Clear mapping guide for support team
