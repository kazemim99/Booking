# Provider Category Model Refactoring - Design Document

## Context

The current Booksy domain model has three overlapping concepts for categorizing providers:
1. **ProviderType** (enum): Mixes business structure with industry verticals
2. **ServiceCategory** (value object): Only on Service entities, not Providers
3. **ProviderHierarchyType** (enum): Organization vs Individual

**Stakeholders**:
- Backend developers maintaining domain model
- Frontend developers implementing registration flow
- Product team defining business rules
- Users (providers) during registration
- Customers searching/filtering providers

**Constraints**:
- Must maintain backward compatibility with existing data
- Must preserve all existing bookings, reviews, and services
- Must work within DDD bounded context (ServiceCatalog)
- Cannot break existing provider profiles
- Migration must be zero-downtime

**Business Requirement**: "Every provider can have only ONE category"

## Goals / Non-Goals

### Goals
1. **Eliminate semantic confusion** between ProviderType, ServiceCategory, and HierarchyType
2. **Enforce one-category-per-provider** rule at domain level
3. **Align frontend and backend** category representations
4. **Enable provider-level filtering** by service category
5. **Maintain data integrity** during migration

### Non-Goals
1. **Multiple categories per provider** - explicitly out of scope (business constraint)
2. **Sub-categories or hierarchies** - keep simple, flat structure
3. **Dynamic/user-defined categories** - predefined enum only
4. **Category localization in domain** - handle in presentation layer
5. **Service category validation** - defer to later phase

## Decisions

### Decision 1: Delete ProviderType, Keep HierarchyType

**Choice**: Remove `ProviderType` enum entirely, rely on `ProviderHierarchyType` for business structure

**Rationale**:
- `ProviderType` conflates two concerns: business structure (Individual, Professional) and industry (Salon, Clinic, Automotive)
- `HierarchyType` already handles business structure (Organization vs Individual)
- Industry/service categorization belongs in ServiceCategory, not ProviderType
- **Result**: Cleaner separation of concerns

**Alternatives Considered**:
1. ❌ **Keep ProviderType, remove HierarchyType** - ProviderType too broad, mixes unrelated concepts
2. ❌ **Keep both, clarify semantics** - Still confusing, doesn't solve overlap problem
3. ✅ **Remove ProviderType entirely** - Simplest, eliminates confusion

### Decision 2: Convert ServiceCategory from Value Object to Enum

**Choice**: Replace `ServiceCategory` value object with a simple enum

**Rationale**:
- **Performance**: Enums stored as integers, faster comparisons
- **Database efficiency**: 4 bytes vs varchar, better indexing
- **Type safety**: Compile-time validation, no string parsing
- **Metadata**: Can use attributes/extension methods for display names, icons, colors
- **Simplicity**: Predefined categories fit enum model perfectly
- **DDD alignment**: ServiceCategory is a true value type (equality by value), enum represents this well

**Value Object Drawbacks**:
- Unnecessary complexity for simple categorical data
- EF Core value object mapping adds boilerplate
- Harder to query/filter in database
- No runtime benefits over enum in this case

**Alternatives Considered**:
1. ❌ **Keep as value object** - Overly complex for simple categories
2. ❌ **Use string constants** - No type safety, error-prone
3. ✅ **Use enum with extension methods** - Best balance of simplicity and functionality

### Decision 3: Add PrimaryCategory to Provider Aggregate

**Choice**: Add required `ServiceCategory PrimaryCategory` property to Provider

**Rationale**:
- **Business alignment**: Enforces "one category per provider" rule
- **Searchability**: Can filter providers by category directly
- **Registration clarity**: Frontend category selection maps to this property
- **Data integrity**: Non-nullable, required at creation
- **Domain correctness**: Provider IS-A category (identity), not HAS-A services of category

**Placement**:
```csharp
public sealed class Provider : AggregateRoot<ProviderId>
{
    public ProviderHierarchyType HierarchyType { get; } // Organization or Individual
    public ServiceCategory PrimaryCategory { get; }     // Hair Salon, Spa, Clinic, etc.
}
```

