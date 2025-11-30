# Profile Image Null Bug Fix

## Problem Summary

When calling `GetProviderById`, the `provider.Profile.ProfileImageUrl` was returning `null` even though the value existed in the database. This happened after updating the business profile (name/description) without explicitly providing a new profile image.

---

## Root Cause

The `UpdateBusinessProfile()` method in the Provider aggregate was **not preserving the existing `ProfileImageUrl`** when updating the profile.

### The Bug

**File:** `src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Domain/Aggregates/ProviderAggregate/Provider.cs`

**Before (BROKEN):**
```csharp
public void UpdateBusinessProfile(string businessName, string description, string? profileImageUrl)
{
    // ✅ Preserves LogoUrl
    var existingLogoUrl = Profile.LogoUrl;

    // ❌ Does NOT preserve ProfileImageUrl!
    // If profileImageUrl is null, it ERASES the existing profile image!
    Profile = BusinessProfile.Create(
        businessName,
        description,
        logoUrl: existingLogoUrl,
        profileImageUrl: profileImageUrl);  // ← BUG: null overwrites existing value!

    RaiseDomainEvent(new BusinessProfileUpdatedEvent(Id, businessName, description, DateTime.UtcNow));
}
```

### The Problem Flow

1. Provider is created with `ProfileImageUrl = "https://example.com/profile.jpg"` ✅
2. Data is saved to database → `ProfileImageUrl` column has the URL ✅
3. User updates business name/description via API
4. API calls `UpdateBusinessProfile(newName, newDescription, null)`
   - **`profileImageUrl` parameter is `null`** ❌
5. Method creates NEW BusinessProfile with `ProfileImageUrl = null` ❌
6. EF Core saves the change → Database `ProfileImageUrl` becomes `null` ❌
7. Next API call returns `ProfileImageUrl = null` ❌

---

## The Fix

### Preserve Existing ProfileImageUrl

```csharp
public void UpdateBusinessProfile(string businessName, string description, string? profileImageUrl)
{
    // ✅ Preserve both LogoUrl AND ProfileImageUrl
    var existingLogoUrl = Profile.LogoUrl;
    var existingProfileImageUrl = Profile.ProfileImageUrl;

    // ✅ Only update ProfileImageUrl if a new one is provided, otherwise keep existing
    var updatedProfileImageUrl = profileImageUrl ?? existingProfileImageUrl;

    Profile = BusinessProfile.Create(
        businessName,
        description,
        logoUrl: existingLogoUrl,
        profileImageUrl: updatedProfileImageUrl);  // ✅ Preserves existing value!

    RaiseDomainEvent(new BusinessProfileUpdatedEvent(Id, businessName, description, DateTime.UtcNow));
}
```

### Key Changes:

1. ✅ Capture existing `ProfileImageUrl` before creating new BusinessProfile
2. ✅ Use null-coalescing operator: `profileImageUrl ?? existingProfileImageUrl`
3. ✅ Only update `ProfileImageUrl` when a new value is explicitly provided

---

## How It Works Now

### Scenario 1: Update Business Name/Description Only

```csharp
// Provider has ProfileImageUrl = "https://example.com/profile.jpg"
provider.UpdateBusinessProfile("New Business Name", "New Description", null);

// Result:
// - BusinessName = "New Business Name" ✅
// - BusinessDescription = "New Description" ✅
// - ProfileImageUrl = "https://example.com/profile.jpg" ✅ PRESERVED!
```

### Scenario 2: Update Profile Image

```csharp
// Provider has ProfileImageUrl = "https://example.com/old-profile.jpg"
provider.UpdateBusinessProfile("Same Name", "Same Description", "https://example.com/new-profile.jpg");

// Result:
// - BusinessName = "Same Name"
// - BusinessDescription = "Same Description"
// - ProfileImageUrl = "https://example.com/new-profile.jpg" ✅ UPDATED!
```

### Scenario 3: Use Dedicated Method for Profile Image

```csharp
// Better approach: Use the dedicated method for updating profile image
profile.UpdateProfileImage("https://example.com/new-profile.jpg");

// This method is specifically designed for updating the profile image
// and doesn't touch other properties
```

---

## Why This Bug Happened

### Context: Owned Entities in EF Core

`BusinessProfile` is an **owned entity** in EF Core:

```csharp
// ProviderConfiguration.cs
builder.OwnsOne(p => p.Profile, profile => {
    profile.Property(bp => bp.ProfileImageUrl)
        .HasColumnName("ProfileImageUrl")
        .HasMaxLength(500);
    // ...
});
```

