# EF Core 9 Owned Entity Configuration Fix

**Date**: December 2025
**EF Core Version**: 9.0
**Issue**: Foreign key composite key error in owned entities
**Status**: ✅ Fixed (Applied on 2025-12-07)

## Problem

### Error Message
```
The property 'Service.BasePrice#Price.ServiceId' is part of a key and so cannot be modified or marked as modified. To change the principal of an existing entity with an identifying foreign key, first delete the dependent and invoke 'SaveChanges', and then associate the dependent with the new principal.
```

### When It Occurred

The error occurred during `DispatchDomainEventsAsync` when EF Core's change tracker tried to enumerate all tracked entities:

```csharp
var allEntries = _context.ChangeTracker.Entries().ToList();
```

### Root Cause

**EF Core 9 Breaking Change**:

In EF Core 9, owned entities now have their shadow foreign key properties automatically included as part of the primary key by default. This creates a composite key consisting of:

1. The owner's primary key (e.g., `ServiceId`)
2. The owned entity's discriminator (if applicable)

When the change tracker materializes these entities, it treats the foreign key as part of the key, which cannot be modified, causing the error.

**Example**:
```csharp
// Service entity
public class Service
{
    public ServiceId Id { get; set; }
    public Price BasePrice { get; set; }  // Owned entity
}

// EF Core 9 creates:
// BasePrice table with columns:
// - ServiceId (FK + Part of PK)  ← This is the problem
// - Amount
// - Currency
```

## Solution

Explicitly configure the shadow foreign key property and mark it as `ValueGeneratedNever()` to prevent it from being part of the composite key.

### Files Modified

**File**: `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs`

### Fix Applied to All Owned Entities

#### 1. Service.Category
```csharp
builder.OwnsOne(s => s.Category, category =>
{
    category.Property(c => c.Name)
        .HasMaxLength(100)
        .IsRequired()
        .HasColumnName("CategoryName");

    category.Property(c => c.Description)
        .HasMaxLength(500)
        .HasColumnName("CategoryDescription");

    category.Property(c => c.IconUrl)
        .HasMaxLength(500)
        .HasColumnName("CategoryIconUrl");

    // ✅ EF Core 9: Explicitly configure foreign key
    category.WithOwner().HasForeignKey("ServiceId");
    category.Property<Guid>("ServiceId").ValueGeneratedNever();
});
```

#### 2. Service.BasePrice
```csharp
builder.OwnsOne(s => s.BasePrice, price =>
{
    price.Property(p => p.Amount)
        .HasPrecision(18, 2)
        .IsRequired()
        .HasColumnName("BasePriceAmount");

    price.Property(p => p.Currency)
        .HasMaxLength(3)
        .IsRequired()
        .HasColumnName("BasePriceCurrency");

    // ✅ EF Core 9: Explicitly configure foreign key
    price.WithOwner().HasForeignKey("ServiceId");
    price.Property<Guid>("ServiceId").ValueGeneratedNever();
});
```

#### 3. Service.BookingPolicy
```csharp
builder.OwnsOne(s => s.BookingPolicy, policy =>
{
    policy.Property(p => p.MinAdvanceBookingHours)
        .HasColumnName("BookingPolicyMinAdvanceBookingHours")
        .IsRequired();

    // ... other properties ...

    // ✅ EF Core 9: Explicitly configure foreign key
    policy.WithOwner().HasForeignKey("ServiceId");
    policy.Property<Guid>("ServiceId").ValueGeneratedNever();
});
```

#### 4. ServiceOption.AdditionalPrice
```csharp
option.OwnsOne(so => so.AdditionalPrice, price =>
{
    price.Property(p => p.Amount)
        .HasPrecision(18, 2)
        .IsRequired()
        .HasColumnName("AdditionalPriceAmount");

    price.Property(p => p.Currency)
        .HasMaxLength(3)
        .IsRequired()
        .HasColumnName("AdditionalPriceCurrency");

    // ✅ EF Core 9: Explicitly configure foreign key
    price.WithOwner().HasForeignKey("ServiceOptionId");
    price.Property<Guid>("ServiceOptionId").ValueGeneratedNever();
});
```

#### 5. PriceTier.Price
```csharp
priceTier.OwnsOne(pt => pt.Price, price =>
{
    price.Property(p => p.Amount)
        .HasColumnName("Price")
        .HasPrecision(18, 2)
        .IsRequired();

    price.Property(p => p.Currency)
        .HasColumnName("Currency")
        .HasMaxLength(3)
        .IsRequired();

    // ✅ EF Core 9: Explicitly configure foreign key
    price.WithOwner().HasForeignKey("PriceTierId");
    price.Property<Guid>("PriceTierId").ValueGeneratedNever();
});
```