**Alternatives Considered**:
1. ❌ **Infer from Service.Category** - Fragile, what if services change or are empty?
2. ❌ **Allow multiple categories** - Violates business rule, increases complexity
3. ❌ **Store as string** - No type safety, harder to query
4. ✅ **Required enum property** - Clear, enforced, performant

### Decision 4: ServiceCategory Enum Values

**Choice**: Define ServiceCategory enum with 15 initial values grouped by domain

```csharp
public enum ServiceCategory
{
    // Beauty & Personal Care (1-5)
    HairSalon = 1,        // آرایشگاه زنانه - Women's hair salon
    Barbershop = 2,       // آرایشگاه مردانه - Men's barbershop
    BeautySalon = 3,      // سالن زیبایی - Beauty salon (makeup, nails, skincare)
    NailSalon = 4,        // آرایش ناخن - Nail salon
    Spa = 5,              // اسپا - Spa & wellness

    // Health & Wellness (6-8)
    Massage = 6,          // ماساژ - Massage therapy
    Gym = 7,              // باشگاه ورزشی - Gym & fitness
    Yoga = 8,             // یوگا - Yoga & meditation

    // Medical (9-11)
    MedicalClinic = 9,    // کلینیک پزشکی - Medical clinic
    Dental = 10,          // دندانپزشکی - Dental clinic
    Physiotherapy = 11,   // فیزیوتراپی - Physiotherapy

    // Professional Services (12-15)
    Tutoring = 12,        // آموزش خصوصی - Private tutoring
    Automotive = 13,      // تعمیرات خودرو - Auto repair/service
    HomeServices = 14,    // خدمات منزل - Home services
    PetCare = 15          // مراقبت حیوانات - Pet care
}
```

**Rationale**:
- **Gender-specific hair services**: Separate HairSalon (women) vs Barbershop (men) matches Iranian market
- **Granular beauty services**: NailSalon separate from BeautySalon for specialization
- **Medical distinction**: Medical vs wellness services clearly separated
- **Extensible**: Can add more categories later without breaking changes
- **Semantic clarity**: Each category has single, well-defined purpose

**Alternatives Considered**:
1. ❌ **Generic "Beauty" category** - Too broad, doesn't match frontend needs
2. ❌ **Combined "HairCare"** - Doesn't distinguish men's vs women's salons
3. ❌ **Dozens of micro-categories** - Over-engineered, analysis paralysis
4. ✅ **15 well-defined categories** - Right granularity for MVP

### Decision 5: Category Metadata via Extension Methods

**Choice**: Store display metadata (Persian name, icon, color) in extension methods, not database

```csharp
public static class ServiceCategoryExtensions
{
    public static string ToPersianName(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "آرایشگاه زنانه",
        ServiceCategory.Barbershop => "آرایشگاه مردانه",
        // ...
    };

    public static string ToIcon(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "💇‍♀️",
        ServiceCategory.Barbershop => "💇‍♂️",
        // ...
    };

    public static string ToColorHex(this ServiceCategory category) => category switch
    {
        ServiceCategory.HairSalon => "#8B5CF6",
        ServiceCategory.Barbershop => "#3B82F6",
        // ...
    };

    public static string ToSlug(this ServiceCategory category) =>
        category.ToString().ToLowerInvariant().Replace("_", "-");
}
```

**Rationale**:
- **Code-first metadata**: Display info stays in code, not database
- **Type safety**: Compile-time validation
- **Performance**: No database lookups for display data
- **Versioning**: Easy to update metadata without migrations
- **Localization**: Can add more extension methods for other languages

**Alternatives Considered**:
1. ❌ **Database table** - Overkill, metadata rarely changes
2. ❌ **Attributes on enum** - Limited metadata, reflection overhead
3. ❌ **Separate configuration class** - Extra indirection
4. ✅ **Extension methods** - Simple, fast, maintainable

### Decision 6: Migration Strategy - Infer from Services

**Choice**: Assign PrimaryCategory by analyzing provider's existing services

