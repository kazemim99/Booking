# Staff Member Profile Tabs - Implementation Guide

## Overview
This document describes the staff member profile tab structure, which mirrors the organization profile layout but with appropriate read-only restrictions for business information and location data.

---

## 🎯 Design Philosophy

**Goal:** Staff members should have a professional profile interface similar to organizations, but with clear visibility into organization data (read-only) and their own personal settings (editable).

**UX Principle:** "Same structure, different permissions"

---

## 📑 Tab Structure

### Complete Tab List

Staff members see **5 tabs** (same as organizations):

```
┌─────────────────────────────────────────────┐
│  پروفایل من │ خدمات │ موقعیت │ ساعات کاری │ گالری │
└─────────────────────────────────────────────┘
```

1. **پروفایل من (Personal)** - Personal information
2. **خدمات (Business)** - Organization business info (**READ-ONLY**)
3. **موقعیت (Location)** - Organization location (**READ-ONLY**)
4. **ساعات کاری (Hours)** - Working hours (managed by org)
5. **گالری (Gallery)** - Personal portfolio (coming soon)

---

## 📋 Tab Details

### 1️⃣ پروفایل من (Personal Tab)

**Purpose:** Staff member's personal information

**Status:** Read-only (editing coming soon)

**Fields:**
- ✅ Profile Image (upload coming soon)
- ✅ First Name
- ✅ Last Name
- ✅ Email
- ✅ Phone Number
- ✅ Bio/About Me

**Notice:**
```
ℹ️ شما می‌توانید اطلاعات شخصی خود را ویرایش کنید.
```

**Coming Soon Badge:**
```
✨ قابلیت ویرایش به زودی فعال می‌شود
```

**Example:**
```vue
<input v-model="personalForm.firstName" disabled />
<input v-model="personalForm.email" disabled />
<textarea v-model="personalForm.bio" disabled />
```

---

### 2️⃣ خدمات (Business Tab) - **READ-ONLY**

**Purpose:** View organization's business information

**Status:** Read-only (**cannot edit**)

**Data Source:** `parentOrganization` from hierarchy store

**Fields:**
- 🔒 Organization Logo (display only)
- 🔒 Organization Name (disabled input)
- 🔒 Business Type (disabled input)

**Warning Notice:**
```
⚠️ این اطلاعات فقط قابل مشاهده است و توسط مدیر سازمان قابل ویرایش است.
```

**Example:**
```vue
<div class="info-notice warning">
  <p>این اطلاعات فقط قابل مشاهده است و توسط مدیر سازمان قابل ویرایش است.</p>
</div>

<input :value="parentOrganization.businessName" disabled readonly />
<input :value="getBusinessTypeLabel(parentOrganization.businessType)" disabled readonly />
```

**Visual:**
```
┌─────────────────────────────────────┐
│ اطلاعات کسب‌وکار سازمان              │
├─────────────────────────────────────┤
│ ⚠️ این اطلاعات فقط قابل مشاهده است │
│                                     │
│ [Logo Image - Read-only]            │
│                                     │
│ نام سازمان: آرایشگاه رز [disabled]  │
│ نوع: سالن زیبایی [disabled]         │
└─────────────────────────────────────┘
```

---

### 3️⃣ موقعیت (Location Tab) - **READ-ONLY**

**Purpose:** View organization's physical location

**Status:** Read-only (**cannot edit**)

**Data Source:** `parentOrganization` from hierarchy store

**Fields:**
- 🔒 City (disabled input)
- 🔒 State/Province (disabled input)
- 🔒 Map placeholder (coming soon)

**Warning Notice:**
```
⚠️ موقعیت مکانی سازمان {{ organizationName }}. این اطلاعات فقط قابل مشاهده است.
```

**Help Text:**
```
💡 برای دیدن اطلاعات کامل سازمان، به بخش "سازمان من" مراجعه کنید.
```

**Example:**
```vue
<div class="info-notice warning">
  <p>موقعیت مکانی سازمان {{ organizationName }}. این اطلاعات فقط قابل مشاهده است.</p>
</div>

<input :value="parentOrganization.city" disabled readonly />
<input :value="parentOrganization.state" disabled readonly />

<div class="map-placeholder">
  <svg class="map-icon">...</svg>
  <p>نمایش نقشه به زودی فعال می‌شود</p>
</div>
```

**Visual:**
```
┌─────────────────────────────────────┐
│ موقعیت مکانی سازمان                 │
├─────────────────────────────────────┤
│ ⚠️ این اطلاعات فقط قابل مشاهده است │
│                                     │
│ شهر: تهران [disabled]               │
│ استان: تهران [disabled]             │
│                                     │
│ ┌─────────────────────────────┐     │
│ │     📍 Map Placeholder      │     │
│ │  نمایش نقشه به زودی...     │     │
│ └─────────────────────────────┘     │
│                                     │
│ 💡 برای اطلاعات کامل، "سازمان من"  │
└─────────────────────────────────────┘
```

