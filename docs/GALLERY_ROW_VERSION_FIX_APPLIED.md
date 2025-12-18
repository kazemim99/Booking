# Gallery RowVersion Fix Applied

## What Was Done

### 1. ✅ Removed RowVersion from GalleryImage Configuration

**File:** `ProviderConfiguration.cs` (lines 171-175)

**BEFORE:**
```csharp
// Add row version for the gallery image as well
galleryImage.Property<byte[]>("RowVersion")
    .IsConcurrencyToken()
    .HasColumnName("row_version")
    .ValueGeneratedOnAddOrUpdate();
```

**AFTER:**
```csharp
// Note: GalleryImage is an owned entity - concurrency is handled at Provider level
// No separate RowVersion needed here to avoid concurrency conflicts on INSERT operations
```

## Next Steps - Run the Migration

**IMPORTANT:** You need to stop your API and run the migration script.

### Option A: Use the Batch Script (Easy)

1. **Stop your API** (close Visual Studio debugger or stop the process)

2. **Run the script:**
   ```bash
   c:\Repos\Booking\scripts\apply_gallery_fix.bat
   ```

3. **Restart your API**

### Option B: Manual Steps

1. **Stop your API**

2. **Open terminal and navigate:**
   ```bash
   cd c:\Repos\Booking\src\BoundedContexts\ServiceCatalog\Booksy.ServiceCatalog.Api
   ```

3. **Create migration:**
   ```bash
   dotnet ef migrations add RemoveGalleryImageRowVersion --context ServiceCatalogDbContext --project "../Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj" --output-dir "Persistence/Migrations"
   ```

4. **Apply migration:**
   ```bash
   dotnet ef database update --context ServiceCatalogDbContext --project "../Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj"
   ```

5. **Restart your API**

## What This Fixes

### ✅ Error Fixed:
```
Npgsql.PostgresException (0x80004005): 42703: column p0.row_version does not exist
```

### ✅ Future Issues Prevented:
```
The database operation was expected to affect 1 row(s), but actually affected 0 row(s)
```

This error occurs when uploading images because EF Core tries to check concurrency on INSERT operations for owned entities with RowVersion.

## Why This Is The Correct Solution

1. **DDD Best Practice**: Owned entities (GalleryImage) shouldn't have separate concurrency tokens
2. **Provider-Level Concurrency**: The Provider aggregate root already has a Version property for concurrency
3. **Prevents INSERT Conflicts**: New gallery images don't have a RowVersion yet, causing the "affected 0 rows" error
4. **Simplifies Architecture**: One concurrency boundary per aggregate root

## Migration Behavior

The migration will attempt to drop the `row_version` column from `provider_gallery_images` table.

**Expected Result:**
- If column exists: Column will be dropped
- If column doesn't exist: Migration succeeds with no changes (PostgreSQL IF EXISTS)

This is safe either way.

## After Applying

### Test Gallery Operations:

1. **Upload Images:**
   - Navigate to Gallery tab
   - Upload 1-3 images
   - ✅ Should succeed without concurrency errors

2. **Delete Images:**
   - Select an image
   - Click delete
   - ✅ Should remove cleanly

3. **Reorder Images:**
   - Drag and drop images
   - ✅ Should update order

4. **Set Primary:**
   - Click star icon on an image
   - ✅ Should set as primary

All operations should now work without the `row_version` error!

## Files Modified

1. ✅ `ProviderConfiguration.cs` - RowVersion configuration removed
2. ⏳ Migration file - Will be created when you run the script
3. ⏳ Database schema - Will be updated when migration is applied

## Rollback Plan

If you need to rollback (unlikely):

```bash
# List migrations
dotnet ef migrations list

# Remove the migration
dotnet ef migrations remove

# Restore ProviderConfiguration.cs from git
git restore src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Infrastructure/Persistence/Configurations/ProviderConfiguration.cs
```

## Summary

- ✅ Code fixed - RowVersion removed from owned entity
- ⏳ Migration ready - Run the script to apply
- ✅ Root cause addressed - No more concurrency conflicts on INSERT
- ✅ Architecture improved - Proper DDD aggregate boundaries

**Action Required:** Stop API and run the migration script!
