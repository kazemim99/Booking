# Figma UI/UX Design Prompt - Booksy Platform

**Project**: Booksy - Service Booking Platform for Iran Market
**Tech Stack**: Vue 3, TypeScript, Tailwind CSS
**Language**: Persian (Farsi) with RTL Support
**Export Format**: HTML/CSS or Vue Components

---

## Design System Requirements

### Core Principles
- **Modern & Clean**: Minimalist design with focus on usability
- **Persian-First**: All text in Persian, full RTL support
- **Mobile-First**: Responsive design (mobile, tablet, desktop)
- **Accessible**: WCAG 2.1 AA compliance
- **Performance**: Lightweight, fast-loading components

### Color Palette
```
Primary: #1976D2 (Blue) - Main actions, links
Secondary: #424242 (Dark Gray) - Text, secondary elements
Success: #4CAF50 (Green) - Success states, confirmations
Warning: #FF9800 (Orange) - Warnings, pending states
Error: #D32F2F (Red) - Errors, destructive actions
Background: #F5F5F5 (Light Gray)
Surface: #FFFFFF (White) - Cards, panels
Text Primary: #212121
Text Secondary: #757575
```

### Typography
- **Font Family**: Vazir, IRANSans, or Yekan (Persian fonts)
- **Headings**:
  - H1: 32px/40px, Bold
  - H2: 24px/32px, SemiBold
  - H3: 20px/28px, SemiBold
  - H4: 16px/24px, Medium
- **Body**: 14px/20px, Regular
- **Small**: 12px/16px, Regular
- **Direction**: RTL (Right-to-Left)

### Spacing System
- xs: 4px
- sm: 8px
- md: 16px
- lg: 24px
- xl: 32px
- 2xl: 48px

### Border Radius
- Small: 4px (buttons, inputs)
- Medium: 8px (cards)
- Large: 12px (modals, containers)
- Round: 50% (avatars, icons)

### Shadows
- Small: 0 1px 3px rgba(0,0,0,0.12)
- Medium: 0 4px 6px rgba(0,0,0,0.1)
- Large: 0 10px 20px rgba(0,0,0,0.15)

---

## 1. Authentication Module

### 1.1 Login Page (`/login`)
**Purpose**: Phone-based authentication entry point

**Layout**:
- Split screen (desktop): Left side = branding/illustration, Right side = form
- Full screen (mobile): Logo at top, form below
- RTL layout

**Components**:
- Logo and branding area
- Page title: "ورود به بوکسی"
- Subtitle: "لطفاً شماره موبایل خود را وارد کنید"
- Phone number input field with country selector
  - Label: "شماره موبایل"
  - Placeholder: "۰۹۱۲۳۴۵۶۷۸۹"
  - Country flag icon (Iran default)
  - Input direction: LTR for numbers
- Primary button: "ارسال کد تأیید"
- Footer links:
  - "ثبت‌نام به عنوان ارائه‌دهنده خدمات"
  - "راهنما و پشتیبانی"

**States**:
- Default
- Focus (input focused)
- Loading (sending SMS)
- Error (invalid phone number)
- Success (code sent, navigate to verification)

**Responsive**:
- Desktop: 1200px+ (split screen)
- Tablet: 768px-1199px (centered form)
- Mobile: <768px (full width form)

---

### 1.2 Phone Verification Page (`/phone-verification`)
**Purpose**: OTP code verification

**Layout**:
- Centered card on background
- RTL layout

**Components**:
- Back button (top right)
- Icon: Phone with checkmark
- Title: "تأیید شماره موبایل"
- Subtitle: "کد ۶ رقمی ارسال شده به {phone} را وارد کنید"
- OTP Input: 6 separate boxes for digits
  - Input direction: LTR
  - Auto-focus first box
  - Auto-advance on input
- Timer: "ارسال مجدد کد تا ۰:۵۹"
- Resend button (disabled until timer expires)
- Submit button: "تأیید و ادامه"
- Error message area (below OTP)
- "تغییر شماره موبایل" link

**States**:
- Default
- Entering code
- Invalid code (shake animation, error message)
- Loading (verifying)
- Success (checkmark, navigate to registration)
- Resend available (timer expired, button enabled)

**Responsive**:
- All sizes: Centered card, responsive width

---

## 2. Provider Registration Module

### 2.1 Registration Progress Indicator (Shared Component)
**Purpose**: Show registration progress

**Components**:
- Stepper with 9 steps
- Persian labels:
  1. "اطلاعات کسب‌وکار"
  2. "دسته‌بندی"
  3. "موقعیت مکانی"
  4. "خدمات"
  5. "کارکنان"
  6. "ساعات کاری"
  7. "گالری تصاویر"
  8. "بازخورد (اختیاری)"
  9. "تکمیل ثبت‌نام"
- Current step highlighted
- Completed steps with checkmarks
- Future steps grayed out

---

### 2.2 Step 1: Business Info (`/registration` - Step 1)
**Purpose**: Collect basic business information

**Layout**:
- Progress indicator at top
- Form in center card
- RTL layout

**Components**:
- Progress indicator (Step 1 active)
- Title: "اطلاعات کسب‌وکار"
- Subtitle: "لطفاً اطلاعات کسب‌وکار خود را وارد کنید"
- Form fields:
  1. **Business Name** (required)
     - Label: "نام کسب‌وکار"
     - Placeholder: "مثال: آرایشگاه زیبا"
  2. **Owner First Name** (required)
     - Label: "نام مالک"
     - Placeholder: "علی"
  3. **Owner Last Name** (required)
     - Label: "نام خانوادگی مالک"
     - Placeholder: "احمدی"
  4. **Phone Number** (read-only, auto-filled)
     - Label: "شماره تماس"
     - Value: From phone verification
     - Disabled/grayed out
- Action buttons:
  - Primary: "بعدی" (enabled only when valid)
  - Secondary: "انصراف" (returns to dashboard)

**Validation**:
- Real-time validation on blur
- Error messages in Persian below each field
- Disable submit until all required fields valid

**Responsive**:
- Desktop: 800px max-width card
- Mobile: Full width with padding

---

### 2.3 Step 2: Category Selection (`/registration` - Step 2)
**Purpose**: Select service category

**Layout**:
- Progress indicator (Step 2 active)
- Grid of category cards

**Components**:
- Title: "انتخاب دسته‌بندی"
- Subtitle: "نوع خدمات خود را انتخاب کنید"
- Category grid (2-4 columns responsive):
  - **Beauty Salon** (آرایشگاه)
    - Icon: Scissors/Mirror
    - Description: "آرایشگری، آرایش، مانیکور و..."
  - **Spa** (اسپا)
    - Icon: Lotus flower
    - Description: "ماساژ، بدن‌سازی، سونا و..."
  - **Medical** (پزشکی)
    - Icon: Stethoscope
    - Description: "دندانپزشکی، فیزیوتراپی و..."
  - **Education** (آموزشی)
    - Icon: Book
    - Description: "کلاس‌های آموزشی و کارگاه‌ها"
  - **Fitness** (ورزشی)
    - Icon: Dumbbell
    - Description: "باشگاه، یوگا، پیلاتس و..."
  - **Other** (سایر)
    - Icon: Grid
    - Description: "سایر خدمات"
