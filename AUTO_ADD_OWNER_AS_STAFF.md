# Auto-Add Owner as Staff for Individual Providers

## Overview
This document explains the implementation of automatically adding the provider owner as a staff member during registration for Individual/solo provider types.

## Problem Solved
Previously, when a solo provider registered, they had to manually add themselves as a staff member before they could offer services or receive bookings. This created a poor user experience and a potential edge case where providers had no staff members, resulting in no available time slots.

## Solution
When an Individual provider registers, the system now automatically adds the owner as a staff member with the role of "ServiceProvider". This ensures:
- ✅ Solo providers can immediately start receiving bookings
- ✅ No "empty staff" edge case for Individual providers
- ✅ Better onboarding experience
- ✅ Reduced setup friction

## Implementation Details

### Backend Changes

#### 1. Updated RegisterProviderCommand
**File**: [RegisterProviderCommand.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommand.cs)

Added owner name parameters:
```csharp
public sealed record RegisterProviderCommand(
    Guid OwnerId,
    string OwnerFirstName,      // NEW
    string OwnerLastName,       // NEW
    string BusinessName,
    string Description,
    // ... other parameters
) : ICommand<RegisterProviderResult>
```

**Why**: The system needs the owner's name to create a proper staff entry.

#### 2. Updated RegisterProviderCommandHandler
**File**: [RegisterProviderCommandHandler.cs:91-108](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommandHandler.cs#L91-L108)

Added logic to automatically create staff entry:
```csharp
// For Individual/solo providers, automatically add owner as staff member
if (request.ProviderType == Domain.Enums.ProviderType.Individual)
{
    var ownerPhone = !string.IsNullOrEmpty(request.PrimaryPhone)
        ? PhoneNumber.Create(request.PrimaryPhone)
        : null;

    provider.AddStaff(
        request.OwnerFirstName,
        request.OwnerLastName,
        Domain.Enums.StaffRole.ServiceProvider,
        ownerPhone);

    _logger.LogInformation(
        "Automatically added owner {OwnerName} as staff for Individual provider {ProviderId}",
        $"{request.OwnerFirstName} {request.OwnerLastName}",
        provider.Id);
}
```

**Logic Flow**:
1. Check if `ProviderType == Individual`
2. Extract owner's phone number from request
3. Call `provider.AddStaff()` with owner's details
4. Assign `StaffRole.ServiceProvider` role
5. Log the action for audit trail

**When**: Executes immediately after provider aggregate is created, before saving to database.

#### 3. Updated API Request Model
**File**: [RegisterProviderRequest.cs:17-29](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Requests/RegisterProviderRequest.cs#L17-L29)

Added validation for owner name fields:
```csharp
/// <summary>
/// Owner's first name
/// </summary>
[Required(ErrorMessage = "Owner first name is required")]
[StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
public string OwnerFirstName { get; set; } = string.Empty;

/// <summary>
/// Owner's last name
/// </summary>
[Required(ErrorMessage = "Owner last name is required")]
[StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
public string OwnerLastName { get; set; } = string.Empty;
```

**Validation Rules**:
- Both fields are required
- Minimum 2 characters
- Maximum 100 characters
- Prevents empty/invalid names

#### 4. Updated API Controller Mapping
**File**: [ProvidersController.cs:869-890](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs#L869-L890)

Updated command mapping to include owner names:
```csharp
private RegisterProviderCommand MapToRegisterCommand(RegisterProviderRequest request)
{
    return new RegisterProviderCommand(
        OwnerId: request.OwnerId,
        OwnerFirstName: request.OwnerFirstName,      // NEW
        OwnerLastName: request.OwnerLastName,        // NEW
        BusinessName: request.BusinessName,
        // ... other mappings
    );
}
```

### Frontend Impact

The frontend registration form will need to be updated to collect and send the owner's name:

```typescript
interface RegisterProviderRequest {
  ownerId: string
  ownerFirstName: string    // NEW - Add input field
  ownerLastName: string     // NEW - Add input field
  businessName: string
  description: string
  // ... other fields
}
```

**Required Frontend Changes**:
1. Add "Owner First Name" input field
2. Add "Owner Last Name" input field
3. Add validation (required, 2-100 chars)
4. Include in API request payload

## Behavior by Provider Type

### Individual Providers
**Before Registration**:
- User fills registration form including their own name
- Submits provider registration

**After Registration**:
- Provider aggregate created
- **Owner automatically added as staff** ✅
- Staff entry includes:
  - First Name: From owner
  - Last Name: From owner
  - Role: ServiceProvider
  - Phone: From contact info
  - Status: Active

**Result**: Provider can immediately:
- Add services
- Assign themselves to services
- Generate availability slots
- Receive bookings

### Non-Individual Providers (Salon, Clinic, etc.)
**Behavior**: No automatic staff creation

**Reason**: Multi-staff businesses need to explicitly manage their team members through the staff management interface.

**Next Steps**: Owner manually adds staff through provider dashboard.

## Staff Properties

The automatically created staff entry has these properties:

| Property | Value | Source |
|----------|-------|--------|
| FirstName | Owner's first name | `request.OwnerFirstName` |
| LastName | Owner's last name | `request.OwnerLastName` |
| Role | ServiceProvider | Hardcoded |
| Phone | Owner's phone | `request.PrimaryPhone` |
| ProviderId | Provider ID | From created provider |
| IsActive | true | Default |
| IsDeleted | false | Default |

## Integration with Availability System

The auto-created staff member integrates seamlessly with the availability system:

1. **Slot Generation**:
   - `GetQualifiedStaff` finds the auto-created owner staff
   - Slots are generated for the owner's availability
   - No "no staff" edge case occurs

2. **Service Assignment**:
   - Owner can assign themselves to services
   - `QualifiedStaff` array can include the owner's staff ID
   - Bookings can be made immediately

3. **Fallback Logic** (Still Active):
   - If Individual provider has no qualified staff for a specific service
   - System falls back to all active staff (the owner)
   - Double protection against edge cases

## Migration Considerations

### Existing Providers
Providers who registered before this feature:
- **Not affected** - No retroactive changes
- Can still manually add themselves as staff
- Existing fallback logic handles their case

### New Providers
Providers registering after deployment:
- **Automatically get staff entry** for Individual type
- Better onboarding experience
- Immediate ability to receive bookings

## Testing

### Test Scenarios

#### Test 1: Individual Provider Registration
```
Given: User registers as Individual provider
When: Registration completes successfully
Then: Provider should have 1 staff member (the owner)
And: Staff name should match owner name
And: Staff role should be ServiceProvider
And: Staff should be active
```

#### Test 2: Salon Provider Registration
```
Given: User registers as Salon provider
When: Registration completes successfully
Then: Provider should have 0 staff members
And: Owner must manually add staff later
```

#### Test 3: Availability Check for New Individual Provider
```
Given: Individual provider just registered
And: Provider added a service
When: Customer checks availability
Then: Slots should be available
And: Slots should show owner as staff
```

## API Contract Changes

### Breaking Changes
⚠️ **Breaking Change**: The `RegisterProvider` API now requires `ownerFirstName` and `ownerLastName` fields.

**Migration Path for API Clients**:
```json
// OLD (will fail validation)
{
  "ownerId": "guid",
  "businessName": "My Salon",
  ...
}

// NEW (required)
{
  "ownerId": "guid",
  "ownerFirstName": "Ali",
  "ownerLastName": "Rezaei",
  "businessName": "My Salon",
  ...
}
```

### API Response
No changes to response structure - staff count for Individual providers will be 1 instead of 0.

## Logging

The system logs the following for Individual provider registrations:

```
[Information] Automatically added owner Ali Rezaei as staff for Individual provider {ProviderId}
```

This helps with:
- Audit trail
- Debugging onboarding issues
- Analytics on provider types

## Security Considerations

✅ **Secure**:
- Owner ID verified from authenticated JWT token
- Cannot create staff for other users' providers
- Standard validation applies to names
- Staff role restricted to ServiceProvider

## Future Enhancements

### Potential Improvements:
1. **Make staff role configurable**: Allow owners to choose their role during registration
2. **Auto-assign to services**: When owner adds first service, auto-assign owner staff
3. **Profile photo sync**: If user has profile photo, use it for staff entry
4. **Biography generation**: Auto-generate staff bio from business description
5. **Skill tags**: Prompt owner to add their specializations during registration

### Dashboard Notification
Consider adding UI notification:
> "You've been automatically added as a service provider. You can manage your profile in Staff Settings."

## Benefits Summary

1. **Better UX**: Solo providers don't need to understand "staff" concept immediately
2. **Fewer Edge Cases**: Eliminates "no staff" scenario for Individual providers
3. **Faster Time-to-Value**: Providers can receive bookings immediately after registration
4. **Reduced Support**: Fewer "why can't I receive bookings?" questions
5. **Data Integrity**: Ensures all Individual providers have at least one staff member

## Related Documentation
- [SOLO_PROVIDER_HANDLING.md](SOLO_PROVIDER_HANDLING.md) - How system handles providers without staff
- [SOLO_PROVIDER_IMPLEMENTATION.md](SOLO_PROVIDER_IMPLEMENTATION.md) - Validation messages implementation

## Files Modified

1. [RegisterProviderCommand.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommand.cs) - Added owner name parameters
2. [RegisterProviderCommandHandler.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Application/Commands/Provider/RegisterProvider/RegisterProviderCommandHandler.cs) - Auto-add staff logic
3. [RegisterProviderRequest.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Models/Requests/RegisterProviderRequest.cs) - Added name fields
4. [ProvidersController.cs](src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api/Controllers/V1/ProvidersController.cs) - Updated mapping

## Build Status
✅ Backend builds successfully with 0 errors
✅ All changes are type-safe
✅ Backward compatible for existing providers
⚠️ Breaking change for API clients (requires frontend update)
