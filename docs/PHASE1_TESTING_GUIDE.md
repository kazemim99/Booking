# Phase 1 Manual Testing Guide
## Customer Search & Browse Functionality

**Last Updated:** November 15, 2025
**Status:** Ready for Testing

---

## Quick Start: Running the Frontend

### Option 1: Full Stack (Recommended)

```bash
# Terminal 1 - Backend
cd /home/user/Booking
dotnet run --project src/BoundedContexts/ServiceCatalog/Booksy.ServiceCatalog.Api

# Terminal 2 - Frontend
cd /home/user/Booking/booksy-frontend
npm install  # First time only
npm run dev
```

Then open: `http://localhost:5173/customer/providers`

### Option 2: Frontend Only (Mock Mode)

If backend isn't running, the frontend will show API errors, which is fine for UI testing.

```bash
cd /home/user/Booking/booksy-frontend
npm run dev
```

---

## Manual Testing Checklist

### 1. Initial Page Load

**Navigate to:** `/customer/providers`

**✅ Verify:**
- [ ] Page loads without console errors
- [ ] "جستجوی ارائه‌دهندگان" (Search Providers) title displays
- [ ] Filter sidebar appears on the left
- [ ] Loading spinner appears initially
- [ ] After loading, either results or empty state shows

**Expected Behavior:**
- If backend is running and has providers → Shows provider cards
- If backend is running but no providers → Shows empty state with 🔍 icon
- If backend is not running → Shows error state with ⚠️ icon and "تلاش مجدد" (Retry) button

---

### 2. Search Filters UI

**Location:** Left sidebar

**✅ Verify Each Filter:**

#### Search Input
- [ ] Input field with placeholder "نام ارائه‌دهنده یا خدمت..."
- [ ] Persian text input works
- [ ] Typing triggers search after 500ms delay (debounced)

#### Category Checkboxes
- [ ] 6 categories listed:
  - آرایشگاه (Salon)
  - کلینیک (Clinic)
  - اسپا (Spa)
  - استودیو (Studio)
  - حرفه‌ای (Professional)
  - فردی (Individual)
- [ ] Checkboxes are clickable
- [ ] Multiple selections allowed
- [ ] Applying filter triggers new search

#### Rating Slider
- [ ] Slider ranges from 0 to 5
- [ ] Current value displays: "حداقل امتیاز: X ⭐"
- [ ] Moving slider updates the display
- [ ] Releasing slider triggers search

#### Distance Slider (if location enabled)
- [ ] Only shows if location search is active
- [ ] Ranges from 1 to 50 km
- [ ] Display: "فاصله: X کیلومتر"

#### Price Range
- [ ] Two number inputs: "از" (from) and "تا" (to)
- [ ] Numbers are accepted
- [ ] Changing values triggers search

#### Availability Toggles
- [ ] "الان باز است" (Open Now) checkbox
- [ ] "نوبت آزاد دارد" (Has Available Slots) checkbox
- [ ] Toggling triggers search

#### Sorting Dropdown
- [ ] Options:
  - پیش‌فرض (Default)
  - نزدیک‌ترین (Nearest)
  - بالاترین امتیاز (Highest Rating)
  - محبوب‌ترین (Most Popular)
  - جدیدترین (Newest)
- [ ] Changing sort triggers search

#### Clear All Button
- [ ] Only appears when filters are active
- [ ] Button text: "پاک کردن همه" (Clear All)
- [ ] Clicking clears all filters and resets search

---

### 3. Provider Card Component

**✅ Verify Each Card Element:**

#### Image Section
- [ ] Cover/logo image displays (or placeholder)
- [ ] Distance badge shows if location search (e.g., "2.5 کیلومتر")
- [ ] Status badge shows:
  - Green "باز" (Open) if currently open
  - Red "بسته" (Closed) if closed
- [ ] Hover effect: image scales slightly

#### Provider Info Section
- [ ] Business name displays prominently
- [ ] Provider type shows (e.g., "آرایشگاه")
- [ ] Rating with star: "⭐ 4.8"
- [ ] Review count: "(X نظر)"
- [ ] Address with 📍 icon
- [ ] Tags display (max 3)
- [ ] Price range (if available)
- [ ] Next available slot (if available)

#### Features
- [ ] "✓ رزرو آنلاین" if online booking enabled
- [ ] "✓ خدمات سیار" if mobile services offered

#### Actions
- [ ] Favorite heart button (top right)
- [ ] "رزرو نوبت" (Book Now) button at bottom
- [ ] Hover effects on buttons
- [ ] Card shadow increases on hover
- [ ] Card lifts slightly on hover

---

### 4. View Mode Toggle

**Location:** Top right of page

**✅ Verify:**
- [ ] Two buttons visible: Grid (⊞) and List (☰)
- [ ] Grid view is active by default (purple background)
- [ ] Clicking List changes layout to vertical list
- [ ] Clicking Grid returns to grid layout
- [ ] Active button has purple background
- [ ] Inactive button is gray
- [ ] View preference saves to localStorage

**Grid View:**
- [ ] Cards in responsive grid
- [ ] 3-4 cards per row on desktop
- [ ] 2 cards per row on tablet
- [ ] 1 card per row on mobile

