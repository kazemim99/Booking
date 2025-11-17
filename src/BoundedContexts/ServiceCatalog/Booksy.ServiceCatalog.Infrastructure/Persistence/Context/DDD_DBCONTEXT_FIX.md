# DDD DbContext Fix - Remove Direct Entity Access

## Problem

The current `ServiceCatalogDbContext` exposes child entities as `DbSet<T>`, which violates DDD aggregate boundaries:

```csharp
// ❌ WRONG - Direct access to child entities
public DbSet<Staff> Staff => Set<Staff>();
public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
public DbSet<HolidaySchedule> Holidays => Set<HolidaySchedule>();
public DbSet<ExceptionSchedule> Exceptions => Set<ExceptionSchedule>();
```

## DDD Principle

**Only Aggregate Roots should be exposed as DbSets.**

Child entities should ONLY be accessible through their aggregate root via navigation properties.

## Aggregate Hierarchy

```
Provider (Aggregate Root)
├── Staff (Child Entity)
├── BusinessHours (Child Entity)
├── HolidaySchedule (Child Entity)
└── ExceptionSchedule (Child Entity)

Service (Aggregate Root)
├── ServiceOption (Owned Entity - already correct with OwnsMany)
└── PriceTier (Owned Entity - already correct with OwnsMany)

Booking (Aggregate Root)
└── BookingHistoryEntry (Child Entity)
```

## Solution

### Step 1: Remove Entity DbSets

**File**: `ServiceCatalogDbContext.cs`

```diff
  // Aggregate Roots - ✅ Keep these
  public DbSet<Provider> Providers => Set<Provider>();
  public DbSet<Service> Services => Set<Service>();
  public DbSet<Booking> Bookings => Set<Booking>();
  public DbSet<Payment> Payments => Set<Payment>();
  public DbSet<Payout> Payouts => Set<Payout>();
  public DbSet<Notification> Notifications => Set<Notification>();
  public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
  public DbSet<UserNotificationPreferences> UserNotificationPreferences => Set<UserNotificationPreferences>();
  public DbSet<ProviderAvailability> ProviderAvailability => Set<ProviderAvailability>();
  public DbSet<Review> Reviews => Set<Review>();

- // Entities - ❌ Remove these
- public DbSet<Staff> Staff => Set<Staff>();
- public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
- public DbSet<HolidaySchedule> Holidays => Set<HolidaySchedule>();
- public DbSet<ExceptionSchedule> Exceptions => Set<ExceptionSchedule>();

  // Keep this - it's a standalone entity for location data
  public DbSet<ProvinceCities> ProvinceCities => Set<ProvinceCities>();
```

### Step 2: Update Code That Directly Queries Child Entities

Before removing the DbSets, find all code that queries them directly:

```bash
# Search for direct queries
grep -r "context.Staff" --include="*.cs"
grep -r "context.BusinessHours" --include="*.cs"
grep -r "context.Holidays" --include="*.cs"
grep -r "context.Exceptions" --include="*.cs"
```

Replace with queries through aggregate root:

#### Example 1: Finding Staff

**Before (Wrong):**
```csharp
var staff = await _dbContext.Staff
    .FirstOrDefaultAsync(s => s.Id == staffId);
```

**After (Correct):**
```csharp
var provider = await _dbContext.Providers
    .Include(p => p.Staff)
    .FirstOrDefaultAsync(p => p.Staff.Any(s => s.Id == staffId));

var staff = provider?.Staff.FirstOrDefault(s => s.Id == staffId);
```

#### Example 2: Finding Business Hours

**Before (Wrong):**
```csharp
var businessHours = await _dbContext.BusinessHours
    .Where(bh => bh.ProviderId == providerId)
    .ToListAsync();
```

**After (Correct):**
```csharp
var provider = await _dbContext.Providers
    .Include(p => p.BusinessHours)
    .FirstOrDefaultAsync(p => p.Id == providerId);

var businessHours = provider?.BusinessHours;
```

### Step 3: Update Seeders

Seeders often query entities directly. Update them to work through aggregates:

**Before (Wrong):**
```csharp
var allStaff = await _context.Staff.ToListAsync();
```

**After (Correct):**
```csharp
var providers = await _context.Providers
    .Include(p => p.Staff)
    .ToListAsync();

var allStaff = providers.SelectMany(p => p.Staff).ToList();
```

### Step 4: Ensure EF Core Configurations Still Work

Even without DbSets, you can still configure entities in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure child entities through the aggregate root
    modelBuilder.Entity<Provider>()
        .HasMany<Staff>()
        .WithOne()
        .HasForeignKey("ProviderId");

    // Or use IEntityTypeConfiguration<T> (recommended)
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProviderConfiguration).Assembly);
}
```

Your existing `ProviderConfiguration` should handle this correctly.

## Benefits of This Approach

### 1. ✅ Enforces Aggregate Boundaries
```csharp
// ❌ Can't do this anymore (good!)
_dbContext.Staff.Add(new Staff(...)); // Bypasses Provider aggregate

