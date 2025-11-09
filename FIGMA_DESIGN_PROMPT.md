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

## Summary: Pages to Design

### Authentication (2 pages)
1. Login
2. Phone Verification

### Provider Registration (9 steps)
1. Business Info
2. Category
3. Location
4. Services
5. Staff
6. Working Hours
7. Gallery
8. Feedback
9. Completion

### Provider Dashboard (7+ pages)
1. Dashboard Home
2. Bookings List
3. Booking Detail
4. Services List
5. Staff List
6. Financial
7. Settings (8 tabs)

### Customer (4 pages)
1. Home/Search
2. Provider Profile
3. Booking Flow
4. Customer Dashboard

### Shared Components (12+)
- Header, Sidebar, Buttons, Inputs, Cards, Modals, Tables, Badges, Toasts, Loading, Empty States, Avatars

**Total**: ~25 unique page designs + 12+ component designs

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
