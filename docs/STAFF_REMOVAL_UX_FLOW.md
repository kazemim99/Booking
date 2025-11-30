# Staff Removal - UX Flow & Navigation Changes

## Overview
This document explains what happens to a staff member when they are removed from an organization, including the complete UX flow and navigation changes.

---

## 🔄 What Happens When Staff is Removed

### Backend Process

When an organization removes a staff member, the following happens in the backend:

**File:** `RemoveStaffMemberCommandHandler.cs` (line 56)

```csharp
// Unlink staff member
staffProvider.UnlinkFromOrganization(request.Reason);
```

**File:** `Provider.cs` (lines 680-690)

```csharp
public void UnlinkFromOrganization(string reason)
{
    if (ParentProviderId == null)
        throw new InvalidProviderException("Provider is not linked to any organization");

    var parentId = ParentProviderId;
    ParentProviderId = null;           // ← CLEARS PARENT LINK
    IsIndependent = true;              // ← BECOMES INDEPENDENT

    RaiseDomainEvent(new StaffMemberRemovedFromOrganizationEvent(
        parentId, Id, reason, DateTime.UtcNow));
}
```

### What Changes for the Staff Member

| Property | Before Removal | After Removal |
|----------|---------------|---------------|
| `hierarchyType` | `"Individual"` | `"Individual"` (unchanged) |
| `parentProviderId` | `<org-guid>` | `null` ✅ |
| `isIndependent` | `false` | `true` ✅ |

---

## 📊 UX Transformation

### BEFORE Removal (Staff Member)

**Role Badge:** `کارمند` (Yellow badge)

**Navigation Menu:**
```
├── 📊 داشبورد (Dashboard)
├── 📅 رزروهای من (My Bookings) - Only their bookings
├── 💰 درآمد من (My Earnings) - Only their earnings
├── 👤 پروفایل من (My Profile)
└── 🏢 سازمان من (My Organization) - Read-only view
```

**Access:**
- ✅ Can view their personal bookings
- ✅ Can view their personal earnings
- ✅ Can view organization details (read-only)
- ✅ Can see other team members
- ❌ CANNOT manage staff
- ❌ CANNOT edit organization settings

### AFTER Removal (Independent Individual)

**Role Badge:** `فردی` (Purple badge)

**Navigation Menu:**
```
├── 📊 داشبورد (Dashboard)
├── 📅 رزروها (Bookings) - All their bookings
├── 💰 مالی (Financial) - Full financial control
└── 👤 پروفایل من (My Profile)
```

**Access:**
- ✅ Can view all their bookings
- ✅ Full control over finances
- ✅ Can edit their own profile
- ✅ Can manage their own services
- ✅ Can convert to organization (if they want to hire staff later)
- ❌ No longer sees "My Organization"
- ❌ No longer restricted to "their earnings only"

---

## 🎭 Step-by-Step UX Flow

### 1. Organization Manager Removes Staff

**Action:** Manager clicks "Remove Staff" on staff card

```
Organization Dashboard
  └── Staff Management
      └── Staff Member Card: "احمد رضایی"
          └── Three-dot menu (⋮)
              └── "حذف کارمند" (Remove Staff) ← CLICK
```

**Confirmation Dialog:**
```
┌─────────────────────────────────────┐
│  حذف کارمند                         │
├─────────────────────────────────────┤
│  آیا مطمئن هستید که می‌خواهید       │
│  احمد رضایی را حذف کنید؟            │
│                                     │
│  [انصراف]    [حذف کارمند]          │
└─────────────────────────────────────┘
```

### 2. Backend Processes Removal

```javascript
DELETE /api/v1/providers/{orgId}/hierarchy/staff/{staffId}
Body: { "reason": "Removed by organization" }

Backend executes:
  1. Validate organization exists
  2. Validate staff member exists
  3. Validate staff is linked to this org
  4. Call: staffProvider.UnlinkFromOrganization(reason)
     - Sets ParentProviderId = null
     - Sets IsIndependent = true
  5. Save to database
  6. Raise domain event: StaffMemberRemovedFromOrganizationEvent
```

### 3. Staff Member's Session Updates

**If staff member is currently logged in:**

#### Option A: Immediate Update (if using real-time sync)
- Hierarchy store automatically refreshes
- Navigation menu updates immediately
- Role badge changes from "کارمند" to "فردی"
- "My Organization" menu item disappears
- Toast notification: "شما از سازمان حذف شدید"