---

### 4️⃣ ساعات کاری (Working Hours Tab)

**Purpose:** Staff working hours management

**Status:** Managed by organization (read-only for now)

**Notice:**
```
ℹ️ ساعات کاری شما در سازمان.
```

**Message:**
```
📅 مدیریت ساعات کاری
ساعات کاری شما توسط مدیر سازمان تنظیم می‌شود.
برای تغییر، با مدیر سازمان هماهنگ کنید.
```

**Future Implementation:**
- Display current working hours
- Integration with organization schedule
- Request changes feature

---

### 5️⃣ گالری (Gallery Tab)

**Purpose:** Staff member's personal portfolio/work samples

**Status:** Coming soon

**Notice:**
```
ℹ️ تصاویر نمونه کارهای شخصی شما.
```

**Message:**
```
🖼️ گالری شخصی
نمونه کارهای خود را آپلود کنید تا مشتریان ببینند.
✨ به زودی فعال می‌شود
```

**Future Implementation:**
- Image upload
- Portfolio management
- Work samples showcase
- Customer-facing display

---

## 🎨 Visual Design Elements

### Warning Notice (Yellow)
```scss
.info-notice.warning {
  background: #fef3c7;
  border-color: #fde68a;

  .icon {
    color: #92400e; // Lock icon
  }

  p {
    color: #92400e;
  }
}
```

Used for: Read-only organization data

### Info Notice (Blue)
```scss
.info-notice {
  background: #eff6ff;
  border-color: #bfdbfe;

  .icon {
    color: #1e40af;
  }

  p {
    color: #1e40af;
  }
}
```

Used for: General information

### Help Text (Light Blue)
```scss
.help-text {
  padding: 12px 16px;
  background: #f0f9ff;
  border: 1px solid #bae6fd;
  color: #0c4a6e;
}
```

Used for: Helpful tips and navigation hints

### Coming Soon Badge (Yellow)
```scss
.coming-soon-notice {
  padding: 8px 16px;
  background: #fef3c7;
  color: #92400e;
  border-radius: 8px;
}
```

Used for: Features in development

---

## 🔐 Access Control Matrix

| Tab | Organization | Staff Member | Independent |
|-----|-------------|--------------|-------------|
| **Personal** | ✏️ Edit | 👁️ View (soon ✏️) | ✏️ Edit |
| **Business** | ✏️ Edit org info | 👁️ View org info | ✏️ Edit own info |
| **Location** | ✏️ Edit address | 👁️ View org address | ✏️ Edit address |
| **Hours** | ✏️ Edit schedule | 👁️ View (managed by org) | ✏️ Edit hours |
| **Gallery** | ✏️ Manage | ✏️ Personal (soon) | ✏️ Manage |

**Legend:**
- ✏️ = Editable
- 👁️ = Read-only
- ❌ = Hidden

---

## 💾 Data Flow

### Personal Tab
```typescript
// Data from provider (staff member's own data)
const provider = computed(() => hierarchyStore.currentHierarchy?.provider)

personalForm.value = {
  firstName: provider.value.firstName,
  lastName: provider.value.lastName,
  email: provider.value.email,
  phoneNumber: provider.value.phoneNumber,
  bio: provider.value.bio,
}
```

### Business Tab
```typescript
// Data from parent organization
const parentOrganization = computed(() =>
  hierarchyStore.currentHierarchy?.parentOrganization
)

// Display:
parentOrganization.value.businessName  // "آرایشگاه رز"
parentOrganization.value.businessType  // "Salon"
parentOrganization.value.logoUrl       // "https://..."
```

### Location Tab
```typescript
// Data from parent organization
parentOrganization.value.city   // "تهران"
parentOrganization.value.state  // "تهران"
```

---

## 🧩 Component Integration

### Imports
```typescript
import { useHierarchyStore } from '../../stores/hierarchy.store'

const hierarchyStore = useHierarchyStore()
```

### Computed Properties
```typescript
const provider = computed(() => hierarchyStore.currentHierarchy?.provider)
const parentOrganization = computed(() => hierarchyStore.currentHierarchy?.parentOrganization)
const organizationName = computed(() => hierarchyStore.currentHierarchy?.parentOrganization?.businessName)
```

### Helper Function
```typescript
const getBusinessTypeLabel = (type: string) => {
  const labels: Record<string, string> = {
    'Salon': 'سالن زیبایی',
    'Barbershop': 'آرایشگاه',
    'SpaWellness': 'اسپا و سلامتی',
    'Clinic': 'کلینیک',
    'BeautySalon': 'سالن زیبایی',
    'Other': 'سایر'
  }
  return labels[type] || type
}
```

---

## 📱 Responsive Design

### Mobile Optimization
```scss
@media (max-width: 768px) {
  .tab-button {
    min-width: 100px;
    padding: 12px 16px;
    font-size: 14px;

    .tab-label {
      display: none; // Show icons only on mobile
    }
  }

  .form-row {
    grid-template-columns: 1fr; // Single column on mobile
  }
}
```