- Each card:
  - Hover effect
  - Selected state (border highlight)
  - Radio button indicator
- Action buttons:
  - Primary: "بعدی"
  - Secondary: "قبلی"

**States**:
- Default (no selection)
- Hover (on card)
- Selected (one card selected)

**Responsive**:
- Desktop: 3 columns
- Tablet: 2 columns
- Mobile: 1 column

---

### 2.4 Step 3: Location (`/registration` - Step 3)
**Purpose**: Set business location and address

**Layout**:
- Progress indicator (Step 3 active)
- Two-column layout (map + form)

**Components**:
- Title: "موقعیت مکانی کسب‌وکار"
- Left panel (form):
  1. **Province** (استان) - Dropdown
  2. **City** (شهر) - Dropdown (depends on province)
  3. **Address Line 1** (خیابان و پلاک) - Text
  4. **Address Line 2** (واحد) - Text (optional)
  5. **Postal Code** (کد پستی) - Text
- Right panel (map):
  - Interactive map (OpenStreetMap/Mapbox)
  - Draggable marker
  - "مکان من را تعیین کن" button (GPS)
  - Selected coordinates display
- Action buttons:
  - Primary: "ذخیره و ادامه"
  - Secondary: "قبلی"

**States**:
- Loading map
- Map loaded
- Marker dragged (coordinates update)
- Form validation errors

**Responsive**:
- Desktop: Side-by-side (50/50)
- Mobile: Stacked (form top, map bottom)

---

### 2.5 Step 4: Services (`/registration` - Step 4)
**Purpose**: Add services offered

**Layout**:
- Progress indicator (Step 4 active)
- List of services with add button

**Components**:
- Title: "خدمات ارائه شده"
- Subtitle: "خدماتی که ارائه می‌دهید را اضافه کنید"
- "افزودن خدمت جدید" button (+ icon)
- Service list (empty state or populated):
  - Each service card:
    - Service name (e.g., "کوتاهی مو")
    - Duration (e.g., "۳۰ دقیقه")
    - Price (e.g., "۱۵۰,۰۰۰ تومان")
    - Edit/Delete icons
- Add/Edit service modal:
  - Service name input
  - Category dropdown (مردانه/زنانه/عمومی)
  - Duration picker (30 min intervals)
  - Price input (تومان)
  - Description textarea (optional)
  - Save/Cancel buttons
- Action buttons:
  - Primary: "بعدی" (requires at least 1 service)
  - Secondary: "قبلی"

**Empty State**:
- Illustration (scissors/tools)
- Text: "هنوز خدمتی اضافه نکرده‌اید"
- "افزودن اولین خدمت" button

**Responsive**:
- Service cards stack on mobile
- Modal full-screen on mobile

---

### 2.6 Step 5: Staff (`/registration` - Step 5)
**Purpose**: Add staff members

**Layout**:
- Progress indicator (Step 5 active)
- Staff member grid

**Components**:
- Title: "کارکنان"
- Subtitle: "اعضای تیم خود را معرفی کنید"
- "افزودن کارمند جدید" button
- Staff grid:
  - Each card:
    - Avatar (upload photo or default)
    - Name
    - Role/Title
    - Assigned services
    - Edit/Delete icons
- Add/Edit staff modal:
  - Photo upload
  - First/Last name
  - Phone number
  - Email (optional)
  - Role/Title
  - Assigned services (multi-select)
  - Bio (optional)
  - Save/Cancel buttons
- Action buttons:
  - Primary: "بعدی"
  - Secondary: "قبلی"
  - "رد کردن این مرحله" (skip - optional)

**Empty State**:
- Illustration (team)
- Text: "هنوز کارمندی اضافه نکرده‌اید"
- "افزودن اولین کارمند" button

---

### 2.7 Step 6: Working Hours (`/registration` - Step 6)
**Purpose**: Set business hours

**Layout**:
- Progress indicator (Step 6 active)
- Weekly schedule grid

**Components**:
- Title: "ساعات کاری"
- Subtitle: "ساعات کاری هفتگی خود را تنظیم کنید"
- Weekly schedule:
  - Rows: Days of week (شنبه to جمعه)
  - Columns: Open/Close times
  - Each row:
    - Day name
    - Toggle switch (روز کاری / تعطیل)
    - Start time picker (if enabled)
    - End time picker (if enabled)
    - "افزودن بازه زمانی" (add break/split shift)
- "اعمال برای همه روزها" button
- Preset options:
  - "شنبه تا چهارشنبه ۹-۱۷"
  - "همه روزه ۱۰-۲۰"
  - "دلخواه"
- Action buttons:
  - Primary: "بعدی"
  - Secondary: "قبلی"

**States**:
- Day enabled (time pickers visible)
- Day disabled (grayed out)
- Multiple time ranges (split shifts)

---

### 2.8 Step 7: Gallery (`/registration` - Step 7)
**Purpose**: Upload business photos

**Layout**:
- Progress indicator (Step 7 active)
- Photo gallery grid

**Components**:
- Title: "گالری تصاویر"
- Subtitle: "تصاویری از کسب‌وکار خود آپلود کنید"
- Upload area:
  - Drag & drop zone
  - "انتخاب تصاویر" button
  - Supported formats: JPG, PNG (max 5MB)
- Gallery grid (uploaded images):
  - Thumbnail previews
  - Delete button (X icon)
  - Set as cover option
  - Drag to reorder
- Minimum 3 images recommended
- Action buttons:
  - Primary: "بعدی"
  - Secondary: "قبلی"
  - "رد کردن این مرحله" (skip - optional)

**Empty State**:
- Illustration (camera/images)
- Text: "هنوز تصویری آپلود نکرده‌اید"
- Upload button

---

### 2.9 Step 8: Feedback (`/registration` - Step 8)
**Purpose**: Optional user feedback

**Layout**:
- Progress indicator (Step 8 active)
- Simple form

**Components**:
- Title: "نظرات شما"
- Subtitle: "چگونه از بوکسی خبر دار شدید؟ (اختیاری)"
- Feedback form:
  - Radio options:
    - "جستجوی اینترنتی"
    - "شبکه‌های اجتماعی"
    - "توصیه دوستان"
    - "تبلیغات"
    - "سایر"
  - Comment textarea (اختیاری)
- Action buttons:
  - Primary: "بعدی"
  - Secondary: "قبلی"
  - "رد کردن" (skip)

---

### 2.10 Step 9: Completion (`/registration` - Step 9)
**Purpose**: Review and submit

**Layout**:
- Progress indicator (Step 9 complete)
- Summary review

**Components**:
- Title: "بررسی و تأیید نهایی"
- Summary sections (collapsible cards):
  1. Business Info
  2. Location
  3. Services (count)
  4. Staff (count)
  5. Working Hours
  6. Gallery (image count)