**Migration Logic**:
```sql
-- Step 1: Infer category from most common service category
UPDATE providers p
SET primary_category = (
    SELECT CASE
        WHEN COUNT(DISTINCT s.category) = 1 THEN /* map that category */
        ELSE /* take most frequent category */
    END
    FROM services s
    WHERE s.provider_id = p.id
    GROUP BY s.provider_id
);

-- Step 2: Fallback to ProviderType for providers without services
UPDATE providers p
SET primary_category = CASE p.type
    WHEN 'Salon' THEN 1  -- HairSalon
    WHEN 'Spa' THEN 5    -- Spa
    WHEN 'Clinic' THEN 9 -- MedicalClinic
    -- etc.
END
WHERE primary_category IS NULL;

-- Step 3: Default remaining to HairSalon (most common)
UPDATE providers
SET primary_category = 1  -- HairSalon default
WHERE primary_category IS NULL;
```

**Rationale**:
- **Data-driven**: Uses actual service data to infer category
- **Accurate**: Providers offering hair services get HairSalon category
- **Safe fallback**: ProviderType provides secondary inference
- **No data loss**: All providers get a valid category
- **Reversible**: Keep old data for rollback

**Alternatives Considered**:
1. ❌ **Manual categorization** - Doesn't scale, too slow
2. ❌ **Default all to one category** - Inaccurate, breaks search
3. ❌ **Require providers to re-register** - Bad UX, not feasible
4. ✅ **Infer from service data** - Best accuracy with automation

### Decision 7: Service Category Alignment Validation (Future Phase)

**Choice**: DEFER validation that Service.Category aligns with Provider.PrimaryCategory

**Rationale**:
- **Migration complexity**: Existing data may have misaligned services
- **Business rule ambiguity**: Should a hair salon offer massage services?
- **Phase approach**: Get category model working first, add validation later
- **Flexibility**: Allow some cross-category services (e.g., spa with haircare)

**Future Validation Rule** (Phase 2):
```csharp
public class CreateServiceValidator
{
    public void Validate(Provider provider, Service service)
    {
        if (!IsCompatible(provider.PrimaryCategory, service.Category))
        {
            throw new ServiceCategoryMismatchException(
                $"Service category {service.Category} not compatible with provider category {provider.PrimaryCategory}"
            );
        }
    }

    private bool IsCompatible(ServiceCategory providerCategory, ServiceCategory serviceCategory)
    {
        // Define compatibility matrix
        return (providerCategory, serviceCategory) switch
        {
            (ServiceCategory.BeautySalon, ServiceCategory.HairSalon) => true,  // Beauty salon can offer hair
            (ServiceCategory.Spa, ServiceCategory.Massage) => true,            // Spa can offer massage
            (var p, var s) when p == s => true,                                 // Same category always OK
            _ => false
        };
    }
}
```

**Alternatives Considered**:
1. ❌ **Enforce immediately** - Breaks existing data, requires cleanup first
2. ❌ **Never validate** - Allows bad data, defeats purpose of category
3. ✅ **Defer to Phase 2** - Pragmatic, allows clean migration

## Risks / Trade-offs

### Risk 1: Migration Data Quality

**Risk**: Inferred categories may be incorrect for some providers

**Mitigation**:
- **Admin UI**: Provide dashboard to review/fix categories post-migration
- **Analytics**: Track category distribution, flag outliers
- **Support process**: Allow providers to request category change
- **Monitoring**: Alert on providers with no services matching their category

### Risk 2: Frontend Breaking Changes

**Risk**: Registration flow changes may break in-flight registrations

**Mitigation**:
- **Backward compatibility**: Support both old and new registration API temporarily
- **Feature flag**: Gradual rollout of new category selection
- **Session handling**: Migrate in-progress registration sessions
- **Clear error messages**: Guide users if validation fails

### Risk 3: Search Performance

**Risk**: Adding PrimaryCategory queries may slow search

**Mitigation**:
- **Database index**: Create index on `providers.primary_category`
- **Caching**: Cache category filters in Redis
- **Denormalization**: Include category in search index
- **Query optimization**: Use covering indexes for common queries

### Risk 4: Category Taxonomy Changes