**Mobile View:**
```
┌──────────────────────┐
│ 👤 │ 🏢 │ 📍 │ 🕐 │ 🖼️ │  ← Icons only
├──────────────────────┤
│  Content (1 column)  │
└──────────────────────┘
```

---

## 🎯 UX Best Practices

### ✅ Clear Visual Indicators
- **Yellow warning notices** for read-only org data
- **Lock icons** to indicate non-editable fields
- **Disabled inputs** with grayed-out styling

### ✅ Helpful Context
- Explain WHY fields are read-only
- Provide alternative actions (link to "My Organization")
- Show who can make changes ("مدیر سازمان")

### ✅ Consistent Terminology
- "سازمان من" = My Organization
- "مدیر سازمان" = Organization Manager
- "قابل مشاهده" = Read-only / View-only

### ✅ Progressive Disclosure
- Show placeholders for coming soon features
- Indicate development status clearly
- Don't hide unavailable features completely

---

## 🚀 Future Enhancements

### Phase 1: Personal Tab Editing
- [ ] Enable personal info editing
- [ ] Profile image upload
- [ ] Bio editing with character limit
- [ ] Phone number verification on change

### Phase 2: Gallery Implementation
- [ ] Image upload functionality
- [ ] Portfolio management
- [ ] Image ordering/organization
- [ ] Public portfolio page

### Phase 3: Working Hours Display
- [ ] Show current schedule
- [ ] Request changes feature
- [ ] Availability calendar
- [ ] Time-off requests

### Phase 4: Enhanced Organization View
- [ ] Full organization details in Business tab
- [ ] Interactive map in Location tab
- [ ] Organization stats and metrics
- [ ] Contact organization manager button

---

## 🧪 Testing Checklist

### Visual Testing
- [ ] All 5 tabs render correctly
- [ ] Warning notices display with yellow background
- [ ] Disabled inputs have grayed-out appearance
- [ ] Icons and images display properly
- [ ] Responsive design works on mobile

### Data Testing
- [ ] Personal data loads from `provider`
- [ ] Organization data loads from `parentOrganization`
- [ ] Business type labels translate correctly
- [ ] Missing data shows gracefully (placeholders)

### Navigation Testing
- [ ] Tab switching works smoothly
- [ ] Active tab highlighted correctly
- [ ] Tab content animates on switch
- [ ] Mobile tab navigation scrolls horizontally

### Access Control Testing
- [ ] Business info fields are disabled
- [ ] Location fields are disabled
- [ ] Personal fields populate correctly
- [ ] Organization name displays in notices

---

## 📊 Comparison: Organization vs Staff Profile

### Organization Profile Tabs
```
┌─ پروفایل من ──────────────────┐
│  ✏️ Editable                   │
│  Personal info                 │
└────────────────────────────────┘

┌─ کسب‌وکار ───────────────────┐
│  ✏️ Editable                   │
│  Business name, logo, etc.     │
└────────────────────────────────┘

┌─ موقعیت ─────────────────────┐
│  ✏️ Editable                   │
│  Address, map, coordinates     │
└────────────────────────────────┘

┌─ پرسنل ──────────────────────┐
│  ✏️ Manage staff               │
│  Invite, remove, view team     │
└────────────────────────────────┘

┌─ ساعات کاری ─────────────────┐
│  ✏️ Editable                   │
│  Business hours, breaks        │
└────────────────────────────────┘

┌─ گالری ──────────────────────┐
│  ✏️ Editable                   │
│  Business portfolio            │
└────────────────────────────────┘
```

### Staff Member Profile Tabs
```
┌─ پروفایل من ──────────────────┐
│  👁️ View only (soon ✏️)        │
│  Personal info                 │
└────────────────────────────────┘

┌─ خدمات ──────────────────────┐
│  👁️ View only                  │
│  ⚠️ Organization business info │
└────────────────────────────────┘

┌─ موقعیت ─────────────────────┐
│  👁️ View only                  │
│  ⚠️ Organization location      │
└────────────────────────────────┘

❌ No "پرسنل" tab (staff can't manage staff)

┌─ ساعات کاری ─────────────────┐
│  👁️ View only                  │
│  Managed by organization       │
└────────────────────────────────┘

┌─ گالری ──────────────────────┐
│  ✨ Coming soon                │
│  Personal portfolio            │
└────────────────────────────────┘
```

---

## 🎉 Summary

### Key Features
✅ **Same Structure** - Professional tab layout like organizations
✅ **Clear Restrictions** - Visual indicators for read-only data
✅ **Organization Context** - Shows parent org data appropriately
✅ **Helpful Messages** - Explains what staff can/cannot do
✅ **Future-Ready** - Placeholders for upcoming features
✅ **Responsive** - Works on all screen sizes

### UX Win
Staff members feel they have a complete, professional profile section while understanding their role boundaries and seeing relevant organization information.

**Perfect balance of transparency and access control!** 🎯
