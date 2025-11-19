# Mobile Filters Guide - How to Access Filters on Mobile 📱

## Quick Answer
On mobile devices, look for the **purple floating button** at the bottom-right corner labeled **"فیلترها"** (Filters).

---

## Visual Guide

### Step 1: Find the Filter Button
When viewing the Provider Search page on mobile (screen width < 768px):

```
┌─────────────────────────────────┐
│  Provider Search Results        │
│                                 │
│  [Provider Card]                │
│  [Provider Card]                │
│  [Provider Card]                │
│                                 │
│                                 │
│                    ┌──────────┐ │
│                    │ 🔍       │ │ ← Floating Filter Button
│                    │ فیلترها  │ │   (Bottom-Right Corner)
│                    │    (3)   │ │   Badge shows active filter count
└────────────────────┴──────────┴─┘
```

**Button Features:**
- **Location**: Fixed position at bottom-right
- **Label**: "فیلترها" (Filters in Persian)
- **Icon**: Funnel/filter icon
- **Badge**: Shows number of active filters (if any)
- **Color**: Purple gradient background (#8b5cf6)
- **Always Visible**: Floats above content

---

### Step 2: Tap the Button
When you tap the "فیلترها" button:

```
┌─────────────────────────────────┐
│                                 │
│  ┌──────────────────────────┐  │
│  │ فیلترها            [X]  │  │ ← Filter Drawer Header
│  ├──────────────────────────┤  │
│  │                          │  │
│  │ [Voice Search Button]    │  │
│  │                          │  │
│  │ [Category Dropdown]      │  │
│  │                          │  │
│  │ [Price Range Buttons]    │  │
│  │                          │  │
│  │ [Location Button]        │  │
│  │                          │  │
│  │ [Rating Filter]          │  │
│  │                          │  │
│  │ [Apply] [Clear]          │  │
│  └──────────────────────────┘  │
│                                 │
│ [Backdrop - tap to close]       │
└─────────────────────────────────┘
```

**What Happens:**
1. Filter drawer **slides in from the right** (0-300ms animation)
2. **Backdrop** (semi-transparent overlay) appears behind the drawer
3. Main content is **locked** (no scrolling)
4. Drawer takes **85% of screen width** (max 380px)

---

### Step 3: Use the Filters
Inside the drawer, you have access to:

#### 1. **Voice Search** 🎤
```
┌─────────────────────────────────┐
│  🎤 جستجوی صوتی                 │  ← Tap to speak
└─────────────────────────────────┘
```
- Speak your search query in Persian
- Automatically fills the search term

#### 2. **Location** 📍
```
┌─────────────────────────────────┐
│  📍 موقعیت من                   │  ← Uses your GPS
└─────────────────────────────────┘
```
- Tap to use your current location
- Filters providers near you

#### 3. **Service Category**
```
┌─────────────────────────────────┐
│  دسته‌بندی خدمات                │
│  [Select Category ▼]            │
└─────────────────────────────────┘
```

#### 4. **Price Range** 💰
```
┌─────────────────────────────────┐
│  محدوده قیمت                    │
│  [💰 اقتصادی] [💰💰 متوسط]      │
│  [💰💰💰 لوکس]                   │
└─────────────────────────────────┘
```
- Colorful button chips
- Select one price range

#### 5. **Provider Type**
```
┌─────────────────────────────────┐
│  نوع ارائه‌دهنده                │
│  [Individual] [Business]        │
└─────────────────────────────────┘
```

#### 6. **Minimum Rating** ⭐
```
┌─────────────────────────────────┐
│  حداقل امتیاز                   │
│  ⭐ ⭐ ⭐ ⭐ ⭐                   │
│  [Slider: 4.0]                  │
└─────────────────────────────────┘
```
- Visual star display
- Slider to select minimum rating

---

### Step 4: Apply or Close
At the bottom of the drawer:

```
┌─────────────────────────────────┐
│  [پاک کردن]      [اعمال فیلتر]  │
│   Clear          Apply          │
└─────────────────────────────────┘
```

**To Apply Filters:**
- Tap **"اعمال فیلتر"** (Apply Filter) button
- Drawer closes automatically
- Results update immediately

**To Close Without Applying:**
1. Tap the **[X]** button in header
2. OR tap the **backdrop** (dark area outside drawer)
3. OR tap **"پاک کردن"** (Clear) to remove all filters

---

## Technical Details

### When Does the Button Appear?
The filter button appears when:
```javascript
window.innerWidth < 768px  // Mobile breakpoint
```

### Button Styling
```css
Position: fixed
Bottom: 2rem (32px from bottom)
Right: 2rem (32px from right, RTL layout)
Z-index: 999 (always on top)
Background: Purple gradient
Box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15)
```

### Drawer Animation
```css
Transition: right 0.3s cubic-bezier(0.4, 0, 0.2, 1)
Initial: right: -100% (off-screen)
Open: right: 0 (visible)
```

### Desktop Behavior
On desktop (width ≥ 768px):
- Filter button is **hidden**
- Filters appear in **left sidebar** (sticky position)
- No drawer animation needed
- Always visible

---

## Troubleshooting

### Can't See the Filter Button?
1. **Check screen size**: Must be < 768px width
2. **Try portrait mode**: Rotate phone to portrait
3. **Zoom out**: Browser zoom might affect layout
4. **Check scroll position**: Button should be visible even when scrolling

### Button Not Working?
1. **Check console**: Look for JavaScript errors
2. **Clear cache**: Hard refresh (Ctrl+Shift+R)
3. **Try different browser**: Test in Chrome/Safari mobile

### Drawer Won't Close?
- Tap outside the drawer (on backdrop)
- Tap the X button in header
- Swipe right (if touch events work)

---

## Accessibility

### Touch Targets
- Filter button: **56px × 48px** (exceeds 44px minimum)
- All filter controls: **44px+ touch targets**
- Proper spacing between buttons

### Keyboard Support (Future)
- Tab to navigate through filters
- Enter/Space to activate buttons
- Esc to close drawer

---

## Screenshots Reference

### Filter Button (Closed State)
- Purple gradient button
- Filter icon (funnel shape)
- Text: "فیلترها"
- Badge with number (if filters active)

### Filter Button (Active State)
- Changes to **red gradient** when drawer is open
- Indicates drawer is active
- Same position and size

### Filter Drawer (Open State)
- Slides from right edge
- White background
- Header with close button
- Scrollable content area
- Action buttons at bottom

---

## Testing on Mobile

### Real Device Testing
1. Open **http://192.168.1.5:3001** on your phone
2. Navigate to Provider Search
3. Look for floating "فیلترها" button
4. Tap to open drawer
5. Try all filter options
6. Tap backdrop to close

### Browser DevTools Testing
1. Open Chrome DevTools (F12)
2. Click "Toggle Device Toolbar" (Ctrl+Shift+M)
3. Select "iPhone 12" or similar
4. Resize to < 768px width
5. Filter button should appear

### Responsive Breakpoints
```
Mobile:   < 768px  (Filter button visible)
Tablet:   768px - 1024px  (Sidebar visible)
Desktop:  > 1024px  (Full sidebar)
```

---

## Quick Tips

1. **Badge Number**: The small circle shows how many filters are active
2. **Auto-Close**: Drawer closes automatically when you apply filters
3. **Body Lock**: Can't scroll main content while drawer is open
4. **Backdrop Click**: Quickest way to close without applying changes
5. **Voice Search**: Works best in quiet environments for Persian speech recognition

---

## Developer Notes

### Component Hierarchy
```
ProviderSearchView
  ├── Mobile Filter Toggle Button (v-if="isMobile")
  ├── Search Sidebar (with mobile-open class)
  │   ├── Mobile Filter Header (v-if="isMobile")
  │   └── ProviderFilters
  └── Mobile Backdrop (v-if="isMobile && showMobileFilters")
```

### State Management
```javascript
isMobile: ref(false)           // Computed from window.innerWidth
showMobileFilters: ref(false)  // Controls drawer open/closed
activeFiltersCount: computed() // Shows badge number
```

### Event Handlers
```javascript
toggleMobileFilters()  // Opens/closes drawer
closeMobileFilters()   // Closes drawer
handleApplyFilters()   // Applies + closes drawer
```

---

## Summary

**Mobile Filter Access:**
1. ✅ Look for purple "فیلترها" button at bottom-right
2. ✅ Tap to open drawer from right
3. ✅ Use all filter options (voice, location, etc.)
4. ✅ Tap "اعمال فیلتر" to apply
5. ✅ Drawer closes automatically

**It's that simple!** 🎉