**Risk**: Need to add/remove/rename categories in future

**Mitigation**:
- **Versioned enum**: Use explicit integer values (not auto-increment)
- **Deprecation process**: Mark old categories deprecated, not deleted
- **Migration tool**: Provide category merge/split utilities
- **Documentation**: Clear governance for category changes

### Risk 5: International Expansion

**Risk**: Categories may not fit non-Iranian markets

**Mitigation**:
- **Extensible design**: Easy to add region-specific categories
- **Metadata separation**: Display names externalized, not hardcoded
- **Market research**: Validate categories for target markets
- **Gradual rollout**: Test in one market before expanding

## Implementation Plan

### Phase 1: Domain Model (Week 1)
1. Create `ServiceCategory` enum
2. Add extension methods for metadata
3. Update `Provider` aggregate with `PrimaryCategory`
4. Update `Service` aggregate to use enum
5. Unit tests for new domain logic

### Phase 2: Database Migration (Week 1)
1. Create migration script
2. Test on staging database (production clone)
3. Generate category assignment report
4. Review and fix any obvious misassignments
5. Prepare rollback script

### Phase 3: Backend Implementation (Week 2)
1. Update repositories for enum mapping
2. Update DTOs and query models
3. Update API controllers
4. Update registration commands
5. Integration tests

### Phase 4: Frontend Implementation (Week 2-3)
1. Define TypeScript category types
2. Update registration CategorySelectionStep
3. Update provider profile display
4. Update search/filter components
5. E2E tests

### Phase 5: Deployment (Week 3)
1. Deploy database migration (zero-downtime)
2. Deploy backend services
3. Deploy frontend (feature flag)
4. Monitor error rates and category distribution
5. Gradual rollout to 100%

### Phase 6: Cleanup & Validation (Week 4)
1. Admin UI for category corrections
2. Analyze category distribution
3. Fix misassigned providers
4. Remove old ProviderType code
5. Update documentation

## Rollback Plan

### Immediate Rollback (Day 0-1)
1. **Frontend**: Flip feature flag, revert to old registration
2. **Backend**: Revert deployment
3. **Database**: Keep new column, re-enable old column
4. **Impact**: Minimal, new registrations affected only

### Extended Rollback (Day 2-7)
1. **Data recovery**: Re-populate `type` from `primary_category` mapping
2. **Code**: Revert all domain model changes
3. **Testing**: Verify old flow works
4. **Communication**: Notify users of temporary issues

### Full Rollback (Week 2+)
1. **Migration script**: Drop `primary_category` column
2. **Remove enum**: Delete `ServiceCategory` enum
3. **Restore value object**: Un-comment old `ServiceCategory.cs`
4. **Database cleanup**: Remove migration artifacts

## Success Metrics

### Technical Metrics
- **Migration success rate**: 100% of providers assigned valid category
- **Category accuracy**: >95% alignment between provider and services
- **Query performance**: <50ms p95 for category-filtered search
- **Zero downtime**: No service interruption during migration

### Business Metrics
- **Registration completion rate**: Maintain >70%
- **Category distribution**: Balanced across categories (no single category >50%)
- **Support tickets**: <5% increase in category-related issues
- **Search improvement**: 20% increase in category-filtered searches

### User Experience Metrics
- **Registration confusion**: Track "back" button clicks on category step
- **Category change requests**: <2% of providers request category change
- **Search satisfaction**: A/B test category filters vs old search

## Open Questions

1. **Cross-category services**: Should we allow services that don't match provider category?
   - **Recommendation**: Phase 2 - define compatibility matrix

2. **Category changes**: Can providers change their category after registration?
   - **Recommendation**: Yes, with admin approval to prevent abuse

3. **Subcategories**: Do we need finer granularity (e.g., "Laser Hair Removal" under BeautySalon)?
   - **Recommendation**: Phase 3 - add service tags, not new categories

4. **Search display**: How to show category in search results?
   - **Recommendation**: Badge with icon + name, filterable chips

5. **Analytics**: Which category metrics to track?
   - **Recommendation**: Distribution, growth rate, booking conversion by category