// ✅ Must do this (correct!)
provider.AddStaff("John", "Doe", StaffRole.ServiceProvider, phoneNumber);
await _dbContext.SaveChangesAsync();
```

### 2. ✅ Prevents Orphaned Entities
```csharp
// ❌ Can't delete staff without going through Provider (good!)
_dbContext.Staff.Remove(staff);

// ✅ Must use aggregate method (ensures business rules)
provider.RemoveStaff(staffId);
```

### 3. ✅ Ensures Business Rules are Applied
```csharp
// ❌ Can't bypass validation (good!)
var staff = new Staff { /* ... */ };
_dbContext.Staff.Add(staff);

// ✅ Business rules in AddStaff method are enforced
provider.AddStaff(...); // Validates, checks duplicates, etc.
```

### 4. ✅ Clearer Code
```csharp
// Shows intent: "Get provider and their staff"
var provider = await _dbContext.Providers
    .Include(p => p.Staff)
    .FirstOrDefaultAsync(p => p.Id == providerId);
```

## Migration Strategy

### Phase 1: Audit (Find Breaking Changes)
```bash
# Find all direct queries to child entities
grep -r "\.Staff\b" --include="*.cs" src/
grep -r "\.BusinessHours\b" --include="*.cs" src/
grep -r "\.Holidays\b" --include="*.cs" src/
grep -r "\.Exceptions\b" --include="*.cs" src/
```

### Phase 2: Refactor
1. Update each file to query through aggregate root
2. Add `.Include()` statements where needed
3. Update navigation property access

### Phase 3: Remove DbSets
1. Comment out the DbSet properties
2. Build and fix any compilation errors
3. Run tests
4. If all tests pass, remove the commented code

### Phase 4: Verify
```bash
# Run all tests
dotnet test

# Check for any remaining references
grep -r "context.Staff" --include="*.cs"
```

## Common Patterns

### Pattern 1: Query by Child Entity ID

```csharp
// Find provider that has specific staff member
var provider = await _dbContext.Providers
    .Include(p => p.Staff)
    .FirstOrDefaultAsync(p => p.Staff.Any(s => s.Id == staffId));

var staff = provider?.Staff.FirstOrDefault(s => s.Id == staffId);
```

### Pattern 2: Update Child Entity

```csharp
// Load aggregate with children
var provider = await _dbContext.Providers
    .Include(p => p.Staff)
    .FirstOrDefaultAsync(p => p.Id == providerId);

// Use aggregate method (recommended)
provider.UpdateStaff(staffId, newName, newRole);

// Or modify directly if needed (less preferred)
var staff = provider.Staff.First(s => s.Id == staffId);
// Modify staff properties
// Provider tracks changes automatically

await _dbContext.SaveChangesAsync();
```

### Pattern 3: Repository Pattern Hides This Complexity

```csharp
public class ProviderRepository : IProviderRepository
{
    public async Task<Staff?> GetStaffAsync(Guid staffId)
    {
        var provider = await _dbContext.Providers
            .Include(p => p.Staff)
            .FirstOrDefaultAsync(p => p.Staff.Any(s => s.Id == staffId));

        return provider?.Staff.FirstOrDefault(s => s.Id == staffId);
    }
}
```

## Exceptions

Keep `DbSet<T>` for:

1. ✅ **Aggregate Roots** - Always expose
2. ✅ **Read Models** - For query-side projections
3. ✅ **Lookup/Reference Data** - Like `ProvinceCities`
4. ❌ **Child Entities** - Never expose

## Questions to Ask

When deciding whether to expose something as a DbSet:

1. **Can this exist independently?** → If yes, maybe it's an aggregate root
2. **Can it be created without a parent?** → If no, it's a child entity
3. **Does it have its own lifecycle?** → If no, it's a child entity
4. **Would direct access bypass business rules?** → If yes, don't expose it

## Final DbContext

```csharp
public sealed class ServiceCatalogDbContext : DbContext
{
    // ONLY Aggregate Roots
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<UserNotificationPreferences> UserNotificationPreferences => Set<UserNotificationPreferences>();
    public DbSet<ProviderAvailability> ProviderAvailability => Set<ProviderAvailability>();
    public DbSet<Review> Reviews => Set<Review>();

    // Reference Data (not part of aggregates)
    public DbSet<ProvinceCities> ProvinceCities => Set<ProvinceCities>();

    // NO child entities exposed!
}
```

This is **true DDD** and protects your aggregate boundaries! 🎯
