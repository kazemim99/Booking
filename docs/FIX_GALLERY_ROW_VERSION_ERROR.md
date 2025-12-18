# Fix: Gallery Row Version Column Missing

## Error
```
Npgsql.PostgresException (0x80004005): 42703: column p0.row_version does not exist
```

## Root Cause

The code configuration in `ProviderConfiguration.cs` expects a `row_version` column on the `provider_gallery_images` table, but it doesn't exist in the database.

```csharp
// ProviderConfiguration.cs line 172-175
galleryImage.Property<byte[]>("RowVersion")
    .IsConcurrencyToken()
    .HasColumnName("row_version")
    .ValueGeneratedOnAddOrUpdate();
```

This column was never created in the original migrations.

## Solution Options

### Option 1: Remove RowVersion from Code (Recommended)

This is the proper fix that we attempted earlier. Remove the concurrency token from the owned entity since Provider already has one.

**Steps:**

1. **Stop the API**

2. **Edit ProviderConfiguration.cs** - Remove the RowVersion configuration:
   ```csharp
   // REMOVE these lines (172-175):
   galleryImage.Property<byte[]>("RowVersion")
       .IsConcurrencyToken()
       .HasColumnName("row_version")
       .ValueGeneratedOnAddOrUpdate();
   ```

3. **Create migration:**
   ```bash
   cd c:\Repos\Booking\src\BoundedContexts\ServiceCatalog\Booksy.ServiceCatalog.Api

   dotnet ef migrations add RemoveGalleryImageRowVersion --context ServiceCatalogDbContext --project "../Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj" --output-dir "Persistence/Migrations"
   ```

4. **Apply migration** (this will do nothing since column doesn't exist):
   ```bash
   dotnet ef database update --context ServiceCatalogDbContext --project "../Booksy.ServiceCatalog.Infrastructure/Booksy.ServiceCatalog.Infrastructure.csproj"
   ```

5. **Start the API**

### Option 2: Add Missing Column to Database (Quick Fix)

Add the missing column to match the code configuration.

**Steps:**

1. **Connect to PostgreSQL:**
   ```bash
   psql -h localhost -U postgres -d ServiceCatalog
   ```

2. **Run SQL:**
   ```sql
   ALTER TABLE "ServiceCatalog"."provider_gallery_images"
   ADD COLUMN "row_version" bytea;
   ```

3. **Verify:**
   ```sql
   \d "ServiceCatalog"."provider_gallery_images"
   ```

4. **Exit psql:**
   ```
   \q
   ```

5. **Restart the API**

### Option 3: Use Migration Script (Alternative)

If you can't connect to psql directly, use the SQL script:

```bash
# Run the script (if you have psql command available)
psql -h localhost -U postgres -d ServiceCatalog -f c:\Repos\Booking\scripts\add_gallery_row_version.sql
```

Or apply it through pgAdmin or your database management tool.

## Recommended Solution

**Use Option 1** (Remove RowVersion from code) because:
- ✅ Owned entities shouldn't have separate concurrency tokens
- ✅ Provider-level concurrency is sufficient
- ✅ Prevents the upload concurrency error
- ✅ Follows DDD best practices

**Only use Option 2 or 3** if:
- You need a quick fix to get the app running immediately
- You'll implement Option 1 later as the proper solution

## Why This Happened

When you reverted the gallery changes, the code was restored to expect a `row_version` column that was defined in the configuration but never created in a migration. The original implementation had the column configured but the database schema never matched.

## Next Steps After Fix

Once you apply **Option 1** (recommended), you'll have:
- ✅ Code without GalleryImage RowVersion
- ✅ Database without row_version column
- ✅ Provider-level concurrency only
- ✅ No more "expected to affect 1 row(s)" errors on upload

## Files to Check

After applying Option 1:
- `ProviderConfiguration.cs` - RowVersion configuration removed
- Migration generated - Will attempt to drop column (but it doesn't exist, so harmless)
- Database - No row_version column on provider_gallery_images
- Code - Works correctly with Provider-level concurrency only