- Each section has "ویرایش" button (returns to step)
- Terms & Conditions:
  - Checkbox: "شرایط و قوانین را خوانده و قبول دارم"
  - Link to T&C
- Action buttons:
  - Primary: "تأیید و ثبت نهایی" (disabled until T&C accepted)
  - Secondary: "قبلی"

**Success State** (after submit):
- Success animation (checkmark)
- Message: "ثبت‌نام شما با موفقیت انجام شد!"
- "ورود به داشبورد" button

---

## 3. Provider Dashboard Module

### 3.1 Dashboard Home (`/dashboard`)
**Purpose**: Overview of business metrics

**Layout**:
- Sidebar navigation (right side, RTL)
- Header with user profile
- Main content area

**Components**:

**Sidebar** (persistent):
- Logo at top
- Navigation menu items:
  - داشبورد (فعال)
  - رزرو‌ها
  - خدمات
  - کارکنان
  - مالی
  - تنظیمات
  - خروج
- Each item: icon + text
- Active state highlight
- Collapse button (mobile)

**Header**:
- Page title: "داشبورد"
- Date: Current Persian date
- Notification bell icon (with badge)
- User profile dropdown (right side):
  - Avatar
  - Name
  - "مشاهده پروفایل"
  - "تنظیمات"
  - "خروج"

**Main Content**:
- Stats cards (4 cards in row):
  1. **رزرو‌های امروز**
     - Icon: Calendar
     - Number: 12
     - Trend: +5% from yesterday
  2. **درآمد این ماه**
     - Icon: Money
     - Number: ۱۵,۵۰۰,۰۰۰ تومان
     - Trend: +12% from last month
  3. **مشتریان جدید**
     - Icon: Users
     - Number: 8
     - Trend: +3 this week
  4. **نرخ رضایت**
     - Icon: Star
     - Number: 4.8/5
     - Based on reviews

- Recent bookings table:
  - Title: "رزرو‌های اخیر"
  - "مشاهده همه" link
  - Columns:
    - مشتری (name + avatar)
    - خدمت
    - تاریخ و ساعت
    - کارمند
    - وضعیت (badge)
    - عملیات (actions)
  - 5 recent rows
  - Empty state: "رزروی وجود ندارد"

- Chart section:
  - Title: "نمودار درآمد"
  - Time range selector (هفته / ماه / سال)
  - Line/Bar chart showing revenue

**Responsive**:
- Desktop: Sidebar visible
- Tablet: Sidebar collapsible
- Mobile: Sidebar as drawer (hamburger menu)

---

### 3.2 Bookings List (`/bookings`)
**Purpose**: Manage all bookings

**Layout**:
- Sidebar + header (same as dashboard)
- Filters + table

**Components**:

**Filters bar**:
- Date range picker
- Status filter dropdown:
  - همه
  - تأیید شده
  - در انتظار
  - انجام شده
  - لغو شده
- Service filter (multi-select)
- Staff filter (multi-select)
- Search box: "جستجو بر اساس نام مشتری"
- "افزودن رزرو جدید" button (primary)

**Bookings table**:
- Columns:
  - چک‌باکس (bulk actions)
  - کد رزرو
  - مشتری (avatar + name + phone)
  - خدمت
  - تاریخ و ساعت
  - کارمند
  - مدت زمان
  - هزینه
  - وضعیت (badge with colors):
    - تأیید شده (green)
    - در انتظار (yellow)
    - انجام شده (blue)
    - لغو شده (red)
  - عملیات:
    - مشاهده
    - ویرایش
    - لغو
- Pagination at bottom
- Bulk actions (when items selected):
  - تأیید دسته‌جمعی
  - لغو دسته‌جمعی
  - صدور فاکتور

**Responsive**:
- Desktop: Full table
- Tablet: Hide some columns
- Mobile: Card view instead of table

---

### 3.3 Booking Detail (`/bookings/:id`)
**Purpose**: View/edit single booking

**Layout**:
- Sidebar + header
- Detail panel + timeline

**Components**:

**Header section**:
- Back button: "بازگشت به لیست"
- Booking code: #۱۲۳۴۵
- Status badge (large)
- Action buttons:
  - ویرایش
  - لغو رزرو
  - صدور فاکتور
  - ارسال پیامک یادآوری

**Main panel** (2 columns):

**Left column** (details):
- Customer info card:
  - Avatar
  - Name
  - Phone
  - Email
  - "مشاهده سوابق مشتری" link
- Service details card:
  - Service name
  - Duration
  - Price
  - Staff member
  - Date & time
- Payment info card:
  - Amount
  - Payment method
  - Status
  - Transaction ID
- Notes section:
  - Internal notes (staff only)
  - Customer notes

**Right column** (timeline):
- Activity timeline:
  - رزرو ایجاد شد (timestamp)
  - رزرو تأیید شد (timestamp)
  - پیامک ارسال شد (timestamp)
  - رزرو انجام شد (timestamp)

**Edit mode**:
- Inline editing of details
- Save/Cancel buttons

---

### 3.4 Services List (`/services`)
**Purpose**: Manage services

**Layout**:
- Sidebar + header
- Grid/List view toggle

**Components**:

**Toolbar**:
- View toggle: Grid / List
- Sort dropdown: (نام / قیمت / محبوبیت)
- Filter: Category
- Search box
- "افزودن خدمت جدید" button (primary)

**Grid view** (cards):
- Each service card:
  - Icon/image
  - Service name
  - Category badge
  - Duration
  - Price
  - Active/Inactive toggle
  - Quick actions:
    - ویرایش
    - حذف
    - تنظیمات
- 3-4 columns responsive

**List view** (table):
- Columns:
  - نام خدمت
  - دسته‌بندی
  - مدت زمان
  - قیمت
  - تعداد رزرو (این ماه)
  - وضعیت (فعال/غیرفعال)
  - عملیات

**Add/Edit service modal**:
- Service details form (same as registration)
- Advanced settings:
  - Buffer time (before/after)
  - Max bookings per day
  - Require deposit
  - Cancellation policy

**Responsive**:
- Desktop: 3-4 columns grid
- Tablet: 2 columns
- Mobile: 1 column or list

---

### 3.5 Staff List (`/staff`)
**Purpose**: Manage staff members

**Layout**:
- Sidebar + header
- Staff grid

**Components**:

**Toolbar**:
- Search box
- Filter: Role/Department
- "افزودن کارمند جدید" button

**Staff grid**:
- Each staff card:
  - Profile photo
  - Name
  - Role/Title
  - Phone
  - Email
  - Assigned services count
  - Stats:
    - رزرو‌های این ماه
    - نرخ رضایت
    - درآمد تولید شده
  - Active/Inactive toggle
  - Actions:
    - مشاهده پروفایل
    - ویرایش
    - حذف

**Staff detail modal**:
- Profile tab:
  - Personal info
  - Contact info
  - Bio
- Services tab:
  - Assigned services list
  - Add/remove services
- Schedule tab:
  - Working hours
  - Days off
  - Break times