#### Option B: Next Page Load (current implementation)
- Staff member continues using current session
- On next page refresh or navigation:
  - `loadProviderHierarchy()` is called
  - API returns updated data: `isIndependent: true`, `parentProviderId: null`
  - Navigation menu updates
  - Role badge changes

### 4. Staff Member Sees Changed Dashboard

**Before:**
```
┌────────────────────────────────────┐
│ 👤 احمد رضایی  [کارمند] ←Yellow    │
├────────────────────────────────────┤
│ 📊 داشبورد                         │
│ 📅 رزروهای من                      │
│ 💰 درآمد من                        │
│ 👤 پروفایل من                      │
│ 🏢 سازمان من                       │ ← Organization view
└────────────────────────────────────┘
```

**After:**
```
┌────────────────────────────────────┐
│ 👤 احمد رضایی  [فردی] ←Purple      │
├────────────────────────────────────┤
│ 📊 داشبورد                         │
│ 📅 رزروها                          │ ← Full bookings
│ 💰 مالی                            │ ← Full financial
│ 👤 پروفایل من                      │
└────────────────────────────────────┘
```

---

## 🔐 Security & Access Changes

### Routes That Become Inaccessible

After removal, the staff member can NO LONGER access:

```typescript
❌ /provider/my-bookings     → Redirects to /provider/bookings
❌ /provider/my-earnings     → Redirects to /provider/financial
❌ /provider/my-organization → 404 or Forbidden (no parent org)
```

The route guards will detect:
- `hierarchyType: "Individual"` ✅
- `parentProviderId: null` ✅
- `isIndependent: true` ✅

And redirect them accordingly.

### Routes That Become Accessible

Now they can access full provider routes:

```typescript
✅ /provider/bookings        → All their bookings (no filtering)
✅ /provider/financial       → Full financial dashboard
✅ /provider/services        → Manage their services
✅ /provider/convert-to-organization → Can become an org
```

---

## 💾 Data Preservation

### What is KEPT After Removal

✅ **All their data is preserved:**
- Profile information (name, bio, photo)
- Services they offer
- Working hours
- Gallery/portfolio
- Reviews and ratings
- Booking history
- Financial history

### What is LOST After Removal

❌ **Organizational context:**
- Link to parent organization
- Organization membership status
- Access to organization resources
- View of other team members
- Organization-specific settings

---

## 🎯 Business Logic Rules

### Can They Rejoin?

**Yes!** The removed staff member can:

1. **Accept a NEW invitation** from the same or different organization
   - They will become a staff member again
   - `isIndependent` changes back to `false`
   - `parentProviderId` is set to the new organization

2. **Send a JOIN REQUEST** to an organization
   - If approved, they become a staff member
   - Same effect as accepting an invitation

3. **Convert to ORGANIZATION**
   - If they want to hire their own staff
   - `hierarchyType` changes to `"Organization"`
   - Can now send invitations to others

### Can They Keep Their Bookings?