**List View:**
- [ ] Cards in vertical stack
- [ ] Full width cards
- [ ] More horizontal layout

---

### 5. Search Results Display

**✅ Verify Different States:**

#### Loading State
- [ ] Spinning purple loader
- [ ] Text: "در حال جستجو..." (Searching...)
- [ ] Centered on page
- [ ] White background card

#### Error State
- [ ] Red ⚠️ icon
- [ ] Error message in red
- [ ] "تلاش مجدد" (Retry) button
- [ ] Clicking retry attempts search again

#### Empty State
- [ ] 🔍 icon (faded)
- [ ] Heading: "ارائه‌دهنده‌ای یافت نشد" (No providers found)
- [ ] Helpful message
- [ ] "پاک کردن فیلترها" (Clear Filters) button
- [ ] Clicking button clears filters

#### Results State
- [ ] Search stats bar shows: "X ارائه‌دهنده یافت شد"
- [ ] Active filters badge (if filters applied)
- [ ] Provider cards display
- [ ] Pagination controls (if > 20 results)

---

### 6. Pagination

**✅ Verify (only if more than 20 results):**

#### Pagination Controls
- [ ] "← قبلی" (Previous) button on right
- [ ] Page number buttons in center
- [ ] "بعدی →" (Next) button on left
- [ ] Shows max 7 page numbers
- [ ] Current page is purple
- [ ] Other pages are gray

#### Functionality
- [ ] Previous button disabled on page 1
- [ ] Next button disabled on last page
- [ ] Clicking page number loads that page
- [ ] Clicking Next/Previous loads adjacent page
- [ ] Page transition shows loading state
- [ ] Scroll to top after page change

---

### 7. Responsive Design

**✅ Test on Different Screen Sizes:**

#### Desktop (1400px+)
- [ ] Sidebar on left (300px wide)
- [ ] Main content on right
- [ ] Grid: 3-4 cards per row
- [ ] Sidebar is sticky (scrolls with page)

#### Tablet (768px - 1024px)
- [ ] Sidebar full width at top
- [ ] Grid: 2 cards per row
- [ ] Touch-friendly buttons

#### Mobile (< 768px)
- [ ] Sidebar full width
- [ ] Grid: 1 card per row
- [ ] Larger touch targets
- [ ] Pagination wraps to 2 rows
- [ ] Smaller text sizes

---

### 8. RTL (Right-to-Left) Layout

**✅ Verify:**
- [ ] All text aligns to the right
- [ ] Sidebar on right side (in RTL)
- [ ] Arrows point correctly (← for next, → for previous)
- [ ] Persian numerals display correctly
- [ ] Icons on correct side
- [ ] Scrollbars on left side

---

### 9. Interactive Features

**✅ Test User Interactions:**

#### Search Flow
1. [ ] Type in search box → Results filter after 500ms
2. [ ] Select category → Results filter immediately
3. [ ] Move rating slider → Results filter on release
4. [ ] Change price range → Results filter after typing
5. [ ] Toggle "Open Now" → Results update
6. [ ] Change sort order → Results reorder

#### Navigation
1. [ ] Click provider card → Navigates to provider details
2. [ ] Click "رزرو نوبت" → Navigates to booking wizard
3. [ ] Click favorite button → Adds to favorites
4. [ ] Click pagination → Loads new page

#### State Persistence
1. [ ] Change view mode → Reload page → View mode persists
2. [ ] Type in search → Reload → Search term may persist
3. [ ] Apply filters → Navigate away → Come back → Filters reset

---

### 10. Performance Testing

**✅ Verify Performance:**

#### Load Times
- [ ] Initial page load < 2 seconds
- [ ] Search results appear < 500ms
- [ ] Page transitions smooth
- [ ] No layout shifts during load

#### Interactions
- [ ] Search debounce works (not firing on every keystroke)
- [ ] Filters don't cause lag
- [ ] Pagination is instant
- [ ] View mode toggle is instant

#### Browser Console
- [ ] No JavaScript errors
- [ ] No 404 errors for assets
- [ ] API calls use correct endpoints
- [ ] Network tab shows reasonable request counts

---

## Testing with Browser DevTools

### 1. Network Tab

**Monitor API Calls:**

```
Expected Endpoint:
GET /api/v1.0/providers/search?pageNumber=1&pageSize=20

With Filters:
GET /api/v1.0/providers/search?searchTerm=آرایشگاه&category=Salon&minRating=4&pageNumber=1&pageSize=20
```

**✅ Verify:**
- [ ] Correct endpoint called
- [ ] Query params formatted correctly
- [ ] Response status 200 (or 404/500 if backend issue)
- [ ] Response has correct structure

### 2. Console Tab

**Check for Errors:**
- [ ] No red errors on page load
- [ ] No uncaught promises
- [ ] Store actions logged (if dev mode)

**Expected Logs:**
```
[ProviderListView] Error loading data: (if backend down)
[SearchStore] Search error: (if API fails)
```

### 3. Vue DevTools

**If Vue DevTools installed:**

