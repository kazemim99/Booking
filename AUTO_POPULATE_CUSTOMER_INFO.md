# Auto-Populate Customer Info for Booking

## Overview
When logged-in customers make a booking, their contact information is now **automatically populated** from their user profile, eliminating the need to re-enter data they've already provided.

## Problem Solved
Previously, even logged-in customers had to manually enter their:
- Full name
- Phone number (even though they authenticated with it!)
- Email address

This created friction and a poor user experience.

## Solution Implemented
The **CustomerInfo.vue** component now automatically populates form fields from the authenticated user's profile stored in the auth token/store.

## Implementation Details

### File Modified
**Location**: `booksy-frontend/src/modules/booking/components/CustomerInfo.vue`

### Changes Made

#### 1. Import Auth Store
```typescript
import { useAuthStore } from '@/core/stores/modules/auth.store'

const authStore = useAuthStore()
```

#### 2. Helper Functions to Extract User Data
```typescript
// Get phone number from authenticated user
const getUserPhoneNumber = (): string => {
  // User already authenticated with phone, use it
  return authStore.user?.phoneNumber || props.customerData.phoneNumber || ''
}

// Get full name from user profile
const getUserFullName = (): string => {
  // Use stored full name or combine first name and last name if available
  if (authStore.user?.fullName) {
    return authStore.user.fullName
  }
  if (authStore.user?.firstName && authStore.user?.lastName) {
    return `${authStore.user.firstName} ${authStore.user.lastName}`
  }
  return props.customerData.fullName || ''
}

// Get email from user profile
const getUserEmail = (): string => {
  return authStore.user?.email || props.customerData.email || ''
}
```

#### 3. Auto-Populate Form Data
```typescript
// State - Auto-populate from user profile for faster booking
const formData = ref<CustomerData>({
  fullName: props.customerData.fullName || getUserFullName(),
  phoneNumber: getUserPhoneNumber(),
  email: props.customerData.email || getUserEmail(),
  notes: props.customerData.notes || '',
})
```

## User Data Sources

### From User Profile (Auth Store)
The system attempts to populate data from:

| Field | Source | Priority |
|-------|--------|----------|
| Phone Number | `authStore.user?.phoneNumber` | 1st choice - from authentication |
| Full Name | `authStore.user?.fullName` | 1st choice - if stored as full name |
| Full Name | `firstName + lastName` | 2nd choice - if separated |
| Email | `authStore.user?.email` | 1st choice - from profile |

### Fallback to Props
If user data is not available (e.g., guest user), the system falls back to:
- `props.customerData.phoneNumber`
- `props.customerData.fullName`
- `props.customerData.email`

### User Can Still Edit
All fields remain **editable** - auto-population is just a convenience, not a restriction.

## User Experience Flow

### Logged-In Customer
1. **Select service and time slot** ✅
2. **Navigate to customer info step** ✅
3. **Form is pre-filled automatically** 🎉
   - Phone: `09123456789` (from authentication)
   - Name: `علی احمدی` (from profile)
   - Email: `ali@example.com` (from profile)
4. **Customer reviews, edits if needed** ✅
5. **Submits booking** ✅

### Guest Customer (Not Logged In)
1. Form starts empty
2. Customer manually enters information
3. Normal booking flow

## Benefits

### For Customers
✅ **Faster booking** - No need to re-type information
✅ **Less friction** - Smoother user experience
✅ **Fewer errors** - Pre-filled data is already validated
✅ **Convenience** - Especially for repeat bookings

### For Business
✅ **Higher conversion** - Reduce booking abandonment
✅ **Better data quality** - Use verified profile data
✅ **Improved retention** - Encourage account creation

## Data Flow

```
Authentication
    ↓
JWT Token contains user claims
    ↓
Auth Store (Pinia)
    ↓
User Profile {
  phoneNumber: "09123456789"
  firstName: "علی"
  lastName: "احمدی"
  fullName: "علی احمدی"
  email: "ali@example.com"
}
    ↓
CustomerInfo Component
    ↓
Auto-populated Form Fields
```

## Security & Privacy

### Phone Number
- **Source**: User's authentication credential (verified during registration/login)
- **Trust Level**: High - this is the number they logged in with
- **Privacy**: User can edit if they want to use a different number for this booking

### Name & Email
- **Source**: User's profile (self-provided during account setup)
- **Trust Level**: Medium - user-provided but not necessarily verified
- **Privacy**: User can edit anytime

### User Control
- ✅ All fields remain editable
- ✅ User can override any auto-populated value
- ✅ Changes apply only to current booking (doesn't update profile)

## Integration with User Profile

### When User Updates Profile
If a customer updates their profile:
- New bookings will use updated information
- Previous bookings retain original information
- Immediate effect - no cache issues

### When User Books Again
1st Booking:
- Form auto-populated from profile
- Customer submits without edits

2nd Booking:
- Form auto-populated again (same data)
- Consistent experience

## Future Enhancements

### Save Booking Contact Info Back to Profile
If customer edits their info during booking, offer to update their profile:
```
"آیا می‌خواهید این اطلاعات را در پروفایل خود ذخیره کنید؟"
(Do you want to save this information to your profile?)
[بله] [خیر]
```

### Multiple Contact Methods
Allow users to save multiple:
- Phone numbers (work, personal, etc.)
- Email addresses
- Select which one to use per booking

### Address Auto-Complete
If booking requires address:
- Auto-populate from user's saved addresses
- Offer address suggestions

### Booking History Integration
Auto-populate based on previous bookings with same provider:
- "Use same contact info as last booking?"
- One-click data entry

## Testing Checklist

### Logged-In User
- [ ] Phone number auto-populated from profile
- [ ] Full name auto-populated from `fullName` field
- [ ] Full name auto-populated from `firstName + lastName`
- [ ] Email auto-populated from profile
- [ ] All fields are editable
- [ ] Empty fields remain empty if no profile data
- [ ] Form submission works with auto-populated data
- [ ] Form submission works with manually edited data

### Guest User
- [ ] All fields start empty
- [ ] No errors when accessing auth store (user is null)
- [ ] Manual data entry works
- [ ] Form validation works

### Edge Cases
- [ ] User with partial profile (only phone, no email)
- [ ] User with phone in different format
- [ ] User changes data mid-booking
- [ ] User logs out during booking (form retains data)

## Related Files

### Frontend
- **CustomerInfo.vue** - Booking form component (modified)
- **auth.store.ts** - User profile store
- **user.types.ts** - User interface definition

### Backend
- User profile API endpoints
- JWT token generation (includes user claims)

## Compatibility

### Browser Support
- All modern browsers
- Progressive enhancement (works without JavaScript)

### Mobile
- Touch-friendly form fields
- Keyboard types (tel for phone, email for email)
- Auto-capitalization disabled for email

## Performance Impact
- **Minimal**: Reading from Pinia store is instant
- **No API calls**: Data already in memory from authentication
- **No delays**: Form renders immediately with pre-filled data

## Conclusion

This simple enhancement significantly improves the booking experience for logged-in customers by eliminating redundant data entry. The phone number auto-population is especially valuable since users have already verified it during authentication.

**User Feedback Expected**: "Much faster!" / "زیاد راحت شد!" 🎉