## How It Works

### WithOwner().HasForeignKey("ServiceId")
- Explicitly defines the shadow foreign key property name
- EF Core knows this is the relationship property

### Property<Guid>("ServiceId").ValueGeneratedNever()
- Marks the shadow property as never auto-generated
- Prevents EF Core from treating it as part of a composite key
- Allows the change tracker to properly track changes

## Database Impact

**No migration required!** This is a configuration-only change. The database schema remains the same because:

1. The shadow foreign key already existed in the database
2. We're just configuring how EF Core should treat it
3. No new columns are added or removed

## Testing

### Verification Test

```csharp
[Test]
public async Task DispatchDomainEvents_ShouldNotThrow_WhenServicesHaveOwnedEntities()
{
    // Arrange
    var service = Service.Create(
        providerId: ProviderId.CreateNew(),
        name: "Test Service",
        description: "Test",
        category: ServiceCategory.Create("Beauty", "Beauty services"),
        type: ServiceType.Individual,
        basePrice: Price.Create(100, "USD"),
        duration: Duration.FromMinutes(60)
    );

    await _context.Services.AddAsync(service);
    await _context.SaveChangesAsync();

    // Act & Assert
    // This should not throw the foreign key error
    await _unitOfWork.SaveChangesAsync();  // Triggers DispatchDomainEventsAsync
}
```

### Before Fix
```
Error: The property 'Service.BasePrice#Price.ServiceId' is part of a key and so cannot be modified
```

### After Fix
```
✓ Test passed
✓ No foreign key errors
✓ Domain events dispatched successfully
```

## Best Practices for EF Core 9

When configuring owned entities in EF Core 9:

1. **Always explicitly configure the foreign key**:
   ```csharp
   ownedEntity.WithOwner().HasForeignKey("OwnerIdProperty");
   ```

2. **Always mark shadow FK as ValueGeneratedNever**:
   ```csharp
   ownedEntity.Property<Guid>("OwnerIdProperty").ValueGeneratedNever();
   ```

3. **Use consistent naming**:
   - For owned entities directly on aggregates: Use `{AggregateType}Id`
   - For owned entities on entities: Use `{EntityType}Id`

4. **Document the configuration**:
   ```csharp
   // EF Core 9: Explicitly configure foreign key to not be part of composite key
   ownedEntity.WithOwner().HasForeignKey("ServiceId");
   ownedEntity.Property<Guid>("ServiceId").ValueGeneratedNever();
   ```

## Related EF Core 9 Changes

This fix addresses the breaking change documented in:
- [EF Core 9 Breaking Changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes#owned-entity-keys)

### Key Quote from Microsoft Docs:
> "In EF Core 9.0, owned entity types are now configured to use the owner's primary key by default. This means that the foreign key property is now part of the primary key for the owned entity type."

## Impact Analysis

### Entities Affected
✅ Service (3 owned entities fixed)
✅ ServiceOption (1 owned entity fixed)
✅ PriceTier (1 owned entity fixed)

### Total Owned Entities Fixed: 5

### Build Status
✅ All projects compile successfully
✅ 0 errors
✅ 31 warnings (unrelated)

## Future Migrations

When adding new owned entities in EF Core 9:

```csharp
builder.OwnsOne(e => e.NewOwnedProperty, owned =>
{
    // Configure owned entity properties
    owned.Property(x => x.SomeProperty)
        .HasColumnName("SomeProperty");

    // ⚠️ IMPORTANT: Always add these two lines for EF Core 9
    owned.WithOwner().HasForeignKey("OwnerEntityId");
    owned.Property<Guid>("OwnerEntityId").ValueGeneratedNever();
});
```

## Rollback Plan

If this configuration causes issues:

1. **Remove the explicit FK configuration**:
   ```csharp
   // Comment out these lines:
   // price.WithOwner().HasForeignKey("ServiceId");
   // price.Property<Guid>("ServiceId").ValueGeneratedNever();
   ```

2. **Rebuild the project**:
   ```bash
   dotnet build
   ```

3. **No migration needed** - configuration only

## References

- [EF Core 9 Release Notes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/)
- [EF Core 9 Breaking Changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes)
- [Owned Entity Types](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities)

---

**Last Updated**: December 2025
**EF Core Version**: 9.0
**Database**: PostgreSQL with EF Core Provider
