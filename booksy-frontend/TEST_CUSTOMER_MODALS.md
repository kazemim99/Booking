# Testing Customer Profile Modals

## Method 1: Test with Real Authentication

1. Start the dev server:
   ```bash
   cd booksy-frontend
   npm run dev
   ```

2. Navigate to `http://localhost:5173/`

3. Login as a customer user

4. Click your user avatar/name in the top-right header

5. Click each menu item to test the modals

---

## Method 2: Test Without Authentication (Browser Console)

1. Start the dev server and navigate to `http://localhost:5173/`

2. Open browser DevTools (F12) → Console tab

3. Paste and run this code to simulate an authenticated user:

```javascript
// Import stores
import { useAuthStore } from '@/core/stores/modules/auth.store'
import { useCustomerStore } from '@/modules/customer/stores/customer.store'

// Get store instances
const authStore = useAuthStore()
const customerStore = useCustomerStore()

// Mock authenticated user
authStore.user = {
  id: 'test-customer-123',
  fullName: 'علی محمدی',
  phoneNumber: '09123456789',
  email: 'ali@example.com',
  role: 'customer'
}
authStore.isAuthenticated = true

// Mock customer profile
customerStore.profile = {
  id: 'test-customer-123',
  fullName: 'علی محمدی',
  phoneNumber: '09123456789',
  email: 'ali@example.com',
  createdAt: new Date().toISOString()
}

// Mock upcoming bookings
customerStore.upcomingBookings = [
  {
    id: 'b1',
    providerId: 'p1',
    providerName: 'آرایشگاه زیبا',
    providerLogoUrl: 'https://via.placeholder.com/100',
    serviceName: 'کوتاهی مو',
    startTime: new Date(Date.now() + 86400000).toISOString(), // Tomorrow
    endTime: new Date(Date.now() + 90000000).toISOString(),
    status: 'Confirmed',
    totalPrice: 150000
  }
]

// Mock booking history
customerStore.bookingHistory = [
  {
    id: 'b2',
    providerId: 'p1',
    providerName: 'آرایشگاه زیبا',
    providerLogoUrl: 'https://via.placeholder.com/100',
    serviceName: 'رنگ مو',
    startTime: new Date(Date.now() - 86400000 * 7).toISOString(), // 7 days ago
    endTime: new Date(Date.now() - 86400000 * 7 + 3600000).toISOString(),
    status: 'Completed',
    totalPrice: 250000,
    createdAt: new Date(Date.now() - 86400000 * 7).toISOString()
  }
]

// Mock favorites
customerStore.favorites = [
  {
    id: 'f1',
    customerId: 'test-customer-123',
    providerId: 'p1',
    providerName: 'آرایشگاه زیبا',
    providerLogoUrl: 'https://via.placeholder.com/100',
    providerCategory: 'آرایشگاه',
    providerRating: 4.5,
    createdAt: new Date().toISOString()
  }
]

// Mock reviews
customerStore.reviews = [
  {
    id: 'r1',
    providerId: 'p1',
    providerName: 'آرایشگاه زیبا',
    providerLogoUrl: 'https://via.placeholder.com/100',
    serviceId: 's1',
    serviceName: 'کوتاهی مو',
    rating: 5,
    text: 'خدمات عالی بود! خیلی راضی هستم.',
    createdAt: new Date(Date.now() - 86400000 * 2).toISOString(), // 2 days ago
    canEdit: true
  }
]

// Mock preferences
customerStore.preferences = {
  smsEnabled: true,
  emailEnabled: true,
  reminderTiming: '24h'
}

console.log('✅ Mock data loaded! Refresh the page to see the user menu.')

// Force refresh
window.location.reload()
```

4. After the page reloads, you should see the user menu in the top-right

5. Click it to test all modals!

---

## Method 3: Quick Manual Test (Direct Modal Trigger)

If you just want to test the modals quickly without setting up auth:

1. Open browser console
2. Run this to open each modal directly:

```javascript
import { useCustomerStore } from '@/modules/customer/stores/customer.store'
const store = useCustomerStore()

// Test Profile Edit Modal
store.openModal('profile')

// Test Bookings Sidebar
store.openModal('bookings')

// Test Favorites Modal
store.openModal('favorites')

// Test Reviews Modal
store.openModal('reviews')

// Test Settings Modal
store.openModal('settings')
```

---

## Expected Results

### ✅ Profile Edit Modal
- Shows full name input (editable)
- Shows phone number (disabled/read-only)
- Shows email input (optional)
- Save button updates profile
- Cancel/ESC/click-outside closes modal

### ✅ Bookings Sidebar
- Slides in from left
- Has "Upcoming" and "Past" tabs
- Shows booking cards with provider info, service, date/time
- Has action buttons (Cancel, Reschedule, Rebook)
- Empty states work

### ✅ Favorites Modal
- Shows grid of favorite provider cards
- Heart button removes favorites (with confirmation)
- Quick Book button works
- Empty state shows correctly

### ✅ Reviews Modal
- Shows list of reviews
- Edit button appears only for reviews <7 days old
- Opens edit modal with star rating and text area
- Character counter works (500 max)

### ✅ Settings Modal
- Shows notification toggles (SMS, Email)
- Shows reminder timing dropdown
- Auto-saves after 500ms
- Shows warning when all notifications disabled

---

## Troubleshooting

### "Cannot find module" errors
Run: `npm install` and restart dev server

### Modals don't open
Check browser console for errors. Make sure CustomerModalsContainer is in App.vue

### No header on landing page
The AppHeader should now be visible. If not, clear cache and hard refresh (Ctrl+Shift+R)

### UserMenu shows login button instead of profile
You need to either:
1. Login as a customer user, OR
2. Use Method 2 above to mock the authenticated state