- Statistics tab:
  - Performance metrics
  - Revenue generated
  - Customer reviews

**Responsive**:
- Desktop: 3 columns
- Tablet: 2 columns
- Mobile: 1 column

---

### 3.6 Financial Overview (`/financial`)
**Purpose**: Revenue and payments

**Layout**:
- Sidebar + header
- Stats + charts + table

**Components**:

**Summary cards**:
- Total revenue (این ماه)
- Pending payments
- Completed transactions
- Average booking value

**Charts section**:
- Revenue chart (line chart)
- Payment methods breakdown (pie chart)
- Top services by revenue (bar chart)

**Transactions table**:
- Filters: Date range, status, payment method
- Columns:
  - تاریخ و ساعت
  - کد رزرو
  - مشتری
  - خدمت
  - مبلغ
  - روش پرداخت (نقد / کارت / آنلاین)
  - وضعیت (پرداخت شده / در انتظار)
  - رسید
- Export button (CSV/PDF)

**Payout section** (if integrated):
- Available balance
- Next payout date
- Payout history
- Bank account info

---

### 3.7 Settings (`/settings`)
**Purpose**: Business configuration

**Layout**:
- Sidebar + header
- Settings tabs

**Tabs**:

1. **پروفایل کسب‌وکار**
   - Business name
   - Logo upload
   - Cover photo
   - Description
   - Contact info
   - Social media links

2. **موقعیت مکانی**
   - Address
   - Map picker
   - Location visibility

3. **ساعات کاری**
   - Weekly schedule (same as registration)
   - Holidays/exceptions
   - Break times

4. **تنظیمات رزرو**
   - Booking window (چند روز قبل)
   - Cancellation policy
   - Deposit requirements
   - Auto-confirm bookings
   - SMS notifications

5. **اعلان‌ها**
   - Email notifications (on/off for each event)
   - SMS notifications
   - Push notifications

6. **پرداخت و مالی**
   - Payment methods
   - Bank account (for payouts)
   - Invoice settings
   - Tax info

7. **کاربران و دسترسی‌ها**
   - Admin users
   - Staff permissions
   - API keys

8. **امنیت**
   - Change password
   - Two-factor authentication
   - Login history
   - Active sessions

**Save button** at bottom of each tab

---

## 4. Customer Module

### 4.1 Customer View - Home/Search (`/`)
**Purpose**: Find and book services

**Layout**:
- Public header (no login required)
- Hero section
- Search
- Categories
- Featured providers

**Components**:

**Header**:
- Logo (left)
- Location selector (city)
- Login/Register button (right)
- If logged in: Avatar dropdown

**Hero section**:
- Background image/gradient
- Headline: "بهترین خدمات زیبایی و سلامت"
- Search bar (large):
  - "چه خدمتی می‌خواهید؟" (autocomplete)
  - Location input
  - Date picker
  - "جستجو" button

**Categories** (horizontal scroll or grid):
- Category cards (with icons):
  - آرایشگاه
  - اسپا
  - پزشکی
  - ورزشی
  - آموزشی
  - سایر

**Featured providers**:
- Provider cards:
  - Cover photo
  - Logo
  - Business name
  - Rating (4.5 ⭐)
  - Review count
  - Location
  - Starting price
  - "مشاهده و رزرو" button

**Responsive**:
- Mobile: Stacked sections, horizontal scroll for categories

---

### 4.2 Provider Profile (`/provider/:id`)
**Purpose**: View provider details and book

**Layout**:
- Public header
- Profile hero
- Tabs content

**Components**:

**Profile hero**:
- Cover photo
- Logo overlay
- Business name
- Rating + reviews
- Location
- Status (باز / بسته)
- Share button
- Favorite button