**Check Pinia Store:**
- [ ] `customer-search` store exists
- [ ] `providers` array populates
- [ ] `currentFilters` updates
- [ ] `isSearching` toggles correctly
- [ ] `error` message shows on failures

### 4. Local Storage

**Check Persistence:**

Open DevTools → Application → Local Storage

**Expected Keys:**
- [ ] `provider-view-mode`: "grid" or "list"
- [ ] `recent-searches`: Array of search terms

---

## Testing Scenarios

### Scenario 1: First-Time User

**Steps:**
1. Navigate to `/customer/providers`
2. Wait for initial load
3. Browse results (if any)
4. Try different view modes
5. Apply some filters
6. View a provider detail

**Expected:**
- Empty filters initially
- Grid view by default
- Results load automatically
- Smooth interactions

---

### Scenario 2: Search by Category

**Steps:**
1. Navigate to search page
2. Select "آرایشگاه" (Salon) category
3. Observe results filter

**Expected:**
- Only salons display
- Active filter badge shows "1 فیلتر فعال"
- Results count updates
- Can add more categories

---

### Scenario 3: Filtered Search

**Steps:**
1. Type "آرایشگاه" in search
2. Set min rating to 4
3. Set price range 100,000 - 500,000
4. Check "Open Now"

**Expected:**
- Multiple filters active
- Badge shows "4 فیلتر فعال"
- Results highly filtered
- Can clear all with one click

---

### Scenario 4: Pagination

**Steps:**
1. Get results with multiple pages
2. Click page 2
3. Click Next
4. Click Previous
5. Jump to last page

**Expected:**
- Smooth page transitions
- Loading state on each change
- Scroll to top
- Correct page highlighted

---

### Scenario 5: Error Handling

**Steps:**
1. Stop backend (if running)
2. Navigate to search page
3. Observe error state
4. Click "تلاش مجدد"

**Expected:**
- Red error icon displays
- Error message in Persian
- Retry button works
- Can attempt retry

---

### Scenario 6: Empty Results

**Steps:**
1. Apply very restrictive filters
2. E.g., Min rating 5, High price, specific category

**Expected:**
- Empty state shows
- 🔍 icon displays
- Helpful message
- Clear filters button available

---

## Common Issues & Solutions

### Issue 1: No Results Show

**Possible Causes:**
- Backend not running
- No providers in database
- Filters too restrictive

**How to Check:**
1. Open Network tab
2. Check API response
3. Look for errors in console

**Solution:**
- Start backend
- Add test data
- Clear filters

---

### Issue 2: Search Doesn't Filter

**Check:**
- DevTools console for errors
- Network tab for API calls
- Store state in Vue DevTools

**Solution:**
- Verify searchStore is imported
- Check filter state updates
- Ensure API integration works

---

### Issue 3: Persian Text Issues

**Check:**
- Font supports Persian characters
- RTL CSS is applied
- `dir="rtl"` attribute present

**Solution:**
- Clear browser cache
- Check CSS is loaded
- Verify lang attributes

---

### Issue 4: View Mode Not Persisting

**Check:**
- Local Storage in DevTools
- Key: `provider-view-mode`

**Solution:**
- Clear local storage
- Try again
- Check browser permissions

---

## Mock Data for Testing

If backend is down, you can temporarily test with mock data:

**Edit `search.store.ts` to add mock data:**

```typescript
// Temporary: Add to loadInitialData or searchProviders
providers.value = [
  {
    id: '1',
    businessName: 'آرایشگاه زیبا',
    type: ProviderType.Salon,
    status: ProviderStatus.Active,
    logoUrl: null,
    averageRating: 4.8,
    totalReviews: 125,
    address: {
      formattedAddress: 'تهران، ونک',
      city: 'تهران',
      province: 'تهران'
    },
    isOpen: true,
    currentStatus: 'باز',
    allowOnlineBooking: true,
    offersMobileServices: false,
    tags: ['زنانه', 'مردانه', 'کودک']
  },
  // Add more...
]
```

---

## Success Criteria

**Phase 1 is successful if:**

✅ All UI components render correctly
✅ Filters work and update results
✅ Pagination functions properly
✅ View modes toggle correctly
✅ RTL layout is correct
✅ No console errors
✅ Responsive on all screen sizes
✅ Loading/error/empty states display
✅ API integration works (when backend running)
✅ User interactions feel smooth

---

## Next Steps After Testing

1. **Document Issues:** Create list of bugs found
2. **Performance:** Note any slow interactions
3. **UX Feedback:** Suggest improvements
4. **Backend Integration:** Test with real data
5. **Ready for Phase 2:** Move to booking wizard

---

## Quick Test Commands

```bash
# Full check
cd /home/user/Booking/booksy-frontend
npm run lint        # Check code quality
npm run type-check  # Check TypeScript
npm run build       # Test production build
npm run preview     # Preview production build

# Development
npm run dev         # Start dev server
```

---

## Contact & Support

**Questions or Issues?**
- Check console for errors
- Review this guide
- Test with mock data first
- Verify backend is running

**Ready to proceed?**
After testing Phase 1, we can move to Phase 2: Booking Wizard Implementation!
