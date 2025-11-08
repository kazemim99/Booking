# Request for Claude Opus: EF Core Change Tracking Issue with Owned Entities

## Problem Summary

**Test Failing:** `DeleteGalleryImage_WithValidImage_DeletesSuccessfully`
**Error:** `Expected deletedImage!.IsActive to be False, but found True.`

**Root Issue:** Changes to owned entity properties are not being persisted to the database despite multiple configuration and tracking attempts.

---

## Architecture Context

### Domain-Driven Design with CQRS
- **Pattern:** Aggregates with owned entities
- **ORM:** Entity Framework Core (PostgreSQL)
- **Transaction Management:** UnitOfWork pattern with TransactionBehavior (MediatR pipeline)
- **Change Tracking:** Attempting to use snapshot-based tracking

### Entity Hierarchy
```
Provider (Aggregate Root)
  └── BusinessProfile (Owned Entity)
       └── GalleryImages (Owned Collection - private backing field _galleryImages)
            └── GalleryImage (Owned Entity)
                 ├── IsActive (bool) ← This property change is NOT persisting
                 ├── DisplayOrder (int)
                 ├── IsPrimary (bool)
                 └── Other properties...
```

### Code Flow
1. **Test calls:** `DELETE /api/v1/providers/{id}/gallery/{imageId}`
2. **Command Handler:** `DeleteGalleryImageCommandHandler.Handle()`
   ```csharp
   var provider = await _providerRepository.GetByIdAsync(providerId); // Loads with Include
   provider.Profile.RemoveGalleryImage(imageId); // Calls image.Deactivate()
   await _providerRepository.UpdateProviderAsync(provider);
   // UnitOfWork saves via TransactionBehavior
   ```
3. **Domain Method:** `BusinessProfile.RemoveGalleryImage(imageId)`
   ```csharp
   var image = _galleryImages.FirstOrDefault(img => img.Id == imageId);
   image.Deactivate(); // Sets IsActive = false
   LastUpdatedAt = DateTime.UtcNow;
   ```
4. **Test verification reads from database:** `IsActive` is still `true`

---

## Current Configuration

### ProviderConfiguration.cs (Lines 105-192)
```csharp
profile.OwnsMany(bp => bp.GalleryImages, galleryImage =>
{
    galleryImage.ToTable("provider_gallery_images");

    // Snapshot change tracking
    galleryImage.HasChangeTrackingStrategy(ChangeTrackingStrategy.Snapshot);

    galleryImage.WithOwner().HasForeignKey("ProviderId");
    galleryImage.HasKey(gi => gi.Id);

    galleryImage.Property(gi => gi.IsActive)
        .HasColumnName("is_active")
        .IsRequired()
        .HasDefaultValue(true);

    // ... other property configurations
});

// Navigation configuration
profile.Navigation(bp => bp.GalleryImages)
    .UsePropertyAccessMode(PropertyAccessMode.Field)
    .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
```

### ProviderWriteRepository.cs (Lines 65-71)
```csharp
public async Task UpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
{
    // With snapshot change tracking configured on GalleryImages,
    // we need to explicitly call DetectChanges to compare current values
    // with the original snapshot and detect modifications
    Context.ChangeTracker.DetectChanges();
}
```

### BusinessProfile.cs (Domain Entity)
```csharp
public sealed class BusinessProfile : Entity<Guid>
{
    private readonly List<GalleryImage> _galleryImages = new();

    public IReadOnlyList<GalleryImage> GalleryImages => _galleryImages.AsReadOnly();

    public void RemoveGalleryImage(Guid imageId)
    {
        var image = _galleryImages.FirstOrDefault(img => img.Id == imageId);
        if (image == null)
            throw new DomainValidationException("Gallery image not found");

        image.Deactivate(); // Sets IsActive = false
        LastUpdatedAt = DateTime.UtcNow;
    }
}
```

### GalleryImage.cs (Owned Entity)
```csharp
public sealed class GalleryImage : Entity<Guid>
{
    public bool IsActive { get; private set; }

    public void Deactivate()
    {
        IsActive = false; // ← This change is NOT being persisted!
    }
}
```