When you assign a new `BusinessProfile` instance:
```csharp
Profile = BusinessProfile.Create(...);  // NEW INSTANCE!
```

EF Core treats this as a **complete replacement** of the owned entity, so all properties get overwritten, not merged!

### Why LogoUrl Worked But ProfileImageUrl Didn't

```csharp
// Original code:
var existingLogoUrl = Profile.LogoUrl;  // ✅ Saved before replacement
Profile = BusinessProfile.Create(
    businessName,
    description,
    logoUrl: existingLogoUrl,        // ✅ Passed back in
    profileImageUrl: profileImageUrl // ❌ null gets written!
);
```

The developer remembered to preserve `LogoUrl` but forgot `ProfileImageUrl`!

---

## Alternative Approaches (Not Implemented)

### Option 1: Update Properties Instead of Replacing Object

Instead of creating a NEW `BusinessProfile`, update properties on the existing one:

```csharp
public void UpdateBusinessProfile(string businessName, string description)
{
    Profile.BusinessName = businessName;
    Profile.BusinessDescription = description;
    Profile.LastUpdatedAt = DateTime.UtcNow;
}
```

**Pros:**
- No risk of losing properties
- Clearer intent

**Cons:**
- `BusinessProfile` properties are `private set` (immutability)
- Would require changing domain model design

### Option 2: Separate Methods for Each Property

```csharp
provider.UpdateBusinessName("New Name");
provider.UpdateBusinessDescription("New Description");
provider.Profile.UpdateProfileImage("new-url.jpg");  // ✅ Already exists!
provider.Profile.UpdateLogo("new-logo.jpg");         // ✅ Already exists!
```

**Pros:**
- Fine-grained control
- No accidental overwrites

**Cons:**
- More API calls for bulk updates
- More methods to maintain

---

## Testing

### Before Fix
```sql
-- Initial state
SELECT ProfileImageUrl FROM ServiceCatalog.Providers WHERE Id = '...';
-- Result: 'https://example.com/profile.jpg'

-- After calling UpdateBusinessProfile(name, description, null)
SELECT ProfileImageUrl FROM ServiceCatalog.Providers WHERE Id = '...';
-- Result: NULL  ❌ BUG!
```

### After Fix
```sql
-- Initial state
SELECT ProfileImageUrl FROM ServiceCatalog.Providers WHERE Id = '...';
-- Result: 'https://example.com/profile.jpg'

-- After calling UpdateBusinessProfile(name, description, null)
SELECT ProfileImageUrl FROM ServiceCatalog.Providers WHERE Id = '...';
-- Result: 'https://example.com/profile.jpg'  ✅ PRESERVED!
```

---

## Related Files

### Modified
- ✅ `Provider.cs:200-212` - Fixed `UpdateBusinessProfile()` method

### Related (No Changes)
- `BusinessProfile.cs` - Owned entity with ProfileImageUrl property
- `ProviderConfiguration.cs` - EF Core configuration for owned entity
- `GetProviderByIdQueryHandler.cs` - Query that returns ProfileImageUrl
- `UpdateBusinessInfoCommandHandler.cs` - Command that calls UpdateBusinessProfile()

---

## Prevention

### Code Review Checklist

When working with owned entities that get replaced:

- [ ] List ALL properties of the owned entity
- [ ] Verify EACH property is either:
  - [ ] Preserved from existing instance, OR
  - [ ] Intentionally updated with new value
- [ ] Check for null-coalescing patterns: `newValue ?? existingValue`
- [ ] Add unit tests for property preservation

### Example Unit Test

```csharp
[Fact]
public void UpdateBusinessProfile_ShouldPreserveExistingProfileImageUrl()
{
    // Arrange
    var provider = CreateProviderWithProfileImage("https://example.com/profile.jpg");

    // Act
    provider.UpdateBusinessProfile("New Name", "New Description", null);

    // Assert
    provider.Profile.ProfileImageUrl.Should().Be("https://example.com/profile.jpg");
}
```

---

## Summary

✅ **FIXED:** `UpdateBusinessProfile()` now preserves existing `ProfileImageUrl` when null is passed
✅ **FIXED:** Profile images no longer get erased when updating business name/description
✅ **IMPROVED:** Consistent behavior for both `LogoUrl` and `ProfileImageUrl`

The fix ensures that existing profile images are preserved unless explicitly updated with a new value! 🎉