**Yes!** All bookings remain with the individual:
- Past bookings are preserved
- Future bookings are still valid
- Customers can still book with them (if they're active)
- They just operate independently now instead of under the organization

---

## 📱 User Notifications

### Recommended Notifications

**For the Removed Staff Member:**
```
┌─────────────────────────────────────────┐
│  ⚠️ تغییر وضعیت حساب                    │
├─────────────────────────────────────────┤
│  شما دیگر عضو سازمان "آرایشگاه رز"     │
│  نیستید.                                │
│                                         │
│  اکنون به عنوان ارائه‌دهنده مستقل      │
│  فعالیت می‌کنید و کنترل کامل حساب      │
│  خود را دارید.                         │
│                                         │
│  [متوجه شدم]                            │
└─────────────────────────────────────────┘
```

**For the Organization:**
```
┌─────────────────────────────────────────┐
│  ✅ کارمند با موفقیت حذف شد             │
├─────────────────────────────────────────┤
│  احمد رضایی دیگر عضو سازمان شما نیست.  │
└─────────────────────────────────────────┘
```

---

## 🧪 Testing Scenarios

### Test Case 1: Remove and Verify Navigation Change

1. Login as staff member (linked to org)
2. Verify menu shows "My Bookings", "My Earnings", "My Organization"
3. Have organization manager remove you
4. Refresh page
5. ✅ Verify menu changes to "Bookings", "Financial"
6. ✅ Verify "My Organization" is gone
7. ✅ Verify role badge changed from "کارمند" to "فردی"

### Test Case 2: Route Access After Removal

1. Login as staff member
2. Navigate to `/provider/my-organization`
3. ✅ Should work (shows org details)
4. Get removed by organization
5. Try to access `/provider/my-organization` again
6. ✅ Should redirect to Forbidden or Dashboard
7. Try to access `/provider/bookings`
8. ✅ Should work (shows all bookings)

### Test Case 3: Re-invitation After Removal

1. Staff member gets removed
2. Becomes independent (`isIndependent: true`)
3. Organization sends new invitation
4. Staff member accepts
5. ✅ Becomes staff member again
6. ✅ Menu changes back to staff view
7. ✅ Role badge changes back to "کارمند"

### Test Case 4: Data Preservation

1. Staff member has:
   - 5 services
   - 20 past bookings
   - 10 future bookings
   - 4.8 star rating
2. Gets removed from organization
3. ✅ All services still exist
4. ✅ All bookings still visible
5. ✅ Rating preserved
6. ✅ Can continue accepting new bookings

---

## 🚨 Edge Cases & Handling

### Edge Case 1: Active Bookings During Removal

**Scenario:** Staff has upcoming bookings with customers

**Handling:**
- Bookings remain valid
- Staff can still fulfill them
- Customers are NOT notified of the change
- Staff operates independently for these bookings

**Recommendation:** Organization should coordinate with staff before removal

### Edge Case 2: Staff Logged In During Removal

**Scenario:** Staff is actively using the dashboard when removed

**Current Behavior:**
- Session continues normally
- Changes take effect on next page load/refresh

**Recommended Enhancement:**
- Implement WebSocket connection
- Send real-time event: `STAFF_REMOVED`
- Show notification immediately
- Force navigation menu refresh

### Edge Case 3: Pending Bookings Assigned to Staff

**Scenario:** Organization has bookings assigned to the staff member

**Handling:**
- Bookings should be reassigned to another staff or organization owner
- Prevent removal if pending bookings exist
- Show warning: "این کارمند رزروهای آتی دارد. ابتدا رزروها را منتقل کنید."

### Edge Case 4: Staff Was the Only Member

**Scenario:** Organization removes their only staff member

**Handling:**
- Organization continues to exist
- `staffCount` becomes 0
- Organization can operate solo
- Can hire new staff later

---

## 📋 Implementation Checklist

### Backend (Already Implemented ✅)
- [x] `UnlinkFromOrganization()` method
- [x] Sets `ParentProviderId = null`
- [x] Sets `IsIndependent = true`
- [x] Raises domain event
- [x] Command handler for removal

### Frontend (Already Implemented ✅)
- [x] Role-based navigation
- [x] Independent individual menu
- [x] Route guards
- [x] Hierarchy store updates
- [x] Role badge indicators

### Future Enhancements (Recommended)
- [ ] Real-time notification on removal
- [ ] Confirmation email to removed staff
- [ ] Graceful handling of active bookings
- [ ] Option to transfer services to organization
- [ ] Removal history/audit log
- [ ] Re-invitation cooldown period
- [ ] Exit interview/feedback form

---

## 🎓 Summary

### Before Removal
```
Staff Member (کارمند)
├── hierarchyType: "Individual"
├── isIndependent: false
├── parentProviderId: <org-guid>
└── Navigation: Limited to personal data + org view
```

### After Removal
```
Independent Individual (فردی)
├── hierarchyType: "Individual"
├── isIndependent: true
├── parentProviderId: null
└── Navigation: Full control, no org features
```

### Key Takeaways

1. **Data is Preserved** - Staff keeps all their services, bookings, and profile
2. **Independence Gained** - Staff becomes fully independent provider
3. **Navigation Changes** - Menu updates to independent individual view
4. **Access Expanded** - Gets full financial and booking access
5. **Can Rejoin** - Can accept new invitations or send join requests
6. **Can Upgrade** - Can convert to organization later

**Perfect UX:** The transition is seamless, and the staff member automatically gets the appropriate dashboard for their new status! 🎉

---

## 📞 Support

For questions about staff removal:
1. Check this documentation
2. Review [ROLE_BASED_NAVIGATION_IMPLEMENTATION.md](./ROLE_BASED_NAVIGATION_IMPLEMENTATION.md)
3. Test in development environment first
4. Verify hierarchy data updates correctly