### UnitOfWork (SaveChanges happens here)
```csharp
// EfCoreUnitOfWork.cs - ExecuteInTransactionAsync (Line 268-290)
public async Task ExecuteInTransactionAsync(Func<Task> operation, ...)
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        await operation(); // Command handler runs here
        var result = await _context.SaveChangesAsync(cancellationToken); // ← Should save changes
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

---

## All Attempts Made (6 Failed Attempts)

### Attempt 1: PreferFieldDuringConstruction
- Changed `PropertyAccessMode.Field` → `PropertyAccessMode.PreferFieldDuringConstruction`
- **Result:** `Unable to cast ReadOnlyCollection to List` error
- **Reason:** EF Core couldn't work with `IReadOnlyList<GalleryImage>` return type

### Attempt 2: Manual Entity State Tracking
- Explicitly marked provider, profile, and all gallery images as `EntityState.Modified`
- **Result:** Still didn't persist `IsActive` change
- **Issue:** Marking entity as Modified doesn't force property-level changes

### Attempt 3: Explicit Property-Level IsModified
- Set `imageEntry.Property(gi => gi.IsActive).IsModified = true` for each image
- **Result:** Still didn't persist
- **Issue:** EF Core still couldn't detect the actual value change

### Attempt 4: Set CurrentValue + IsModified
- Set `imageEntry.Property(gi => gi.IsActive).CurrentValue = galleryImage.IsActive`
- Then marked `IsModified = true`
- **Result:** Still didn't persist
- **Issue:** Current value setting didn't help

### Attempt 5: Remove PropertyAccessMode.Field
- Removed `.UsePropertyAccessMode(PropertyAccessMode.Field)` entirely
- Simplified `UpdateProviderAsync` to just `Context.Update(provider)`
- **Result:** Still didn't persist
- **Issue:** Default property access mode didn't solve it

### Attempt 6: Snapshot Change Tracking (Current)
- Added `HasChangeTrackingStrategy(ChangeTrackingStrategy.Snapshot)`
- Restored `PropertyAccessMode.Field`
- Call `DetectChanges()` in `UpdateProviderAsync`
- **Result:** STILL DOESN'T WORK ❌
- **Issue:** Unknown - snapshot tracking should work but doesn't

---

## Questions for Claude Opus

1. **Is this a known EF Core limitation** with owned entities using private backing fields and `PropertyAccessMode.Field`?

2. **Does snapshot change tracking work with owned collections** that use `PropertyAccessMode.Field`?

3. **Should we be calling `Context.Update(provider)` in addition to `DetectChanges()`**, or does that interfere with snapshot tracking?

4. **Is the problem related to the entity being already tracked** when loaded via `GetByIdAsync` with `.Include()`?

5. **Could the issue be in how we're using the UnitOfWork pattern?** Should `UpdateProviderAsync` be calling `SaveChangesAsync` directly instead of relying on TransactionBehavior?

6. **Is there a conflict between:**
   - Owned entity collection with private backing field
   - `IReadOnlyList<T>` return type
   - `PropertyAccessMode.Field`
   - Snapshot change tracking

7. **What is the correct EF Core configuration** for this scenario:
   - Owned entity collection
   - Private backing field (`_galleryImages`)
   - Public read-only property (`IReadOnlyList<GalleryImage>`)
   - Need to detect property changes (like `IsActive = false`)
   - Need to support soft deletes

---

## Expected Behavior

When `image.Deactivate()` is called:
1. The `IsActive` property should change from `true` to `false` in memory
2. EF Core should detect this change
3. When `SaveChangesAsync()` is called, it should generate an UPDATE statement
4. The database should be updated
5. Reading the entity again should show `IsActive = false`

---

## Actual Behavior

The change is made in memory but NOT persisted to the database. The test reads the entity from the database and `IsActive` is still `true`.

---

## Files Involved

1. `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ProviderConfiguration.cs`
2. `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Repositories/ProviderWriteRepository.cs`
3. `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/BusinessProfile.cs`
4. `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Entities/GalleryImage.cs`
5. `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/DeleteGalleryImage/DeleteGalleryImageCommandHandler.cs`
6. `tests/Booksy.ServiceCatalog.IntegrationTests/GalleryManagementTests.cs` (Line 401-428)

---

## Request

Please analyze this situation and provide:
1. **Root cause analysis** - Why is EF Core not persisting the property change?
2. **Correct configuration** - What should the entity configuration look like?
3. **Correct repository method** - What should `UpdateProviderAsync` do?
4. **Alternative approaches** - If the current approach is fundamentally flawed, what pattern should we use?

Thank you for your help!