**Info bar**:
- Quick info:
  - Address
  - Phone (call button)
  - Hours (today's schedule)
  - Price range

**Tabs**:

1. **خدمات**
   - Services list/grid
   - Each service card:
     - Name
     - Duration
     - Price
     - "رزرو" button

2. **درباره ما**
   - Business description
   - Facilities
   - Amenities

3. **کارکنان**
   - Staff grid
   - Filter by service

4. **گالری**
   - Photo grid
   - Lightbox view

5. **نظرات**
   - Review cards:
     - Customer name + avatar
     - Rating
     - Date
     - Comment
     - Helpful buttons
   - Write review (if logged in + booked)

**Booking widget** (sticky on desktop):
- Service selector
- Staff selector (optional)
- Date picker (calendar)
- Time slot picker
- Total price
- "تأیید رزرو" button

---

### 4.3 Booking Flow (Modal/Page)
**Purpose**: Complete booking

**Steps**:

1. **انتخاب خدمت**
   - Service selection
   - Staff selection (optional)

2. **انتخاب زمان**
   - Date calendar
   - Available time slots
   - Duration shown

3. **اطلاعات تماس**
   - Name (if not logged in)
   - Phone
   - Email (optional)
   - Notes

4. **تأیید و پرداخت**
   - Booking summary
   - Terms checkbox
   - Payment method:
     - پرداخت در محل
     - پرداخت آنلاین
   - "تأیید رزرو" button

**Success screen**:
- Confirmation message
- Booking details
- QR code
- Add to calendar
- Share buttons

---

### 4.4 Customer Dashboard (`/customer/dashboard`)
**Purpose**: Manage bookings and profile

**Layout**:
- Customer header
- Tabs navigation

**Tabs**:

1. **رزرو‌های من**
   - Upcoming bookings
   - Past bookings
   - Cancelled bookings
   - Each booking card:
     - Provider info
     - Service
     - Date/time
     - Status
     - Actions (cancel, reschedule, review)

2. **علاقه‌مندی‌ها**
   - Favorite providers
   - Quick re-book

3. **پروفایل**
   - Personal info
   - Contact info
   - Password
   - Notifications

4. **پرداخت‌ها**
   - Transaction history
   - Saved cards (if applicable)

---

## 5. Shared Components

### 5.1 Navigation Header
- Logo
- Menu items
- User profile/login

### 5.2 Sidebar Navigation
- Collapsible menu
- Active state
- Icons + labels

### 5.3 Buttons
**Variants**:
- Primary (filled, blue)
- Secondary (outlined)
- Ghost (text only)
- Danger (red, for destructive)

**Sizes**:
- Small (32px height)
- Medium (40px height)
- Large (48px height)

**States**:
- Default
- Hover
- Active
- Disabled
- Loading (spinner)

### 5.4 Form Inputs
**Types**:
- Text input
- Number input
- Textarea
- Select dropdown
- Multi-select
- Date picker
- Time picker
- File upload
- Checkbox
- Radio button
- Toggle switch

**States**:
- Default
- Focus
- Filled
- Error (red border + message)
- Disabled
- Read-only

### 5.5 Cards
- Default card (white background, shadow)
- Hover effect
- Clickable card
- Card with header/footer
- Stat card (with icon + metric)

### 5.6 Modals
- Small (400px)
- Medium (600px)
- Large (800px)
- Full-screen (mobile)
- Header + content + footer
- Close button (X)
- Backdrop overlay

### 5.7 Tables
- Sortable columns
- Filterable
- Selectable rows (checkbox)
- Pagination
- Empty state
- Loading state
- Mobile: Card view fallback

### 5.8 Status Badges
- Success (green)
- Warning (yellow/orange)
- Error/Danger (red)
- Info (blue)
- Neutral (gray)

### 5.9 Notifications/Toasts
- Success toast
- Error toast
- Warning toast
- Info toast
- Duration: 3-5 seconds
- Position: top-right (RTL)
- Close button

### 5.10 Loading States
- Spinner (inline)
- Skeleton loaders (cards, tables)
- Progress bar
- Full-page loader

### 5.11 Empty States
- Illustration/icon
- Message
- Action button
- Used in: no bookings, no services, no results

### 5.12 Avatars
- Sizes: xs (24px), sm (32px), md (40px), lg (64px), xl (128px)
- Default avatar (initials)
- Online/offline indicator
- Avatar group (overlapping)

---

## 6. Responsive Breakpoints

```css
/* Mobile */
@media (max-width: 767px) {
  - Single column layouts
  - Hamburger menu
  - Full-width modals
  - Stacked forms
  - Card view for tables
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
  - 2-column grids
  - Collapsible sidebar
  - Side-by-side forms
}

/* Desktop */
@media (min-width: 1024px) {
  - Multi-column layouts
  - Persistent sidebar
  - Hover states
  - Tooltips
}
```

---

## 7. Animations & Transitions

- **Page transitions**: Fade in (300ms)
- **Modal open**: Slide up + fade (250ms)
- **Button hover**: Scale 1.02 (150ms)
- **Toast notification**: Slide in from right (250ms)
- **Loading spinner**: Continuous rotation
- **Success checkmark**: Draw animation (500ms)
- **Error shake**: Horizontal shake (400ms)

---

## 8. Accessibility Requirements

- **Keyboard navigation**: All interactive elements accessible
- **Focus indicators**: Clear focus rings
- **ARIA labels**: Proper labeling
- **Color contrast**: WCAG AA (4.5:1 for text)
- **Screen reader**: Semantic HTML
- **Form errors**: Descriptive error messages
- **Alt text**: All images

---

## 9. Icons & Illustrations

**Icon library**: Use Material Icons, Heroicons, or similar
**Icon style**: Outlined (stroke-based)
**Icon sizes**: 16px, 24px, 32px

**Common icons needed**:
- Calendar, Clock, User, Phone, Email, Location, Star, Heart
- Edit, Delete, Save, Cancel, Check, Close, Search, Filter
- Arrow left/right, Chevron, Menu, Settings, Logout
- Service-specific: Scissors, Mirror, Spa, Medical, Fitness

**Illustrations**:
- Empty states
- Success states
- Error states
- Hero sections
- Onboarding

---

## 10. Export Requirements

### For HTML/CSS Export:
- Semantic HTML5
- BEM naming convention
- Responsive CSS (flexbox/grid)
- RTL support (direction: rtl)
- Modern browser support

### For Vue Component Export:
- Vue 3 Composition API
- TypeScript support
- Scoped styles
- Props interface definitions
- Emits definitions
- Responsive directives
- RTL class bindings

---

## 11. Admin Panel Module

### 11.1 Admin Dashboard (`/admin/dashboard`)
**Purpose**: Platform-wide overview and management

**Layout**:
- Admin sidebar (darker theme, different from provider)
- Top header with admin profile
- Main dashboard content

**Components**:

**Admin Sidebar**:
- Logo + "پنل مدیریت" badge
- Navigation items:
  - داشبورد (فعال)
  - ارائه‌دهندگان
  - مشتریان
  - رزرو‌ها
  - مالی و تسویه
  - گزارشات
  - محتوا
  - تنظیمات سیستم
  - پشتیبانی
  - خروج
- Each item with icon + badge (for pending items)
- Collapsible on mobile

**Header**:
- Breadcrumb: داشبورد
- Search (global): "جستجو در سیستم..."
- Notification bell (system alerts)
- Admin profile dropdown:
  - Name + role
  - مشاهده پروفایل
  - تنظیمات
  - خروج

**Dashboard Content**:

**Summary Cards** (6 cards, 3x2 grid):
1. **کل ارائه‌دهندگان**
   - Icon: Building
   - Number: 1,234
   - Status breakdown:
     - فعال: 987 (green)
     - در انتظار تأیید: 123 (yellow)
     - تعلیق شده: 45 (red)
     - پیش‌نویس: 79 (gray)

2. **کل مشتریان**
   - Icon: Users
   - Number: 15,678
   - Trend: +234 this week
   - Active users: 8,456

3. **رزرو‌های امروز**
   - Icon: Calendar
   - Number: 456
   - Status:
     - تأیید شده: 345
     - در انتظار: 89
     - انجام شده: 22

4. **درآمد پلتفرم (این ماه)**
   - Icon: Money
   - Number: ۴۵,۰۰۰,۰۰۰ تومان
   - Commission: ۲,۲۵۰,۰۰۰ تومان (5%)
   - Trend: +15% from last month

5. **تسویه‌های در انتظار**
   - Icon: Wallet
   - Number: ۱۲,۵۰۰,۰۰۰ تومان
   - Count: 45 providers
   - Next payout: 3 days

6. **تیکت‌های پشتیبانی**
   - Icon: Headset
   - Open tickets: 12
   - Pending: 5
   - Avg response time: 2.5h

**Recent Activities Table**:
- Title: "فعالیت‌های اخیر سیستم"
- Columns:
  - زمان
  - نوع فعالیت
  - کاربر/ارائه‌دهنده
  - جزئیات
  - وضعیت
- Real-time updates
- 10 latest activities

**Charts Section**:
- **Revenue Chart** (line chart):
  - Platform revenue vs provider revenue
  - Time range: week/month/year

- **Registration Chart** (bar chart):
  - New providers per day
  - New customers per day

- **Top Categories** (horizontal bar):
  - Most popular service categories
  - Booking count per category

**Quick Actions**:
- "تأیید ارائه‌دهندگان جدید" (badge: 23)
- "بررسی گزارشات" (badge: 5)
- "تسویه حساب" (badge: 45)
- "پاسخ به تیکت‌ها" (badge: 12)

**Responsive**:
- Desktop: Full layout with sidebar
- Tablet: Collapsible sidebar, 2-column cards
- Mobile: Stacked cards, drawer sidebar

---

### 11.2 Providers Management (`/admin/providers`)
**Purpose**: Manage all providers on platform

**Layout**:
- Admin sidebar + header
- Filters + providers table

**Components**:

**Toolbar**:
- Status tabs (with counts):
  - همه (1,234)
  - در انتظار تأیید (123) - highlighted
  - فعال (987)
  - تعلیق شده (45)
  - رد شده (79)
- Filters:
  - دسته‌بندی (multi-select)
  - شهر (dropdown)
  - تاریخ ثبت‌نام (date range)
  - درآمد (range slider)
- Search: "جستجو بر اساس نام، تلفن، ایمیل"
- Sort: (جدیدترین / محبوب‌ترین / بیشترین درآمد)
- Export button: "خروجی اکسل"

**Providers Table**:
- Columns:
  - چک‌باکس (bulk actions)
  - تصویر + نام کسب‌وکار
  - صاحب کسب‌وکار
  - دسته‌بندی
  - موقعیت (شهر)
  - تاریخ ثبت‌نام
  - تعداد رزرو (این ماه)
  - درآمد (این ماه)
  - امتیاز (rating)
  - وضعیت (badge):
    - در انتظار تأیید (yellow)
    - فعال (green)
    - تعلیق شده (red)
    - رد شده (gray)
  - عملیات:
    - مشاهده پروفایل
    - ویرایش
    - تأیید/رد
    - تعلیق/فعال‌سازی
    - حذف
- Pagination
- Bulk actions (when selected):
  - تأیید دسته‌جمعی
  - رد دسته‌جمعی
  - تعلیق دسته‌جمعی
  - ارسال ایمیل

**Filters Panel** (collapsible):
- Advanced filters:
  - تاریخ آخرین فعالیت
  - تعداد کارمندان
  - تعداد خدمات
  - نوع اشتراک (free/premium)
  - وضعیت تأیید هویت

**Responsive**:
- Desktop: Full table
- Tablet: Hide some columns, show in detail modal
- Mobile: Card view with key info

---

### 11.3 Provider Detail/Approval (`/admin/providers/:id`)
**Purpose**: View provider details and approve/reject

**Layout**:
- Admin sidebar + header
- Provider detail panel

**Components**:

**Header Section**:
- Back button: "بازگشت به لیست"
- Provider name + ID
- Status badge (large)
- Action buttons (based on status):
  - If Pending: "تأیید", "رد", "درخواست تکمیل اطلاعات"
  - If Active: "تعلیق", "ویرایش"
  - If Suspended: "فعال‌سازی", "حذف دائمی"
- "مشاهده به عنوان مشتری" link (preview)

**Tabs**:

**1. اطلاعات کسب‌وکار**
- Profile photo + cover
- Business name
- Owner name
- Phone (verified badge)
- Email
- Category
- Description
- Registration date
- Last activity

**2. موقعیت مکانی**
- Full address
- Map with marker
- Province, City
- GPS coordinates

**3. خدمات** (count: 12)
- Services list
- Each service:
  - Name
  - Category
  - Duration
  - Price
  - Status (active/inactive)
- "مشاهده همه" button

**4. کارکنان** (count: 5)
- Staff members
- Photos, names, roles
- Assigned services
- "مشاهده همه" button

**5. ساعات کاری**
- Weekly schedule
- Holidays/exceptions
- Break times

**6. گالری** (12 photos)
- Photo grid
- View full-size
- Flag inappropriate (admin action)

**7. رزرو‌ها** (last 30 days)
- Bookings table
- Stats:
  - Total bookings: 145
  - Completed: 132
  - Cancelled: 8
  - No-show: 5
- "مشاهده همه رزرو‌ها" link

**8. مالی**
- Revenue stats:
  - This month: ۱۵,۰۰۰,۰۰۰ تومان
  - Platform commission (5%): ۷۵۰,۰۰۰ تومان
  - Payout pending: ۵۰۰,۰۰۰ تومان
- Payment methods
- Bank account info (for payouts)
- Transaction history

**9. نظرات و امتیازها**
- Overall rating: 4.5/5 (from 87 reviews)
- Review breakdown (5 stars to 1 star)
- Recent reviews:
  - Customer name
  - Rating
  - Date
  - Comment
  - Admin actions:
    - تأیید/رد نظر
    - حذف نظر نامناسب
    - پاسخ از طرف ادمین

**10. فعالیت‌ها و گزارشات**
- Activity log:
  - Login history
  - Changes made
  - Bookings created
  - Services added/edited
- Reports from customers (if any):
  - Report count: 2
  - Reasons
  - Status: investigating/resolved
- Admin notes (internal):
  - Textarea for admin notes
  - Note history

**Approval Panel** (if status = Pending):
- Checklist:
  - ✅ اطلاعات کسب‌وکار کامل است
  - ✅ موقعیت مکانی تأیید شده
  - ✅ حداقل 3 خدمت اضافه شده
  - ✅ حداقل 3 تصویر آپلود شده
  - ⚠️ اطلاعات بانکی تکمیل نشده
- Approval decision:
  - Radio: تأیید / رد / درخواست تکمیل
  - If reject: Rejection reason (textarea)
  - If request completion: Missing items (checkboxes)
- Send notification: Email + SMS checkbox
- "ثبت تصمیم" button

**Responsive**:
- Desktop: Full tabs
- Mobile: Accordion-style sections

---

### 11.4 Customers Management (`/admin/customers`)
**Purpose**: Manage all customers

**Layout**:
- Admin sidebar + header
- Filters + customers table

**Components**:

**Toolbar**:
- Filters:
  - وضعیت (active/inactive/blocked)
  - تاریخ ثبت‌نام (date range)
  - تعداد رزرو (range)
- Search: "جستجو بر اساس نام، تلفن، ایمیل"
- Export button

**Customers Table**:
- Columns:
  - چک‌باکس
  - نام + تصویر
  - تلفن (verified badge)
  - ایمیل
  - تاریخ عضویت
  - تعداد رزرو (کل)
  - آخرین رزرو
  - هزینه کل
  - وضعیت (active/blocked)
  - عملیات:
    - مشاهده پروفایل
    - ویرایش
    - مسدود کردن
    - حذف
- Pagination
- Bulk actions

**Responsive**:
- Mobile: Card view

---

### 11.5 Customer Detail (`/admin/customers/:id`)
**Purpose**: View customer details

**Layout**:
- Admin sidebar + header
- Customer detail tabs

**Tabs**:

**1. اطلاعات شخصی**
- Avatar
- Name
- Phone (verified)
- Email
- Date of birth
- Gender
- Address
- Registration date
- Last login

**2. رزرو‌ها**
- Bookings history (all)
- Stats:
  - Total bookings: 45
  - Completed: 40
  - Cancelled: 3
  - No-show: 2
- Bookings table

**3. علاقه‌مندی‌ها**
- Favorite providers (8)
- Favorite categories

**4. نظرات**
- Reviews written by this customer
- Rating given
- Provider reviewed
- Date

**5. پرداخت‌ها**
- Transaction history
- Total spent: ۳,۵۰۰,۰۰۰ تومان
- Payment methods

**6. گزارشات**
- Reports filed by customer
- Reports against customer (if any)

**7. یادداشت‌های ادمین**
- Internal notes
- Support history

**Action Buttons**:
- ویرایش اطلاعات
- مسدود کردن
- ارسال ایمیل
- ارسال پیامک
- حذف حساب

---

### 11.6 All Bookings (`/admin/bookings`)
**Purpose**: Platform-wide booking management

**Layout**:
- Admin sidebar + header
- Advanced filters + bookings table

**Components**:

**Toolbar**:
- Status tabs (with counts):
  - همه
  - تأیید شده
  - در انتظار
  - انجام شده
  - لغو شده
- Filters:
  - ارائه‌دهنده (autocomplete search)
  - مشتری (autocomplete search)
  - دسته‌بندی
  - شهر
  - تاریخ (date range picker)
  - مبلغ (range slider)
- Search: "جستجو کد رزرو، نام مشتری، ارائه‌دهنده"
- Real-time toggle: "به‌روزرسانی خودکار"
- Export button

**Bookings Table**:
- Columns:
  - کد رزرو
  - مشتری (name + avatar)
  - ارائه‌دهنده (name + logo)
  - خدمت
  - تاریخ و ساعت
  - مدت زمان
  - مبلغ
  - کمیسیون پلتفرم (5%)
  - وضعیت پرداخت
  - وضعیت رزرو
  - عملیات:
    - مشاهده جزئیات
    - لغو رزرو (with reason)
    - بازپرداخت
- Pagination
- Auto-refresh every 30 seconds

**Stats Row** (above table):
- Today's bookings: 456
- This week: 2,345
- Revenue today: ۱۲,۳۴۵,۰۰۰ تومان
- Commission earned: ۶۱۷,۲۵۰ تومان

---

### 11.7 Financial Management (`/admin/financial`)
**Purpose**: Platform finances and provider payouts

**Layout**:
- Admin sidebar + header
- Financial tabs

**Tabs**:

**1. نمای کلی**
- Summary cards:
  - درآمد کل (این ماه)
  - کمیسیون پلتفرم
  - تسویه‌های پرداخت شده
  - تسویه‌های در انتظار
- Revenue chart (platform commission over time)
- Top earning providers (table)
- Payment method breakdown (pie chart)

**2. تسویه حساب‌ها**
- Payout management
- Filters:
  - وضعیت (pending/processing/paid)
  - تاریخ درخواست
  - ارائه‌دهنده
- Payouts table:
  - ارائه‌دهنده
  - دوره تسویه (from-to dates)
  - درآمد ارائه‌دهنده
  - کمیسیون پلتفرم
  - مبلغ قابل پرداخت
  - تاریخ درخواست
  - وضعیت
  - عملیات:
    - بررسی و تأیید
    - رد
    - پرداخت (mark as paid)
- Bulk approve/pay

**Payout Detail Modal**:
- Provider info
- Period breakdown
- Booking list (in this period)
- Bank account info
- Transaction calculations
- Approve/Reject buttons
- Payment confirmation

**3. تراکنش‌ها**
- All platform transactions
- Columns:
  - تاریخ
  - نوع (booking/payout/refund)
  - ارائه‌دهنده
  - مشتری
  - مبلغ
  - کمیسیون
  - روش پرداخت
  - وضعیت
  - رسید
- Export to CSV/Excel

**4. گزارشات مالی**
- Report generator:
  - Select period
  - Select report type:
    - درآمد پلتفرم
    - درآمد ارائه‌دهندگان
    - تسویه‌حساب‌ها
    - بازپرداخت‌ها
    - مالیات
  - Generate PDF/Excel
- Scheduled reports:
  - Monthly reports
  - Email to admin
  - Auto-generation settings

---

### 11.8 Reports & Analytics (`/admin/reports`)
**Purpose**: Platform analytics and insights

**Layout**:
- Admin sidebar + header
- Dashboard with charts

**Components**:

**Time Range Selector** (top):
- Presets: امروز / هفته / ماه / سال / دلخواه
- Date range picker
- Compare with previous period toggle

**Analytics Cards**:

**1. کاربران**
- New providers (chart)
- New customers (chart)
- Active users ratio
- Retention rate

**2. رزرو‌ها**
- Total bookings (chart)
- Completion rate
- Cancellation rate
- No-show rate
- Peak hours heatmap
- Popular days

**3. درآمد**
- Revenue chart (total vs commission)
- Average booking value
- Revenue by category (pie chart)
- Revenue by city (bar chart)

**4. محبوبیت**
- Top categories
- Top providers (by bookings)
- Top services
- Top cities

**5. عملکرد**
- Average response time
- Booking confirmation speed
- Customer satisfaction (ratings)
- Provider satisfaction

**6. جغرافیایی**
- Map view:
  - Providers by city (markers)
  - Bookings heatmap
  - Revenue by region

**Export Options**:
- Download as PDF
- Download as Excel
- Schedule automated reports
- Email reports

---

### 11.9 Content Management (`/admin/content`)
**Purpose**: Manage platform content

**Layout**:
- Admin sidebar + header
- Content tabs

**Tabs**:

**1. دسته‌بندی‌ها**
- Categories list
- Each category:
  - Name (Persian + English)
  - Icon
  - Description
  - Active/Inactive
  - Provider count
  - Actions: ویرایش / حذف
- "افزودن دسته‌بندی جدید" button
- Add/Edit modal:
  - Name (Persian)
  - Slug (English)
  - Icon upload
  - Description
  - Parent category (for subcategories)
  - Display order

**2. صفحات**
- Static pages manager
- Pages list:
  - درباره ما
  - تماس با ما
  - قوانین و مقررات
  - حریم خصوصی
  - راهنما
  - سوالات متداول
- Each page:
  - Title
  - Slug
  - Status (published/draft)
  - Last updated
  - Actions: ویرایش / مشاهده
- Rich text editor for content
- SEO fields (meta title, description, keywords)

**3. بلاگ**
- Blog posts list
- Add new post button
- Each post:
  - Title
  - Author
  - Category
  - Published date
  - Status
  - Views
  - Actions: ویرایش / حذف / انتشار
- Post editor:
  - Title
  - Slug
  - Content (rich text)
  - Featured image
  - Category
  - Tags
  - SEO fields
  - Publish/Draft

**4. اعلان‌ها**
- Platform-wide notifications
- Banner notifications (top of site)
- Add notification button
- Fields:
  - Message
  - Type (info/warning/success)
  - Target (all/providers/customers)
  - Start date
  - End date
  - Link (optional)

**5. ایمیل‌ها**
- Email templates manager
- Templates:
  - خوش‌آمدگویی
  - تأیید ثبت‌نام
  - یادآوری رزرو
  - لغو رزرو
  - تأیید پرداخت
  - خبرنامه
- Each template:
  - Subject
  - Body (HTML editor)
  - Variables ({{name}}, {{date}}, etc.)
  - Preview
  - Test send

**6. پیامک‌ها**
- SMS templates
- Same structure as emails
- Character count (70 chars per SMS)

---

### 11.10 System Settings (`/admin/settings`)
**Purpose**: Platform configuration

**Layout**:
- Admin sidebar + header
- Settings tabs

**Tabs**:

**1. عمومی**
- Site name
- Logo upload
- Favicon upload
- Support email
- Support phone
- Social media links
- Business hours
- Timezone
- Language (Persian default)

**2. پرداخت**
- Payment gateways:
  - ZarinPal (enable/disable)
    - Merchant ID
    - Sandbox mode toggle
  - Other gateways (future)
- Commission settings:
  - Platform commission rate (%)
  - Min commission amount
- Payout settings:
  - Payout schedule (weekly/monthly)
  - Min payout amount
  - Payout processing time

**3. رزرو**
- Booking settings:
  - Max booking window (days ahead)
  - Min booking notice (hours)
  - Auto-confirm bookings (on/off)
  - Cancellation policy:
    - Allow cancellation (on/off)
    - Cancellation deadline (hours before)
    - Refund policy
  - No-show policy
  - Overbooking settings

**4. اعلان‌ها**
- Email notifications:
  - New provider registration
  - Booking created
  - Payment received
  - Payout requested
  - Support ticket created
- SMS notifications (same list)
- Admin notification emails (comma-separated)

**5. امنیت**
- Two-factor authentication (require for admins)
- Session timeout (minutes)
- Password policy:
  - Min length
  - Require uppercase
  - Require numbers
  - Require special chars
- IP whitelist (for admin panel)
- Failed login attempts (max before lockout)

**6. API**
- API keys management
- Rate limiting
- Webhook settings
- Third-party integrations:
  - SMS provider (Rahyab)
  - Map provider (OpenStreetMap/Google Maps)
  - Analytics (Google Analytics)

**7. کاربران ادمین**
- Admin users list
- Each admin:
  - Name
  - Email
  - Role (super admin/admin/moderator)
  - Last login
  - Status
  - Actions: ویرایش / حذف
- Add admin button
- Role permissions:
  - Super Admin: Full access
  - Admin: Most features
  - Moderator: Limited (content, support)

**8. پشتیبان‌گیری**
- Database backup:
  - Manual backup button
  - Auto backup schedule
  - Backup history
  - Download/Restore options
- System logs:
  - Error logs
  - Access logs
  - Download logs

---

### 11.11 Support & Help Desk (`/admin/support`)
**Purpose**: Customer & provider support

**Layout**:
- Admin sidebar + header
- Support tickets interface

**Components**:

**Toolbar**:
- Status tabs (with counts):
  - باز (12)
  - در حال بررسی (5)
  - پاسخ داده شده (8)
  - بسته شده (234)
- Priority filter:
  - همه
  - فوری (red)
  - بالا (orange)
  - متوسط (yellow)
  - پایین (green)
- Category filter:
  - مشکل فنی
  - سوال
  - گزارش تخلف
  - درخواست ویژگی
  - سایر
- Assignee filter (admin users)
- Search

**Tickets List**:
- Columns:
  - شماره تیکت
  - موضوع
  - ارسال‌کننده (name + type: provider/customer)
  - دسته‌بندی
  - اولویت (badge)
  - وضعیت
  - مسئول پاسخ
  - تاریخ ایجاد
  - آخرین پاسخ
  - عملیات: مشاهده
- Sort by date, priority, status

**Ticket Detail Modal/Page**:
- Ticket header:
  - Ticket #12345
  - Subject
  - Status badge
  - Priority badge
  - Category tag
  - Created date
- User info sidebar:
  - Name + avatar
  - Type (provider/customer)
  - Email
  - Phone
  - "مشاهده پروفایل" link
  - Previous tickets count
- Conversation thread:
  - Messages (customer/admin)
  - Timestamps
  - Attachments
- Reply box:
  - Rich text editor
  - Attach file
  - Canned responses dropdown
  - Internal note (not visible to user)
  - "ارسال پاسخ" button
- Actions:
  - Assign to admin (dropdown)
  - Change priority
  - Change status
  - Close ticket
  - Merge with another ticket

**Canned Responses** (templates):
- Quick replies:
  - "ممنون از تماس. در حال بررسی هستیم..."
  - "مشکل شما حل شد. لطفاً بررسی کنید..."
  - "برای اطلاعات بیشتر به ... مراجعه کنید"
- Manage canned responses

**Stats Widget**:
- Open tickets: 12
- Avg response time: 2.5 hours
- Avg resolution time: 24 hours
- Customer satisfaction: 4.5/5

---

### 11.12 Admin Profile (`/admin/profile`)
**Purpose**: Admin user profile

**Layout**:
- Admin sidebar + header
- Profile form

**Components**:

**Profile Section**:
- Avatar upload
- Name
- Email
- Phone
- Role (display only)
- Last login
- Account created date

**Security Section**:
- Change password:
  - Current password
  - New password
  - Confirm password
- Two-factor authentication:
  - Enable/Disable toggle
  - QR code setup
  - Backup codes
- Active sessions:
  - Device list
  - "خروج از همه دستگاه‌ها" button

**Notification Preferences**:
- Email notifications (on/off for each type)
- SMS notifications
- Browser notifications

**Activity Log** (read-only):
- Admin's recent actions
- Login history
- Changes made

---

## Summary: Pages to Design

### Authentication (2 pages)
1. Login
2. Phone Verification

### Provider Module (16+ pages)
**Provider Registration (9 steps):**
1. Business Info
2. Category
3. Location
4. Services
5. Staff
6. Working Hours
7. Gallery
8. Feedback
9. Completion

**Provider Dashboard (7+ pages):**
1. Dashboard Home
2. Bookings List
3. Booking Detail
4. Services List
5. Staff List
6. Financial
7. Settings (8 tabs)

### Customer Module (4 pages)
1. Home/Search
2. Provider Profile
3. Booking Flow
4. Customer Dashboard

### Admin Panel (12 pages)
1. Admin Dashboard
2. Providers Management
3. Provider Detail/Approval
4. Customers Management
5. Customer Detail
6. All Bookings
7. Financial Management (4 tabs)
8. Reports & Analytics
9. Content Management (6 tabs)
10. System Settings (8 tabs)
11. Support & Help Desk
12. Admin Profile

### Shared Components (12+)
- Header, Sidebar, Buttons, Inputs, Cards, Modals, Tables, Badges, Toasts, Loading, Empty States, Avatars

**Total**: ~37 unique page designs + 12+ component designs

**Breakdown by Module:**
- Authentication: 2 pages
- Provider: 16 pages (9 registration steps + 7 dashboard pages)
- Customer: 4 pages
- Admin: 12 pages
- Shared Components: 12+

**Total Persian UI Screens**: 37 pages covering complete platform

---

## Instructions for Figma AI

1. **Create frames** for each page listed above
2. **Use the design system** (colors, typography, spacing) specified
3. **Include all states** (default, hover, active, error, loading, empty)
4. **Design mobile versions** for all pages (responsive)
5. **Use Persian text** throughout (sample text provided in each section)
6. **Apply RTL layout** (all alignments reversed)
7. **Export to HTML/Vue** with proper structure and styling
8. **Maintain consistency** across all pages (reuse components)

---

**Note**: This prompt provides complete specifications for the entire Booksy platform. Generate designs progressively, starting with Authentication → Registration → Dashboard → Customer pages.
